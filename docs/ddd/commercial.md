# Commercial: compras y ventas

Las rutas aparecen en Scalar (entorno Development) bajo la etiqueta **Commercial**.
Hay ocho endpoints: seis crean/consultan operaciones y dos las confirman.
Crear borradores no modifica productos, lotes ni `inventory_movements`.

Los cuatro POST requieren JWT de un usuario activo. Empleado confirma sus propios
documentos; Administrador puede confirmar documentos ajenos. El creador se conserva
y el movimiento registra al confirmador real. Ver [autenticación IAM](autenticacion-iam.md)
para login, inicialización administrativa y configuración segura.

## Endpoints

- `POST /api/v1/purchases`: registra una compra Draft y sus detalles.
- `GET /api/v1/purchases`: lista compras y totales.
- `GET /api/v1/purchases/{id}`: obtiene compra y detalles; 404 si no existe.
- `POST /api/v1/sales`: registra una venta Draft y sus detalles.
- `GET /api/v1/sales`: lista ventas y totales.
- `GET /api/v1/sales/{id}`: obtiene venta y detalles; 404 si no existe.
- `POST /api/v1/purchases/{id}/confirm`: confirma compra e ingresa mercadería.
- `POST /api/v1/sales/{id}/confirm`: confirma venta y descuenta mercadería.

## JSON para Scalar

Compra (mismo contrato para venta):

```json
{
  "items": [
    {
      "productId": "GUID-DE-PRODUCTO-EXISTENTE",
      "lotId": null,
      "quantity": 3,
      "unitAmount": 2.50
    }
  ]
}
```

Para `POST /api/v1/sales`, el importe representa precio unitario. Para compras representa costo unitario. `productId`, `quantity`, `unitAmount` y al menos un detalle son obligatorios; `lotId` es opcional y cuando se especifica debe existir y pertenecer al producto. El usuario se obtiene del JWT: enviar `userId` en el body se rechaza con 400. Cantidad debe ser mayor que cero; importe admite hasta dos decimales y no puede ser negativo. Éxito: 201 con recurso y GUID; validación/referencia inválida: 400; consulta inexistente: 404.

Prueba en Scalar: iniciar la API en Development, abrir `/scalar`, hacer login y configurar Bearer. Seleccionar POST y pegar el JSON con GUID reales de productos. Copiar el ID de la respuesta para probar GET por ID y revisar las listas. Usar una base aislada para pruebas que escriban datos.

## Verificación en MySQL Workbench

```sql
SELECT id, user_id, registered_at, status, total FROM purchases ORDER BY registered_at DESC;
SELECT purchase_id, product_id, lot_id, quantity, unit_cost, subtotal FROM purchase_items;
SELECT id, user_id, registered_at, status, total FROM sales ORDER BY registered_at DESC;
SELECT sale_id, product_id, lot_id, quantity, unit_price, subtotal FROM sale_items;
```

Los totales se calculan sumando cada subtotal `cantidad * importe unitario`; el dominio
persiste ambos. Crear un borrador no escribe en inventario. Confirmar sí modifica
inventario y registra movimientos, según las reglas siguientes.

Los errores de confirmación se presentan en español sin exponer excepciones MySQL.
Las pruebas HTTP/MySQL de las confirmaciones y sus resultados se detallan al final.

## Confirmación de compras

El POST de confirmación no recibe cuerpo: utiliza el GUID de la compra Draft.
ConfirmationService carga la cabecera bloqueada y TODOS sus detalles. Valida Draft,
usuario activo, productos activos, stock igual a suma de lotes y referencias válidas.

- Con lotId existente: Product.ReceiveStock repone ese lote y aumenta Stock/Version.
  Es una ampliación mínima de Inventory; ProductLot.Receive es interno. No se añadieron
  setters ni se actualiza stock mediante SQL manual.
- Con lotId null: Product.ReceiveLot crea un lote por detalle, código determinista
  `PUR-{purchaseId sin guiones}-{itemId sin guiones}` (69 caracteres), vencimiento
  null y cantidad del detalle. Purchase.AssignReceivedLot deja el GUID generado en
  el detalle mientras sigue Draft. No cambia ningún GUID de entidades existentes.
- Cada detalle genera un InventoryMovement positivo, MovementType Purchase,
  SourceType Purchase, SourceId del agregado y LotId efectivo.
- Confirm del agregado recalcula el total y cambia el estado; se persiste junto al
  inventario. Si cualquier detalle falla, no se confirma parcialmente.

El contrato actual de creación no tiene lotCode ni expirationDate. Por eso no se
inventan esos campos en el JSON: el lote nuevo usa el código anterior y no tiene
vencimiento. Para reponer un lote con vencimiento conocido, seleccionarlo mediante
su GUID existente. Ampliar datos de recepción requerirá otro acuerdo del equipo.

