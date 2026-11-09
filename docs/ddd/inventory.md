# Inventory: implementación y guía de integración

Estado: ocho endpoints implementados. **Las pruebas reales HTTP/MySQL están PENDIENTES** y deben ejecutarse antes del merge a develop. No se ejecutaron migraciones, INSERT SQL ni pruebas contra MySQL durante esta implementación.

## Alcance y organización

Se reutiliza BodeGo.API, .NET 10, el proveedor MySql.EntityFrameworkCore y BodeGoDbContext.
No se cambia el esquema aprobado de 14 tablas ni las migraciones.

- Domain: Product, Category, ProductLot, Value Objects, validaciones, comandos, consultas, excepciones y contratos de repositorios.
- Application: CategoryCommandService y ProductCommandService registran; CategoryQueryService y ProductQueryService consultan y proyectan resultados.
- Infrastructure: repositorios EF con el DbContext compartido y AddInventory para registrar dependencias.
- Interfaces/REST: controladores, recursos JSON, ensambladores y errores HTTP en español.

CQRS separa mensajes y servicios de escritura y lectura. No requiere MediatR ni bases separadas.
Las consultas EF usan AsNoTracking. Cada registro guarda una entidad con SaveChangesAsync.
No se crean lotes ni movimientos al registrar productos. Commercial y Operations deberán coordinar sus propios movimientos válidos.

## Endpoints

Todos llevan la etiqueta **Inventory** en Scalar.

| Método | Ruta | Respuesta correcta | Errores previstos |
| --- | --- | --- | --- |
| POST | /api/v1/categories | 201 + recurso + Location | 400, 409 |
| GET | /api/v1/categories | 200 + lista | — |
| GET | /api/v1/categories/{id} | 200 + recurso | 400, 404 |
| POST | /api/v1/products | 201 + recurso + Location | 400 |
| GET | /api/v1/products | 200 + lista | — |
| GET | /api/v1/products/{id} | 200 + recurso | 400, 404 |
| GET | /api/v1/products/{id}/lots | 200 + lista | 400, 404 |
| GET | /api/v1/products/low-stock | 200 + lista | — |

Los identificadores son GUID en texto con guiones. GUID inválido o vacío devuelve 400; GUID válido inexistente devuelve 404.
La ruta literal low-stock tiene prioridad sobre {id}; no se interpreta como un GUID.
Los listados de categorías/productos ordenan por nombre e ID; los lotes por received_at e ID. No hay paginación en este alcance.

## JSON exacto de categorías

| Campo | Tipo | Regla |
| --- | --- | --- |
| name | string obligatorio | No blanco; máximo 100 caracteres |
| description | string o null, opcional | Si se envía texto, no blanco; máximo 255 caracteres |

El dominio recorta espacios exteriores. El nombre tiene índice único.
El chequeo previo de duplicados usa la comparación de MySQL; el índice protege también las solicitudes concurrentes.
La comparación de mayúsculas y acentos depende de la collation real del servidor; los dobles unitarios no demuestran esa semántica.

POST /api/v1/categories:

```json
{
  "name": "Abarrotes Inventory",
  "description": "Arroz, azúcar y productos secos"
}
```

También es válido omitir description o enviarlo como null. No enviar id ni active; se asignan en el dominio.
Las propiedades JSON desconocidas se rechazan con 400 en los dos POST.

Respuesta ilustrativa 201, **no resultado de una ejecución MySQL**:

```json
{
  "id": "11111111-1111-4111-8111-111111111111",
  "name": "Abarrotes Inventory",
  "description": "Arroz, azúcar y productos secos",
  "active": true
}
```

Copiar el ID real devuelto y verificar el encabezado Location: /api/v1/categories/{id}.
GET por ID devuelve ese recurso; GET general devuelve un arreglo de recursos de esta forma, o [] si no existen categorías.
Se incluyen las categorías existentes sin filtrar actividad.

## JSON exacto de productos

| Campo | Tipo | Regla |
| --- | --- | --- |
| categoryId | string GUID obligatorio | No vacío; debe existir en categories |
| name | string obligatorio | No blanco; máximo 150 caracteres |
| salePrice | número decimal obligatorio | Entre 0 y 9999999999.99, máximo dos decimales |
| minimumStock | número entero obligatorio | Entre 0 y 2147483647 |

Se permite precio cero según Money y el CHECK existente. No se impone unicidad al nombre de producto porque el esquema no la exige.
Se comprueba existencia de categoría, sin inventar una restricción de actividad que el modelo no contiene.
No enviar stock, lots, version, active ni id. Se rechazan campos adicionales; omitir campos numéricos no equivale a enviarlos como cero.

POST /api/v1/products (reemplazar categoryId con el ID real del POST de categoría):

```json
{
  "categoryId": "11111111-1111-4111-8111-111111111111",
  "name": "Arroz extra Inventory 1 kg",
  "salePrice": 3.50,
  "minimumStock": 5
}
```

Respuesta ilustrativa 201, **no resultado de una ejecución MySQL**:

