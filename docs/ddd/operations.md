# Operations: mermas, movimientos y conteos

Implementación sobre las seis tablas existentes; no añade columnas ni migraciones.
En Development, abrir Scalar en /scalar y seleccionar la etiqueta **Operations**.
Configurar MySQL como se indica en [tablas.md](../database/tablas.md).
Los identificadores de los ejemplos son GUID de ejemplo: sustituirlos por registros reales.
Las fechas se generan en UTC.

Los dos POST requieren JWT de un Administrador o Empleado activo.
Hacer login y configurar `Authorization: Bearer <token>` en Scalar/Axios.
El usuario y su rol se comprueban en MySQL en cada petición; ver
[autenticación IAM](autenticacion-iam.md).
El GUID del actor se obtiene de la identidad validada, nunca del formulario.
Los GET conservan su acceso anterior; no se agregaron rutas ni migraciones.

## Endpoints implementados

| Método y ruta | Resultado |
| --- | --- |
| GET /api/v1/inventory-movements | 200, arreglo de movimientos históricos; vacío: [] |
| POST /api/v1/wastes | 201, merma registrada con stock y movimientos |
| GET /api/v1/wastes | 200, arreglo de mermas; vacío: [] |
| POST /api/v1/inventory-counts | 201, conteo confirmado con detalles, sin ajuste |
| GET /api/v1/inventory-counts/{id} | 200 con detalles; 404 si no existe |

Los GET usan repositorios EF y AsNoTracking; no guardan cambios. Listados ordenados
por fecha descendente e identificador, sin paginación en este alcance.
Producto y lote se exponen por sus identificadores, sin añadir campos al modelo.

## Registrar merma desde Scalar

JSON exacto de CreateWasteResource:

```json
{
  "productId": "11111111-1111-1111-1111-111111111111",
  "lotId": "22222222-2222-2222-2222-222222222222",
  "quantity": 2,
  "reason": "Daño"
}
```

ProductId, Quantity y Reason son obligatorios. LotId puede omitirse o ser null.
No enviar UserId: el recurso rechaza propiedades desconocidas y devuelve 400.
Los GUID vacíos se rechazan. La cantidad debe ser un entero positivo; el usuario debe
existir y estar activo, igual que el producto. El lote indicado debe pertenecer al producto.
Reason sigue el modelo aprobado: texto no vacío de hasta 255 caracteres después de
recortar espacios, por ejemplo Daño, Vencimiento o Pérdida. No existe enum ni catálogo
cerrado de motivos; se conserva texto libre válido.

Respuesta 201 y Location /api/v1/wastes (colección disponible para consultar la merma):

```json
{
  "id": "44444444-4444-4444-4444-444444444444",
  "productId": "11111111-1111-1111-1111-111111111111",
  "lotId": "22222222-2222-2222-2222-222222222222",
  "userId": "33333333-3333-3333-3333-333333333333",
  "quantity": 2,
  "reason": "Daño",
  "registeredAt": "2026-11-09T15:00:00Z"
}
```

GET /api/v1/wastes devuelve un arreglo de esos recursos.
GET /api/v1/inventory-movements devuelve recursos como:

```json
[
  {
    "id": "55555555-5555-5555-5555-555555555555",
    "productId": "11111111-1111-1111-1111-111111111111",
    "lotId": "22222222-2222-2222-2222-222222222222",
    "userId": "33333333-3333-3333-3333-333333333333",
    "movementType": "Waste",
    "quantityDelta": -2,
    "sourceType": "Waste",
    "sourceId": "44444444-4444-4444-4444-444444444444",
    "createdAt": "2026-11-09T15:00:00Z"
  }
]
```

### Descuento, atomicidad y concurrencia

Application coordina repositorio y puerto IOperationsSession; Infrastructure usa el
DbContext compartido sin invocar repositorios privados de Inventory o IAM.
OperationsSession abre una transacción RepeatableRead en MySQL/InnoDB. Carga el
Product completo con todos sus lotes y comprueba la igualdad stock = suma de lotes.
WasteStockPolicy valida disponibilidad antes de modificar y llama Product.RemoveStock.
Con lote explícito, toda la cantidad sale de ese lote. Sin lote, distribuye por ReceivedAt
ascendente y GUID como desempate. No selecciona automáticamente por fecha de vencimiento.
Una merma sin lote conserva lotId null; sus movimientos identifican cada lote afectado.