## Confirmación de ventas

También recibe solo el GUID de una venta Draft. Carga todos los productos/lotes,
valida el total requerido por producto y por lote explícito antes de descontar.
No basta con validar cada detalle por separado: dos detalles pueden compartir lote.

- Con lotId: retira exclusivamente de ese lote mediante Product.RemoveStock.
- Sin lotId: distribuye FIFO por ReceivedAt y GUID como desempate, usando lotes con
  unidades disponibles. Los lotes explícitos de otros detalles se descuentan primero
  para que FIFO no consuma sus reservas.
- Cada descuento efectivo genera un movimiento negativo, MovementType Sale,
  SourceType Sale y SourceId igual al GUID de la venta. Una línea sin lote puede
  producir varios movimientos; la línea conserva lotId null y los movimientos
  guardan los lotes exactos. La suma de deltas es el negativo de la cantidad vendida.
- Reutiliza RemoveStock y Version; nunca escribe stock directo. Stock insuficiente
  devuelve 409 y conserva Draft, existencias y movimientos sin cambios.

FIFO sigue la convención existente de Operations. No añade una regla nueva de
rechazo por vencimiento: debe acordarse después si el negocio necesita FEFO o
bloqueo de lotes vencidos. No genera alertas automáticamente.

## Atomicidad, doble confirmación y concurrencia

CommercialConfirmationSession usa el mismo BodeGoDbContext de los módulos integrados.
Los repositorios anteriores se conservan para crear/consultar Draft. La sesión nueva
necesita lecturas rastreadas con bloqueo; no reutiliza sus consultas AsNoTracking ni
su SaveChanges de creación porque romperían la unidad de trabajo.

1. Rechaza una unidad de trabajo con modificaciones pendientes y limpia únicamente
   instancias sin cambios para evitar una cabecera antigua en el ChangeTracker.
2. Abre una transacción ReadCommitted en MySQL/InnoDB.
3. Ejecuta SELECT de cabecera por PK con FOR UPDATE, parametrizado, antes de comprobar
   Draft. No compone Include/Single sobre ese SQL: lee la fila y después carga Items.
4. Bloquea los productos en orden de GUID y carga todos sus lotes. Cabecera/productos
   permanecen bloqueados hasta commit o rollback.
5. Modifica agregados, referencias de recepción y añade movimientos al mismo contexto.
6. Ejecuta un solo SaveChangesAsync y un solo CommitAsync. No devuelve 200 antes del commit.

Dos solicitudes para la MISMA operación no pueden validar Draft simultáneamente:
la segunda espera el bloqueo y, tras el commit de la primera, lee Confirmed y devuelve
409. Si la primera revierte, la segunda puede confirmar el Draft. No hay reintento
automático ni creación de movimientos antes de validar el estado bloqueado.
Productos compartidos entre operaciones distintas también se bloquean; Product.Version
sigue siendo token optimista compatible con los escritores existentes de Operations.
Todos los escritores deben respetar ese token y la transacción de producto/lotes.
Futuros editores de detalles deberán bloquear primero su cabecera.

Cualquier fallo previo a completar el commit dispone la transacción y revierte
cabecera, detalles, producto, lotes y movimientos; se limpia el ChangeTracker.
DbUpdateConcurrencyException, deadlock (1213), timeout de bloqueo (1205) y conflictos
de FK/unicidad se traducen a 409 en español. Un desbordamiento de capacidad de INT
también produce 409. Otros errores MySQL producen 503 genérico, sin mensaje interno.
Si se pierde la conexión durante el commit, el resultado puede ser incierto: consultar
la operación por GET antes de reintentar. El bloqueo/estado evita aplicar sus efectos
dos veces al reenviar el mismo GUID. Dos Draft diferentes son operaciones diferentes.

No hay nuevas columnas, índices, migraciones ni cambios en Shared. No se emplean
triggers, EnsureCreated ni SQL de actualización de existencias. La única SQL explícita
de la sesión es SELECT FOR UPDATE. Se requiere InnoDB en las tablas involucradas.

## Flujo manual completo en Scalar: 0 → 20 → 17

Iniciar Development con DefaultConnection en User Secrets. Abrir
http://localhost:5281/scalar. No utilizar una instancia antigua que todavía exponga
28 operaciones; detener/reiniciar la API compilada desde esta rama.
Los siguientes GUID son ejemplos de contrato: sustituir por IDs REALES obtenidos
en los GET y copiar los IDs devueltos en cada POST.

1. Iniciar sesión con un usuario activo y configurar Bearer; obtener una categoría existente. Crear un producto con
   POST /api/v1/products (el servidor inicia stock en cero):

