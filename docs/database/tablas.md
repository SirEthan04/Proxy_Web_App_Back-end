# Inventario aprobado de tablas MySQL

El modelo EF y la migración `CrearBaseDatosInicial` definen estas 14 tablas.
La migración está preparada, pero NO se ha aplicado: no había conexión segura
configurada en User Secrets ni en variables de entorno. No se verificaron tablas
físicas ni datos en MySQL. Una tabla no implica un Aggregate Root.

| Contexto propietario | Tabla | Responsabilidad |
| --- | --- | --- |
| IAM | roles | Catálogo de roles |
| IAM | users | Usuarios |
| Inventory | categories | Catálogo de categorías |
| Inventory | products | Productos |
| Inventory | product_lots | Lotes de productos |
| Commercial | purchases | Cabeceras de compras |
| Commercial | purchase_items | Detalles de compras |
| Commercial | sales | Cabeceras de ventas |
| Commercial | sale_items | Detalles de ventas |
| Operations | inventory_movements | Historial inmutable de movimientos |
| Operations | wastes | Mermas |
| Operations | inventory_counts | Cabeceras de conteos |
| Operations | inventory_count_items | Detalles de conteos |
| Monitoring | alerts | Alertas persistidas y su atención |

Reporting utilizará consultas sobre estas tablas para Dashboard y reportes;
no añade tablas inicialmente. Shared no posee tablas de negocio.

## Modelo DDD y relaciones

- Aggregate Roots: User, Product, Purchase, Sale, Waste, InventoryCount y Alert.
- Catálogos: Role y Category. Role utiliza un INT asignado explícitamente, sin enum
  paralelo; no se insertan roles ni usuarios automáticamente.
- Entidades hijas: ProductLot pertenece a Product; PurchaseItem a Purchase;
  SaleItem a Sale; InventoryCountItem a InventoryCount.
- InventoryMovement tiene identidad y conserva un registro histórico inmutable.
- Value Objects: EmailAddress; ProductName, Money y StockLevel en Inventory;
  Quantity y Money en Commercial. Se convierten a columnas escalares, sin tablas extra.
- Enums persistidos como texto: CommercialStatus (Draft, Confirmed, Cancelled),
  InventoryCountStatus (Draft, Confirmed, Cancelled), MovementType (Purchase, Sale,
  Waste, CountAdjustment), AlertType (LowStock, OutOfStock, Expiration) y
  AlertStatus (Pending, Attended).

Hay 24 claves foráneas, todas con eliminación RESTRICT. Role tiene usuarios;
Category tiene productos; Product tiene lotes. User identifica al responsable de
compras, ventas, movimientos, mermas y conteos, y opcionalmente a quien atiende Alert.
Las cabeceras Purchase, Sale e InventoryCount tienen sus detalles. Cada detalle,
movimiento, merma y alerta referencia Product por identificador.

Las seis tablas con lot_id opcional usan FK compuesta `(lot_id, product_id)` hacia
`product_lots(id, product_id)`: un lote de otro producto es rechazado por MySQL.
La clave alternativa del lote contiene ambas columnas. No hay navegaciones de
objetos entre agregados de diferentes contextos; los vínculos SQL se configuran
en Infrastructure.

## Tipos y protección de datos

Todos los Guid, incluidas las FK opcionales, usan conversión GuidToStringConverter
y VARCHAR(36), formato canónico con guiones. Es el equivalente textual explícito
de CHAR(36): se eligió porque el proveedor oficial 10.0.12 rechazó el mapeo CHAR(36)
durante la validación del modelo, mientras VARCHAR(36) se validó y generó SQL.
No se cambió de proveedor. PK y FK conservan exactamente el mismo tipo.

Los importes son DECIMAL(12,2), las cantidades INT, las fechas de vencimiento DATE,
las marcas temporales DATETIME(6), y los booleanos tinyint(1). Las fechas del dominio
se generan en UTC; MySQL DATETIME no guarda zona horaria y debe interpretarse como UTC.

Índices únicos: roles.name, users.email, categories.name y producto/código de lote.
Hay índices de FK, búsqueda por fecha/estado y origen del movimiento. El índice
único de detalle de conteo incluye count_id/product_id/lot_id; MySQL permite repetir
NULL en índices únicos, por lo que el dominio valida también los duplicados sin lote.
La comparación de textos en MySQL depende de la collation configurada en el servidor.