Cada lote descontado genera un InventoryMovement inmutable con delta negativo,
MovementType Waste, SourceType Waste y SourceId igual al GUID de la merma.
La suma de deltas es el negativo de Quantity. Se guarda merma, producto, lotes y
movimientos con un solo SaveChangesAsync y un solo commit de transacción.
Cualquier excepción, incluso posterior a SaveChanges, provoca rollback al disponer
la transacción. No se entrega 201 antes de completar el commit.

Product.Version es el token optimista ya existente y RemoveStock lo incrementa.
Si dos solicitudes leen la misma versión, una actualización pierde la concurrencia
o MySQL detecta un deadlock: se revierte toda la solicitud perdedora y devuelve 409.
También se traducen timeout de bloqueo, referencia concurrentemente eliminada o
clave duplicada a 409. No se reintenta automáticamente con un stock desactualizado.
Esto exige InnoDB y que todos los escritores de Inventory respeten Version y sus
agregados; SQL externo que ignore ese contrato queda fuera de esta protección.

## Registrar conteo físico

JSON exacto de CreateInventoryCountResource:

```json
{
  "notes": "Conteo al cierre",
  "items": [
    {
      "productId": "11111111-1111-1111-1111-111111111111",
      "lotId": "22222222-2222-2222-2222-222222222222",
      "countedQuantity": 7
    }
  ]
}
```

Items es obligatorio. No enviar UserId: se toma del JWT y enviarlo produce 400.
Items debe contener al menos un detalle no nulo;
ProductId y CountedQuantity son obligatorios en cada detalle. LotId y Notes son
opcionales. Notes, si se envía, admite de 1 a 500 caracteres después de recortar.
No se aceptan cantidades negativas, productos inexistentes, lotes ajenos, detalles
duplicados ni mezclar el total de un producto con sus lotes en el mismo conteo.

El servidor obtiene ExpectedQuantity del stock actual del producto o del lote;
no lo recibe del cliente. Guarda Difference = CountedQuantity - ExpectedQuantity
y confirma el conteo. La lectura y escritura están en una transacción RepeatableRead:
el esperado representa la fotografía leída, no una reserva de stock.
No modifica Product, ProductLot ni Version y no crea movimientos CountAdjustment.
La cabecera y todos los detalles se guardan juntos.

Respuesta 201 con Location /api/v1/inventory-counts/{id}; GET de esa ruta devuelve
el mismo recurso, por ejemplo si el lote tenía 8 unidades:

```json
{
  "id": "66666666-6666-6666-6666-666666666666",
  "userId": "33333333-3333-3333-3333-333333333333",
  "countedAt": "2026-11-09T16:00:00Z",
  "status": "Confirmed",
  "notes": "Conteo al cierre",
  "items": [
    {
      "id": "77777777-7777-7777-7777-777777777777",
      "productId": "11111111-1111-1111-1111-111111111111",
      "lotId": "22222222-2222-2222-2222-222222222222",
      "expectedQuantity": 8,
      "countedQuantity": 7,
      "difference": -1
    }
  ]
}
```

## Casos de error

| Solicitud o condición | HTTP |
| --- | --- |
| Token ausente, alterado o vencido; usuario desactivado | 401 |
| Identidad autenticada sin permiso para la operación | 403 |
| userId enviado en el cuerpo o propiedad desconocida de la cabecera | 400 |
| JSON mal formado, tipos incorrectos, GUID inválido o vacío | 400 |
| Campo obligatorio omitido o detalle nulo | 400 |
| Producto inexistente/inactivo en merma | 400 |
| Producto inexistente en conteo o lote inexistente/ajeno | 400 |
| Merma cero/negativa, motivo vacío/largo, conteo negativo | 400 |
| Detalles duplicados, sin detalles o total mezclado con lotes | 400 |
| Stock insuficiente del producto o lote | 409 |
| Stock distinto de suma de lotes | 409 |
| Conflicto de versión, deadlock o timeout de bloqueo | 409 |
| Conteo con GUID válido que no existe | 404 |

Los errores de negocio usan ProblemDetails con status y title en español:
```json
{
  "title": "Stock insuficiente para registrar la merma.",
  "status": 409
}
```
El framework puede añadir type y traceId. Los errores del JSON usan
ValidationProblemDetails con title en español y errors por campo, también en español.
Fallos de conexión/servidor son errores de infraestructura; no se presentan como
validaciones ni se devuelven cadenas de conexión.