```json
{
  "id": "22222222-2222-4222-8222-222222222222",
  "categoryId": "11111111-1111-4111-8111-111111111111",
  "name": "Arroz extra Inventory 1 kg",
  "salePrice": 3.50,
  "stock": 0,
  "minimumStock": 5,
  "active": true,
  "version": 0
}
```

Copiar el ID real y comprobar Location: /api/v1/products/{id}.
GET por ID devuelve ese recurso. GET general y GET low-stock devuelven arreglos de recursos de esta misma forma, o [].
El producto nuevo pertenece a low-stock porque 0 <= 5. Incluso con mínimo cero, 0 <= 0 y también pertenece.
La consulta compara el stock persistido con minimum_stock; no calcula ni inventa existencias y no filtra por actividad.

GET /api/v1/products/{id}/lots devuelve [] para un producto nuevo.
Para un producto existente con lotes, cada elemento tiene esta forma ilustrativa:

```json
{
  "id": "33333333-3333-4333-8333-333333333333",
  "productId": "22222222-2222-4222-8222-222222222222",
  "lotCode": "LOTE-REAL",
  "expirationDate": "2027-12-31",
  "quantityAvailable": 4,
  "receivedAt": "2026-11-09T12:00:00Z"
}
```

expirationDate puede ser null. receivedAt representa UTC; MySQL datetime(6) no almacena zona y la serialización de fechas materializadas puede no incluir Z.
No hay POST de lotes en este módulo.

## Errores para probar

Los errores de negocio usan ProblemDetails; los errores de binding/anotaciones usan ValidationProblemDetails.
Los campos type, traceId, instance y el texto técnico de parámetros pueden variar. Verificar status y title o errors según corresponda.

- 400: POST categoría con {"name":" "} o nombre de 101 caracteres; description con 256 caracteres o solo espacios.
- 400: POST producto con nombre vacío/de 151 caracteres, precio -1, precio 1.001, precio 10000000000, mínimo -1, GUID vacío o categoría inexistente.
- 400: omitir categoryId, salePrice o minimumStock; enviar mínimo fraccionario, JSON malformado o stock adicional.
- 400: GET con id igual a texto inválido o GUID vacío.
- 404: GET categoría/producto con GUID válido inexistente; GET lotes de producto inexistente.
- 409: repetir exactamente el POST de categoría existente. No se espera 409 por nombres repetidos de producto.

Ejemplos de cuerpos ProblemDetails abreviados:

```json
{"status":400,"title":"La categoría indicada no existe."}
```

```json
{"status":404,"title":"No se encontró el producto solicitado."}
```

```json
{"status":409,"title":"El nombre de la categoría ya está registrado."}
```

Ejemplo abreviado de validación de campo obligatorio:

```json
{
  "status": 400,
  "title": "Se produjeron uno o más errores de validación.",
  "errors": {"Name":["El nombre de la categoría es obligatorio."]}
}
```

## Procedimiento con Scalar y MySQL (PENDIENTE)

1. Usar feature/inventory, con las 14 tablas previamente preparadas. Revisar la conexión propia conforme a [tablas.md](../database/tablas.md). Guardar credenciales en User Secrets; no en Git. No ejecutar una migración inicial sobre tablas existentes.
2. Ejecutar dotnet restore y dotnet build -c Release desde la raíz.
3. Iniciar en Development: dotnet run --project BodeGo.API -c Release. Usar la URL que indique la terminal y abrir /scalar/v1.
4. Confirmar las ocho operaciones Inventory y las cinco IAM. /openapi/v1.json verifica documentación; esto por sí solo no prueba MySQL.
5. En Scalar abrir POST categories, usar el ejemplo y ejecutar. Guardar status, cuerpo y Location; copiar el ID real. Repetir el mismo POST para 409 y comprobar que no se duplicó la fila.
6. Probar ambos GET de categorías y los casos 400/404.
7. Crear un producto con el ID de categoría copiado. Guardar el ID real y verificar 201, Location, stock = 0 y version = 0. No agregar stock inicial al JSON.
8. Probar GET productos, GET por ID y GET lotes: el producto nuevo debe tener [] con 200. Un ID inexistente debe dar 404.
9. Probar GET low-stock y comprobar que se ejecuta esa ruta, devuelve 200 y contiene el producto creado.
10. Probar todos los errores anteriores; verificar con SELECT que las solicitudes rechazadas no insertaron filas.
11. Registrar resultados reales en el PR antes de aprobar el merge. Mantenerlo abierto si quedan fallos.

## SELECT para comprobar persistencia

Los nombres provienen de las configuraciones Inventory y la convención snake_case de BodeGoDbContext.
Reemplazar los GUID de ejemplo por los IDs reales obtenidos con Scalar.