```json
{
  "categoryId":"11111111-1111-1111-1111-111111111111",
  "name":"Producto de demostración comercial",
  "salePrice":2.50,
  "minimumStock":0
}
```

2. POST /api/v1/purchases, usando el token del creador y el producto:

```json
{
  "items":[
    {"productId":"22222222-2222-2222-2222-222222222222","lotId":null,"quantity":20,"unitAmount":1.20}
  ]
}
```

Debe devolver 201, estado Draft, total 24.00; stock sigue 0. Copiar purchaseId.

3. POST /api/v1/purchases/{purchaseId}/confirm sin cuerpo → 200. Ejemplo del recurso:

```json
{
  "id":"44444444-4444-4444-4444-444444444444",
  "userId":"33333333-3333-3333-3333-333333333333",
  "registeredAt":"2026-11-10T12:00:00",
  "status":"Confirmed",
  "total":24.00,
  "items":[
    {"id":"55555555-5555-5555-5555-555555555555","productId":"22222222-2222-2222-2222-222222222222","lotId":"66666666-6666-6666-6666-666666666666","quantity":20,"unitAmount":1.20,"subtotal":24.00}
  ]
}
```

Los timestamps existentes se interpretan en UTC. Copiar lotId; comprobar stock 20
y un movimiento Purchase +20. La fecha de la operación sigue siendo RegisteredAt,
no una fecha nueva de confirmación: el esquema no tiene ConfirmedAt.

4. POST /api/v1/sales con el lote recibido:

```json
{
  "items":[
    {"productId":"22222222-2222-2222-2222-222222222222","lotId":"66666666-6666-6666-6666-666666666666","quantity":3,"unitAmount":2.50}
  ]
}
```

Debe devolver 201 Draft, total 7.50; stock sigue 20. Copiar saleId.
POST /api/v1/sales/{saleId}/confirm sin cuerpo → 200 con el mismo formato de recurso,
status Confirmed y total 7.50. GET del producto/lote debe indicar 17 y debe existir
un movimiento Sale -3. Reporting incluye esta venta según su RegisteredAt UTC.

## Errores y pruebas manuales

| Caso | HTTP |
| --- | --- |
| Confirmación correcta | 200 |
| GUID inválido/vacío, producto/lote inválido, usuario/producto inactivo, sin detalles | 400 |
| Compra/venta inexistente | 404 |
| Ya Confirmed o Cancelled | 409 |
| Stock insuficiente, stock incoherente o conflicto concurrente | 409 |
| Infraestructura MySQL no disponible | 503 genérico |

Ejemplo de error:

```json
{"title":"Stock insuficiente del producto o lote para confirmar la venta.","status":409}
```

El framework puede añadir type y traceId. No se devuelven hashes ni cadenas de conexión.
Repetir confirm debe producir 409 sin variar stock ni movimientos. Una venta por
18 unidades cuando hay 17 debe producir 409 y seguir Draft. Para lote de otro producto,
la creación Draft ya rechaza 400 y la FK compuesta protege la base; las pruebas
unitarias también cubren referencias corruptas sin desactivar esas restricciones.
Cancelled se comprueba en pruebas de dominio, sin añadir un endpoint de cancelación.

Para dos confirmaciones simultáneas del mismo ID puede usarse desde una terminal:

```powershell
Add-Type -AssemblyName System.Net.Http
$client = [System.Net.Http.HttpClient]::new()
$url = 'http://localhost:5281/api/v1/sales/GUID-REAL-DEL-DRAFT/confirm'
$one = $client.PostAsync($url, $null)
$two = $client.PostAsync($url, $null)
[System.Threading.Tasks.Task]::WaitAll(@($one, $two))
[int]$one.Result.StatusCode
[int]$two.Result.StatusCode
$client.Dispose()
```

Se esperan 200 y 409, con un solo descuento. Verificar también dos ventas DIFERENTES
que individualmente caben pero juntas exceden stock: una confirma y otra queda Draft.

## Workbench: verificar entrada, salida y coherencia