## Workbench: comprobar datos reales

Usar una base local con la migración existente aplicada y sin escritores concurrentes
durante las comparaciones manuales. No crear ni alterar tablas para Operations.

```sql
USE bodego_db;
SELECT DATABASE(), @@hostname, @@port, VERSION(), @@default_storage_engine;
SELECT id, name, active FROM users WHERE active = 1;
SELECT id, name, stock, version FROM products WHERE active = 1;
SELECT id, product_id, lot_code, quantity_available FROM product_lots;

-- Sustituir con los GUID seleccionados; conservar esta sesión de Workbench.
SET @product_id = '11111111-1111-1111-1111-111111111111';
SET @lot_id = '22222222-2222-2222-2222-222222222222';
SELECT stock, version INTO @stock_before, @version_before
FROM products WHERE id = @product_id;
SELECT quantity_available INTO @lot_before FROM product_lots WHERE id = @lot_id;
SELECT COUNT(*) INTO @wastes_before FROM wastes WHERE product_id = @product_id;
SELECT COUNT(*) INTO @movements_before FROM inventory_movements WHERE product_id = @product_id;
```

Ejecutar POST de merma de 2 unidades con ese producto y lote en Scalar. Copiar el
id de la respuesta y ejecutar:

```sql
SET @waste_id = '44444444-4444-4444-4444-444444444444';
SELECT id, stock, version, @stock_before AS antes,
       @stock_before - stock AS descuento
FROM products WHERE id = @product_id;
SELECT quantity_available, @lot_before - quantity_available AS descuento_lote
FROM product_lots WHERE id = @lot_id;
SELECT * FROM wastes WHERE id = @waste_id;
SELECT * FROM inventory_movements
WHERE source_type = 'Waste' AND source_id = @waste_id;
SELECT w.id, w.quantity, SUM(m.quantity_delta) AS delta,
       SUM(m.quantity_delta) = -w.quantity AS coherente
FROM wastes w JOIN inventory_movements m
  ON m.source_type = 'Waste' AND m.source_id = w.id
WHERE w.id = @waste_id GROUP BY w.id, w.quantity;
SELECT p.id, p.stock, COALESCE(SUM(l.quantity_available), 0) AS suma_lotes
FROM products p LEFT JOIN product_lots l ON l.product_id = p.id
WHERE p.id = @product_id GROUP BY p.id, p.stock;
```

Se espera descuento 2 en producto y lote, Version incrementada, merma y delta -2.
Repetir con lotId null y cantidad que abarque varios lotes: una merma y varios
movimientos cuya suma sea negativa de la cantidad; el stock debe seguir igual a
la suma de lotes. Probar stock insuficiente y lote ajeno: sin cambios en tablas.

Antes del POST de conteo tomar otra fotografía:

```sql
SELECT stock, version INTO @count_stock_before, @count_version_before
FROM products WHERE id = @product_id;
SELECT quantity_available INTO @count_lot_before FROM product_lots WHERE id = @lot_id;
SELECT COUNT(*) INTO @count_moves_before FROM inventory_movements WHERE product_id = @product_id;
```

Después del POST, copiar su identificador:

```sql
SET @count_id = '66666666-6666-6666-6666-666666666666';
SELECT * FROM inventory_counts WHERE id = @count_id;
SELECT *, counted_quantity - expected_quantity AS diferencia_calculada
FROM inventory_count_items WHERE count_id = @count_id;
SELECT stock = @count_stock_before AS stock_intacto,
       version = @count_version_before AS version_intacta
FROM products WHERE id = @product_id;
SELECT quantity_available = @count_lot_before AS lote_intacto
FROM product_lots WHERE id = @lot_id;
SELECT COUNT(*) = @count_moves_before AS movimientos_intactos
FROM inventory_movements WHERE product_id = @product_id;
```

Todos los indicadores deben ser 1. GET del conteo debe coincidir con cabecera y detalles.

## Pruebas automatizadas

```powershell
dotnet restore
dotnet build
dotnet run --project tests/BodeGo.Checks
dotnet run --project tests/BodeGo.IamChecks
dotnet run --project tests/BodeGo.InventoryChecks
dotnet run --project tests/BodeGo.OperationsChecks
```

