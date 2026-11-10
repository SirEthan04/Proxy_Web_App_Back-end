# Monitoring y Reporting

## Alcance y dependencias

Esta entrega implementa cuatro consultas, sin escrituras ni nuevas tablas/migraciones.
Se preparó desde develop `c9e6694`, que ya incluye IAM, Inventory, Commercial y
Operations. Los 34 commits heredados al actualizar la rama no son avances de esta
entrega; los siete commits nuevos se comparan contra esa base de develop.

Monitoring reutiliza Alert, AlertType, AlertStatus y su configuración EF existente.
Reporting no tiene Domain ni agregados: usa Queries, Read Models y contratos de
lectura en Application. No accede a repositorios privados de otros contextos.
Sus repositorios de lectura consultan las tablas aprobadas mediante EF Core.
No se modifica Shared ni BodeGoDbContext. Program.cs solo registra dos extensiones.

## Endpoints y parámetros

| Método y ruta | Parámetros | Resultados |
| --- | --- | --- |
| GET /api/v1/alerts | Ninguno | 200, lista incluso si está vacía |
| GET /api/v1/alerts/{id} | GUID no vacío | 200, 400 por ID inválido, 404 si no existe |
| GET /api/v1/reports/sales-summary | from y to opcionales, yyyy-MM-dd | 200, 400 por fechas inválidas |
| GET /api/v1/reports/top-products | from, to; limit opcional, por defecto 10, entre 1 y 100 | 200, 400 por parámetros inválidos |

Los parámetros mal formados, los rangos invertidos y los IDs inválidos devuelven
ProblemDetails con mensajes en español. Las etiquetas de Scalar son Monitoring y
Reporting. No se cambia la configuración ni las rutas de los otros contextos.

### Fechas e importes

Los dos reportes filtran por sales.registered_at, interpretado en UTC, como lo crea
Sale. from incluye las 00:00 UTC del día indicado; to incluye el día completo hasta
antes de las 00:00 UTC del siguiente. Un filtro omitido no limita ese extremo.
El día máximo 9999-12-31 no necesita límite superior porque DATETIME no admite
fechas posteriores. Se rechazan años anteriores a 1000, fuera del rango de MySQL.
El rango de un solo día es válido. No se convierten implícitamente fechas a Lima;
el equipo debe acordar un filtro horario local diferente si lo necesita después.

Los importes se suman como decimal, sin redondeos adicionales ni conversión de moneda.
El esquema no guarda moneda: todos los importes deben pertenecer a una misma moneda
acordada por el equipo (PEN para BodeGo). La respuesta no inventa una columna currency.

### Respuestas JSON

Si no hay registros, las listas devuelven `[]` y sales-summary devuelve:

```json
{"salesCount":0,"totalAmount":0.00}
```

Ejemplos ilustrativos del contrato cuando existan registros, NO datos insertados
ni resultados que se afirmen existentes en la base:

```json
{
  "id":"11111111-1111-1111-1111-111111111111",
  "productId":"22222222-2222-2222-2222-222222222222",
  "lotId":null,
  "attendedByUserId":null,
  "alertType":"LowStock",
  "status":"Pending",
  "message":"Stock por debajo del mínimo",
  "createdAt":"2026-11-10T10:00:00Z",
  "attendedAt":null
}
```

GET alerts devuelve una lista de esos recursos; GET por ID devuelve un solo recurso.
Se conservan los nombres de enums persistidos, sin crear estados nuevos. Las marcas
DATETIME del recurso se identifican como UTC al serializarse.

```json
{"salesCount":2,"totalAmount":19.50}
```

```json
[
  {"productId":"22222222-2222-2222-2222-222222222222","productName":"Producto de ejemplo","quantitySold":7}
]
```

## Implementación de las consultas

- AlertsController → Query → AlertQueryService → IAlertRepository → AlertRepository
  → DbContext.Alerts. Consulta AsNoTracking, ordenada por CreatedAt descendente e Id
  ascendente para desempates. No crea, atiende ni cambia alertas.