Los CHECK protegen cantidades, importes, subtotales, diferencias, estados válidos,
nombres no vacíos, signo del movimiento y atención/vencimiento de alertas. Se
mantienen junto con las FK RESTRICT, sin acciones SET NULL ni UPDATE CASCADE.
Usar MySQL 8.0.16 o posterior para CHECK efectivos y motor InnoDB para las FK.
Verificar además que la versión del servidor esté soportada por Connector/NET.

Product comienza sin stock. ReceiveLot incrementa el lote y el stock, y RemoveStock
reduce ambos sin permitir negativos; cada operación aumenta Version, configurado
como token de concurrencia optimista. Cargar siempre el agregado con TODOS sus lotes
antes de modificarlo. Una recepción adicional usa otro código de lote.
Purchase y Sale calculan subtotales y total y no permiten añadir detalles ni cancelar
una operación ya confirmada. Sus colecciones no son modificables desde fuera.
Cargar siempre TODOS los detalles antes de agregar o confirmar una operación.
Los conteos tampoco se modifican después de confirmarse.

SaveChanges y SaveChangesAsync rechazan editar/eliminar InventoryMovement y eliminar
compras, ventas, mermas, conteos y sus detalles. RESTRICT impide eliminar referencias.
Estas protecciones de aplicación no impiden SQL directo ni ExecuteUpdate/Delete:
no utilizar esos mecanismos para modificar historia; limitar permisos del usuario
de ejecución cuando se prepare el despliegue. No se utilizan triggers.

## Conexión local desde la terminal PowerShell de Rider

Program.cs registra BodeGoDbContext con UseMySQL y DefaultConnection. appsettings.json
contiene una cadena vacía; el identificador de User Secrets está en el csproj.
Cada integrante configura su propia conexión fuera de Git. No cambiar launchSettings.

Desde la raíz de la solución, este bloque pide la contraseña de forma oculta y
envía JSON por stdin a User Secrets, sin poner la contraseña en el historial del shell:

```powershell
$localCredential = Get-Credential -UserName root -Message 'Clave local de MySQL'
$localConnection = [System.Data.Common.DbConnectionStringBuilder]::new()
$localConnection['Server'] = 'localhost'
$localConnection['Port'] = 3306
$localConnection['Database'] = 'bodego_db'
$localConnection['User ID'] = 'root'
$localConnection['Password'] = $localCredential.GetNetworkCredential().Password
@{ 'ConnectionStrings:DefaultConnection' = $localConnection.ConnectionString } |
    ConvertTo-Json -Compress | dotnet user-secrets set --project BodeGo.API
$localConnection.Clear()
Remove-Variable localConnection,localCredential
$env:ASPNETCORE_ENVIRONMENT = 'Development'
```

User Secrets es almacenamiento local fuera del repositorio, no una caja fuerte
cifrada. No compartir su archivo ni ejecutar `user-secrets list` en capturas.
La aplicación carga secretos en Development. Revisar que no exista una variable
ConnectionStrings__DefaultConnection apuntando a otro servidor: tiene precedencia.
La fábrica de diseño carga secretos y variables y permite generar el modelo con
un destino local sin contraseña si falta conexión; eso no verifica ni aplica nada.

## Migración y comprobación previa

```powershell
dotnet tool restore
dotnet restore
dotnet build
dotnet ef migrations has-pending-model-changes --project BodeGo.API
```

La herramienta dotnet-ef 10.0.12 está fijada en dotnet-tools.json. La migración,
su Designer y el Snapshot están en Shared/Infrastructure/Persistence/EFC/Migrations.
El SQL generado para revisión está en `docs/database/CrearBaseDatosInicial.sql`.
No ejecutar simultáneamente el SQL manual y database update: utilizar EF para
mantener el historial. Para regenerar sólo el script:

```powershell
dotnet ef migrations script 0 CrearBaseDatosInicial --project BodeGo.API --output docs/database/CrearBaseDatosInicial.sql
```

Up crea exactamente las 14 tablas, claves, CHECK e índices. No contiene DROP,
DELETE ni TRUNCATE. Down es el rollback estándar que elimina esas tablas nuevas:
no ejecutarlo sobre datos que deban conservarse. No se usa EnsureCreated ni se
aplican migraciones automáticamente al arrancar.

Antes de aplicar, abrir en Workbench la conexión LOCAL localhost:3306, seleccionar
bodego_db y ejecutar:

```sql
USE bodego_db;
SELECT DATABASE(), @@hostname, @@port, CURRENT_USER(), VERSION(), @@default_storage_engine;
SHOW FULL TABLES;
```