Si dotnet no está en PATH, añadir temporalmente la instalación local del SDK.
OperationsChecks ejecuta reglas, servicios, consultas, metadatos/SQL del proveedor,
HTTP sobre Kestrel con puertos dobles y las cinco etiquetas OpenAPI. Comprueba
productos/usuarios inexistentes, lote ajeno, cantidades inválidas, motivo, stock
insuficiente, descuentos por lote y distribuidos, trazabilidad y conteos sin cambios
de stock. Comprueba 200/201/400/404/409 y JSON/detalles incompletos.

Resultado actualizado: OperationsChecks aprobó 52 comprobaciones con HTTP/JWT
y dobles explícitos de persistencia/identidad. Conserva los escenarios anteriores
y agrega 401 sin token y rechazo de userId. AuthenticationChecks aprobó
129 comprobaciones sin MySQL y 110 comprobaciones HTTP/MySQL reales, incluyendo
mermas/conteos autenticados. OpenAPI conserva las 32 operaciones.

### Integración optativa de rollback y concurrencia real

Preparar por separado una base **desechable** cuyo nombre comience por
bodego_operations_test, con la migración inicial existente aplicada y motor InnoDB.
Configurar BODEGO_OPERATIONS_TEST_CONNECTION fuera de Git (no imprimirla).

```powershell
dotnet run --project tests/BodeGo.OperationsChecks -- --mysql
```

La prueba exige una base local con prefijo bodego_operations_test o
bodego_test_autenticacion_. Inserta datos propios
identificados como Operations test, sin borrar historia ni crear tablas. Inyecta
un fallo justo después de SaveChanges, verifica desde otro DbContext que producto,
lote, merma y movimientos se revirtieron, y luego coordina dos solicitudes que
leen el mismo stock 10 e intentan descontar 6 cada una. Debe haber un éxito y un
conflicto, stock y lote finales 4, una merma y delta -6. Finalmente registra/consulta
un conteo y verifica stock y movimientos intactos. Los datos quedan en la base
desechable para revisión. Esta variante se ejecutó correctamente desde la suite
de autenticación en la misma base aislada que sus pruebas HTTP:
`bodego_test_autenticacion_20261111210853_a4b0cfea`.

Para crear una base aislada nueva, aplicar sólo la migración existente y ejecutar
IAM, Commercial y Operations con la misma conexión:

```powershell
dotnet run --project tests/BodeGo.AuthenticationChecks -- --mysql
```

La suite prepara y restaura `BODEGO_OPERATIONS_TEST_CONNECTION` sólo en memoria.
La prueba optativa acepta también el prefijo local `bodego_test_autenticacion_`.
No escribe en `bodego_db`, no elimina bases anteriores y deja las fixtures para revisar.
Los servicios directos de rollback/concurrencia usan un actor de prueba explícito;
las pruebas HTTP de mermas/conteos usan JWT y usuarios reales de esa base aislada.

El flujo HTTP comprobó: compra 20, venta 3, merma 2 → stock/lote 15;
conteo físico 14 → diferencia -1, sin cambiar stock ni crear movimientos.
La merma y sus movimientos guardan al Empleado autenticado; el conteo guarda al
Administrador autenticado. POST con userId, token inválido o usuario desactivado
se rechazaron sin modificar el inventario.

## Coordinación pendiente con el equipo

- Inventory debe proporcionar existencias mediante su flujo de recepción de lotes.
  Un producto recién creado tiene stock cero; Operations no inventa stock inicial.
- El puerto IOperationsSession utiliza el agregado público Product y consultas
  de existencia de IAM mediante EF en Infrastructure. No cambia esos contextos.
  Todos los futuros escritores deben respetar Version y la atomicidad de lotes.
- Commercial confirma compras/ventas de forma atómica y autenticada. No duplicar
  sus movimientos desde Operations ni modificar las reglas de recepción/FIFO.
- IAM acredita al actor; Administrador y Empleado registran operaciones propias.
  Incluso un Administrador no puede elegir otro actor mediante un Command.
  El primer Administrador habitual necesita inicialización manual autorizada;
  no se ejecutó como parte de estas pruebas.
- No hay clave de idempotencia en el esquema/contrato aprobado. Dos POST válidos
  enviados nuevamente son dos operaciones distintas. Tras una respuesta perdida,
  consultar historial antes de reenviar; acordar idempotencia antes de producción.
- No se implementan conciliaciones ni ajustes automáticos a partir de conteos.
- Pendiente integrar Vue, ensayar carga sostenida y carreras combinadas con otros
  escritores de inventario. Las pruebas reales por módulo ya pasaron.