```sql
SELECT id, name, description, active
FROM categories
ORDER BY name, id;

SELECT id, category_id, name, sale_price, stock, minimum_stock, active, version
FROM products
ORDER BY name, id;

SELECT id, product_id, lot_code, expiration_date, quantity_available, received_at
FROM product_lots
ORDER BY received_at, id;

SELECT id, name, description, active
FROM categories
WHERE id = '11111111-1111-4111-8111-111111111111';

SELECT id, category_id, name, sale_price, stock, minimum_stock, active, version
FROM products
WHERE id = '22222222-2222-4222-8222-222222222222';

SELECT id, product_id, lot_code, expiration_date, quantity_available, received_at
FROM product_lots
WHERE product_id = '22222222-2222-4222-8222-222222222222'
ORDER BY received_at, id;

SELECT COUNT(*) AS category_name_count
FROM categories
WHERE name = 'Abarrotes Inventory';

SELECT id, category_id, name, sale_price, stock, minimum_stock, active, version
FROM products
WHERE stock <= minimum_stock
ORDER BY name, id;
```

Después del POST categoría, debe existir exactamente una fila por el ID devuelto y el nombre enviado. Repetir el POST debe devolver 409 y mantener el conteo por nombre.
Después del POST producto, comprobar category_id, precio, mínimo y stock cero en esa fila. No debe haber lotes para el producto nuevo.
Comparar los IDs del SELECT low-stock con los del GET correspondiente; deben coincidir con el estado actual de la base.

Para probar stock positivo menor/igual/mayor al mínimo, usar productos y lotes reales ya existentes o un flujo válido de Commercial/Operations que registre movimientos de forma coherente.
No ejecutar UPDATE manual de stock ni INSERT ficticio de lotes para esta prueba.
Si todavía no existe tal flujo o esos datos, dejar esos casos de integración pendientes; las pruebas unitarias solo verifican las reglas en objetos aislados.

## Verificaciones sin MySQL

Se sigue el formato de ejecutables de comprobación ya utilizado por IAM; no se añade otro framework de pruebas ni otro proveedor.

```powershell
dotnet restore
dotnet build -c Release
dotnet run --project tests/BodeGo.Checks -c Release
dotnet run --project tests/BodeGo.IamChecks -c Release
dotnet run --project tests/BodeGo.InventoryChecks -c Release
```

Ejecutar IamChecks sin argumentos: sus modos adicionales sí pueden intentar integración.
Los dobles de repositorios existen solo en tests/BodeGo.InventoryChecks. No son persistencia real.
Inventory comprueba dominio, límites, servicios, proyecciones, estados de controladores invocados directamente, campos obligatorios, DI y generación SQL con el proveedor MySQL sin abrir conexión.

Para comprobar exclusivamente OpenAPI sin conexión, iniciar otra terminal con:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__DefaultConnection = 'Server=127.0.0.1;Port=1;Database=bodego_openapi_only;User=unused;'
dotnet BodeGo.API/bin/Release/net10.0/BodeGo.API.dll --urls http://127.0.0.1:5188
```

Esa cadena no operativa no se guarda en configuración ni se usa como prueba de base de datos.
En otra terminal:

```powershell
dotnet run --project tests/BodeGo.InventoryChecks -c Release -- --openapi http://127.0.0.1:5188
```

Este modo solo solicita /openapi/v1.json y comprueba ocho operaciones Inventory y cinco IAM.
Cerrar la terminal de documentación al terminar; usar la conexión real para integración.
Si Debug está bloqueado por una instancia abierta, Release permite compilar sin interrumpirla.

## Integración pendiente antes del merge

Resultados ejecutados durante la implementación:

- dotnet restore: correcto (solución y proyecto InventoryChecks).
- dotnet build -c Release: correcto, cero advertencias y cero errores.
- BodeGo.Checks: 76 comprobaciones sin acceso a base de datos.
- BodeGo.IamChecks sin argumentos: 12 comprobaciones unitarias.
- BodeGo.InventoryChecks: 63 comprobaciones sin MySQL.
- OpenAPI en una instancia separada: ocho operaciones Inventory y cinco IAM documentadas.
- El intento de compilación Debug encontró el ejecutable bloqueado por una instancia existente; Release compiló correctamente sin detenerla.

Estos resultados no acreditan integración HTTP/MySQL.

- **PENDIENTE:** los ocho endpoints HTTP con MySQL real, incluidos 201/Location y lectura posterior de filas insertadas.
- **PENDIENTE:** materialización de GUID, Money, StockLevel, fechas, booleanos y valores nulos con datos reales.
- **PENDIENTE:** traducción/ejecución de consultas, comparación de stock y ordenamiento con el proveedor real.
- **PENDIENTE:** comportamiento de unicidad según collation y traducción de error 1062 a 409 en registros concurrentes.
- **PENDIENTE:** FK de categoría y traducción de error 1452 si desaparece entre validación e inserción.
- **PENDIENTE:** validación HTTP real de cuerpos, identificadores, 400, 404, 409 y ruta literal low-stock.
- **PENDIENTE:** lotes reales y stock positivo en los tres límites, mediante datos/movimientos válidos.
- **PENDIENTE:** comprobar que POST producto no altera movimientos ni crea lotes.
- **PENDIENTE:** regresión HTTP/MySQL de IAM por el integrante responsable; su código no se modifica.