- ReportsController → Queries → QueryServices → contratos de lectura → repositorios
  EF. Los servicios validan rango y límite; los ensambladores construyen Resources.
- SalesSummaryRepository ejecuta COUNT y COALESCE(SUM(total), 0) sobre sales con
  status Confirmed. No hace JOIN de detalles que pudiera duplicar el total de cabecera.
- TopProductsRepository une sale_items, sales y products, filtra Confirmed, agrupa
  por ID/nombre del producto y suma quantity, incluso cuando un producto tiene
  varios detalles/lotes. Orden descendente de cantidad y desempate por GUID ascendente.
  Usa el nombre actual de Product, porque no hay nombre histórico en SaleItem.
- EF Database.SqlQueryRaw proyecta tipos de lectura sin registrarlos como entidades.
  El SQL es constante; fechas, estado y límite viajan como MySqlParameter tipados,
  nunca concatenados desde la petición. Esto permite agregar las columnas escalares
  que EF mapea como Value Objects sin acceder a sus miembros no traducibles en LINQ.
- El resumen devuelve long y decimal; el ranking usa long para las cantidades.
  No hay SaveChanges, comandos de escritura ni cambios de existencias en estos módulos.

## Disponibilidad de datos

No existe aún un generador de alertas: Monitoring solo consulta las guardadas.
Stock bajo no crea una alerta automáticamente. Operations no genera alertas en esta
entrega. No se deducen alertas a partir de stock para aparentar que están persistidas.

Commercial está integrado, pero su Application no expone confirmación de ventas.
Crear una venta Draft no hace que aparezca en estos reportes. Aunque Sale tiene
Confirm como método de dominio, falta el flujo transaccional completo de Commercial.
Esta entrega no lo implementa ni cambia estados con SQL. Operations y Commercial
deberán coordinar inventario/movimientos cuando implementen esas funcionalidades.

## Pruebas reproducibles

Desde la raíz, con .NET 10:

```powershell
dotnet restore
dotnet build
dotnet run --project tests/BodeGo.Checks
dotnet run --project tests/BodeGo.IamChecks
dotnet run --project tests/BodeGo.InventoryChecks
dotnet run --project tests/BodeGo.OperationsChecks
dotnet run --project tests/BodeGo.MonitoringReportingChecks
```

El último comando sin argumentos ejecuta pruebas unitarias con dobles explícitos:
alerta existente/ausente, lista vacía, GUID, mapeo UTC, rangos, formato, límites y
contratos de controladores. Eso no demuestra acceso real a MySQL.