```sql
USE bodego_db;
SET @product_id = '22222222-2222-2222-2222-222222222222';
SET @purchase_id = '44444444-4444-4444-4444-444444444444';
SET @sale_id = '77777777-7777-7777-7777-777777777777';

SELECT id, stock, version FROM products WHERE id = @product_id;
SELECT id, lot_code, expiration_date, quantity_available
FROM product_lots WHERE product_id = @product_id;
SELECT id, status, total FROM purchases WHERE id = @purchase_id;
SELECT * FROM purchase_items WHERE purchase_id = @purchase_id;
SELECT id, status, total FROM sales WHERE id = @sale_id;
SELECT * FROM sale_items WHERE sale_id = @sale_id;
SELECT product_id, lot_id, movement_type, quantity_delta, source_type, source_id
FROM inventory_movements WHERE source_id IN (@purchase_id,@sale_id)
ORDER BY created_at,id;
SELECT source_type, source_id, SUM(quantity_delta) AS delta
FROM inventory_movements WHERE source_id IN (@purchase_id,@sale_id)
GROUP BY source_type,source_id;
SELECT p.id,p.stock,COALESCE(SUM(l.quantity_available),0) AS suma_lotes
FROM products p LEFT JOIN product_lots l ON l.product_id=p.id
WHERE p.id=@product_id GROUP BY p.id,p.stock;
```

Para el flujo manual aislado se esperan +20 en Purchase, -3 en Sale, stock/suma 17.
No borrar movimientos para repetir una prueba. Crear otro producto identificado o
continuar con nuevas operaciones válidas, conservando historia.

## Pruebas automatizadas y resultado observado

Los ejecutables de pruebas se retiraron de esta entrega. Para verificar compilación:

```powershell
dotnet restore
dotnet build
```

Para comprobación funcional usar el flujo manual y las consultas SQL de esta guía,
con JWT válido y una base de demostración autorizada. No crear operaciones de prueba
sobre datos importantes. Los resultados descritos a continuación son históricos,
obtenidos antes del retiro de las suites; no implican que sigan disponibles para
volver a ejecutarlas desde este repositorio.

Se aprobaron **131 comprobaciones HTTP/MySQL reales**. Además del flujo manual,
prueba varios detalles, dos líneas sobre un mismo lote, errores 400/404/409, rechazo
de lote ajeno en Draft, entradas/salidas, incremento de Version y Reporting.
Inyecta un fallo de prueba DESPUÉS de SaveChanges para compra y venta; desde otro
contexto verifica restauración de Draft, lotId original, stock, lotes y movimientos.
El interceptor de fallos existe solo en el proyecto de pruebas, no en la API.

Para doble confirmación usa dos contextos/conexiones reales: mantiene la primera
cabecera bloqueada con una barrera de prueba, comprueba que la segunda espera,
libera la primera y exige un éxito/un conflicto y un único efecto. Esto se ejecutó
para compra y venta. También envía HTTP simultáneo para la misma compra y para dos
ventas distintas que compiten por todo el stock: resultados 200/409 y stock nunca negativo.

La suite completa continúa después del checkpoint 17: el primer producto termina
con 19 por las pruebas multidetalle y el segundo con 0 por la carrera de ventas.
No confundir esos resultados finales con el checkpoint intermedio 0→20→17 aprobado.
La ejecución histórica anterior a JWT usó el sufijo `e8403fc017524a8d9e0bc25573b7bfa0` y dejó:
- Producto flujo: `8782cc2f-cb0d-48dc-8dd8-1c9a732b0750`.
- Producto multidetalle/concurrencia: `5643747b-49fc-410a-a1da-9dc41d7ee804`.

También pasaron las suites existentes: 76 generales, 12 IAM, 63 Inventory, 48 Operations
y 29 Monitoring/Reporting en modo sin MySQL. La integración de Reporting aprobó
68 verificaciones sobre datos reales con cuatro ventas Confirmed, más cuatro
verificaciones con fixtures SQL virtuales; no escribió información.
Actualmente OpenAPI contiene **32 operaciones**: las 30 originales (ocho Commercial)
y login/me. La reanudación con JWT aprobó 131 comprobaciones comerciales reales
en `bodego_test_autenticacion_20261111200206_ea819d69`. Ver el informe completo
en [autenticación IAM](autenticacion-iam.md). No se creó una migración nueva.

La compilación habitual encontró un ejecutable anterior bloqueado; la regresión se
compiló en una salida aislada con 0 advertencias y 0 errores. La comprobación EF se
ejecutó también con una compilación Release nueva. Reiniciar Rider para cargar la API
actual antes de probar Scalar. No se publicó esta rama ni se abrió un PR automáticamente.

Pendiente: pruebas de carga sostenida, fallo real de red en commit, provocar deadlocks
reales y carreras con escritores de Operations. Los códigos de conflicto están
contemplados, pero no se afirma haber reproducido todas esas condiciones. Tampoco se
implementan reglas nuevas de vencimiento ni generación de alertas. La autenticación
y autorización de los POST comerciales sí están implementadas; los GET conservan
sus permisos anteriores. Revisar estos límites antes de producción.

Referencias de implementación: [bloqueos MySQL](https://dev.mysql.com/doc/refman/8.4/en/innodb-locking-reads.html)
y [transacciones EF Core](https://learn.microsoft.com/en-us/ef/core/saving/transactions).