Confirmar que es la instancia de desarrollo propia, InnoDB y versión compatible.
En esta primera instalación se espera una base sin tablas. Si aparece cualquier
tabla o historial previo, detenerse y revisar sus datos y migraciones; no borrar ni
asumir que está vacía basándose en TABLE_ROWS de information_schema.
Después de configurar la misma conexión segura y confirmar esas condiciones:

```powershell
dotnet ef database update CrearBaseDatosInicial --project BodeGo.API
```

No se ejecutó este comando en la preparación actual por falta de conexión segura.
MySQL puede hacer commit implícito de DDL: si falla a mitad, revisar el esquema antes
de reintentar, sin eliminar tablas automáticamente.

## Revisar desde Workbench después de aplicar

```sql
USE bodego_db;
SHOW TABLES;
SELECT * FROM __EFMigrationsHistory;
SELECT COUNT(*) AS business_tables
FROM information_schema.tables
WHERE table_schema = 'bodego_db' AND table_type = 'BASE TABLE'
  AND table_name <> '__EFMigrationsHistory';

SELECT k.table_name, k.constraint_name, k.column_name,
       k.referenced_table_name, k.referenced_column_name, k.ordinal_position,
       r.delete_rule, r.update_rule
FROM information_schema.key_column_usage k
JOIN information_schema.referential_constraints r
  ON r.constraint_schema = k.constraint_schema
 AND r.table_name = k.table_name AND r.constraint_name = k.constraint_name
WHERE k.table_schema = 'bodego_db' AND k.referenced_table_name IS NOT NULL
ORDER BY k.table_name, k.constraint_name, k.ordinal_position;

SHOW CREATE TABLE products;
SHOW CREATE TABLE sale_items;
SELECT table_name, constraint_name, constraint_type
FROM information_schema.table_constraints
WHERE table_schema = 'bodego_db'
ORDER BY table_name, constraint_type;
```

Se esperan 14 tablas de negocio más __EFMigrationsHistory, 24 FK y 30 filas en la
consulta de columnas FK porque seis FK son compuestas. También puede usarse
Database > Reverse Engineer en Workbench para visualizar el diagrama de relaciones.

## Pendiente antes de los endpoints

- Coordinar en Application, en UNA transacción, la confirmación comercial, el cambio
  de stock/lotes y el movimiento histórico, igual para mermas y ajustes de conteo.
- Manejar DbUpdateConcurrencyException recargando el agregado y revisando la
  operación; no reintentar a ciegas. Evitar registros duplicados ante reenvíos.
- Validar existencia y actividad de usuario, rol, categoría y producto; aplicar
  autorizaciones, generar hashes seguros y no devolver password_hash en GET.
- Verificar disponibilidad/vencimiento de lotes, origen del movimiento y coherencia
  entre stock y suma de lotes. Al crear un producto con stock inicial, registrarlo
  mediante lotes y movimientos en la misma transacción.
- Decidir cómo registrar ajustes positivos de conteo en un lote existente: el dominio
  actual recibe nuevos lotes y descuenta stock; no hay aún un flujo completo de ajuste.
- Asegurar total = suma de detalles y stock = suma de lotes al persistir; un CHECK
  no consulta otras filas. Prohibir modificaciones externas a agregados, cargar sus
  colecciones completas y comprobarlos dentro de la transacción.
- Validar duplicados y concurrencia de conteos, incluido el caso lot_id NULL y no
  mezclar conteo de producto completo con conteos parciales de sus lotes.
- Definir umbrales, anticipación de vencimiento y deduplicación de alertas desde
  Application. Alert sólo representa el estado y controla su atención.
- Añadir pruebas de integración sobre MySQL real para transacciones, restricciones,
  materialización de Value Objects, concurrencia y consultas paginadas de los GET.

Los IDs numéricos/alfanuméricos de JSON Server no se convierten automáticamente.
En una importación futura crear un mapa externo por contexto `id_origen -> Guid`,
generar primero los principales y luego resolver sus referencias. Conservar ese
mapa como artefacto de importación seguro, fuera de las 14 tablas aprobadas, y adaptar
los contratos Vue a identificadores string. No importar ni cambiar el frontend aquí.

Referencias: [proveedor oficial](https://dev.mysql.com/doc/connector-net/en/connector-net-entityframework-core.html),
[migraciones EF](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing),
[CHECK en MySQL](https://dev.mysql.com/doc/refman/8.4/en/create-table-check-constraints.html).