Con DefaultConnection mediante User Secrets y la migración común aplicada, iniciar
la API en una terminal y ejecutar la integración en otra:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project BodeGo.API --launch-profile http
# Otra terminal, mismo entorno Development:
dotnet run --project tests/BodeGo.MonitoringReportingChecks -- --integration http://localhost:5281
```

La integración solo admite API local y bodego_db en localhost/127.0.0.1:3306.
No imprime credenciales ni crea datos. Compara respuestas con ventas/detalles reales,
comprueba OpenAPI, códigos HTTP y conserva el stock/versión de los productos.
Después ejecuta el MISMO SQL de los repositorios contra CTE virtuales de prueba
que sombrean los nombres sales/sale_items/products solo durante esos SELECT.
Estas fixtures conocidas prueban Draft/Cancelled/Confirmed, suma 19.50, cantidades
7 y 5, orden, límite y caso vacío. No son ventas reales, no pasan por endpoints
y no se insertan, actualizan ni eliminan registros. Requieren MySQL 8 o posterior.

Resultado observado: MySQL tenía alerts vacía y cero ventas confirmadas. Las consultas
reales devolvieron listas vacías/resumen cero. La lectura de una alerta existente
se verificó solo con doble unitario; sigue pendiente verificar un registro real
cuando el negocio lo genere. Las fixtures SQL validaron el cálculo sin poblar la base.
La ejecución final aprobó 29 comprobaciones unitarias nuevas y 64 verificaciones
HTTP/MySQL reales; con las cuatro comprobaciones de fixtures SQL fueron 68.
También pasaron las suites existentes: 76 generales, 12 IAM, 63 Inventory y 48 Operations
en sus modos sin MySQL. El build final no tuvo advertencias ni errores y el modelo
EF no tiene cambios pendientes respecto de la migración común.
La primera prueba detectó una aserción demasiado amplia que rechazaba la palabra
password en el documento OpenAPI de IAM; se corrigió para revisar credenciales solo
en las respuestas de negocio, sin ocultar fallos de la API.
Otra prueba detectó que limit vacío se tomaba como omitido; ahora limit= devuelve
400 en español, mientras omitir el parámetro conserva el valor por defecto 10.

## Casos en Scalar

Abrir http://localhost:5281/scalar, expandir Monitoring o Reporting y usar Test Request.

1. GET alerts → 200 con [] si no hay alertas; copiar un ID real si existe para GET por ID.
2. ID no registrado → 404 en español; invalid o GUID vacío → 400.
3. sales-summary sin filtros → cantidad/total de todas las Confirmed, o ceros.
4. from=2026-11-10 y to=2026-11-10 → solo ese día UTC.
5. from=2026-11-11 y to=2026-11-10, o fecha 2026-02-30 → 400.
6. top-products con limit=1 y luego 10 → orden descendente; 0, 101 o abc → 400.
7. No confirmar ventas ni generar alertas desde estos GET; una base sin esos datos
   produce resultados vacíos legítimos.

## MySQL Workbench: solo lectura

```sql
USE bodego_db;
SELECT id, product_id, lot_id, alert_type, status, message, created_at,
       attended_by_user_id, attended_at
FROM alerts ORDER BY created_at DESC, id;

SELECT status, COUNT(*) AS sales_count, COALESCE(SUM(total),0) AS total_amount
FROM sales GROUP BY status;

SET @from = '2026-11-10 00:00:00';
SET @to = '2026-11-11 00:00:00'; -- extremo exclusivo
-- Para no limitar un extremo, asignarle NULL.
SELECT COUNT(*) AS sales_count, COALESCE(SUM(total),0) AS total_amount
FROM sales WHERE status = 'Confirmed'
  AND (@from IS NULL OR registered_at >= @from)
  AND (@to IS NULL OR registered_at < @to);

SELECT p.id, p.name, SUM(i.quantity) AS quantity_sold
FROM sale_items i JOIN sales s ON s.id = i.sale_id
JOIN products p ON p.id = i.product_id
WHERE s.status = 'Confirmed'
  AND (@from IS NULL OR s.registered_at >= @from)
  AND (@to IS NULL OR s.registered_at < @to)
GROUP BY p.id, p.name ORDER BY quantity_sold DESC, p.id ASC LIMIT 10;
```

Cuando Commercial implemente una confirmación real y segura, registrar/confirmar
ventas por su flujo autorizado y comparar estas consultas con Scalar para el mismo
rango UTC. No cambiar Draft a Confirmed manualmente para poblar el reporte: omitiría
las reglas transaccionales que Commercial debe implementar. No borrar historia.

## Pendientes y límites

- Generación/atención de alertas y confirmación transaccional de Commercial quedan fuera.
- Pendiente GET de alerta real existente y comparación de reportes con ventas reales
  confirmadas; las fixtures SQL no sustituyen esas pruebas de negocio completas.
- No se ejecutaron pruebas de carga ni se garantiza una instantánea entre dos GET
  distintos si Commercial confirma nuevas ventas simultáneamente.
- No se modificaron autenticación, permisos, frontend, IDs, migraciones ni otros módulos.
- El equipo debe revisar el PR antes de integrar a develop. No se hace merge automático.
