# Autenticación y autorización IAM

## Alcance

La rama `feature/autenticacion-iam` conserva las 30 rutas originales y agrega
dos operaciones: `POST /api/v1/auth/login` y `GET /api/v1/auth/me`.
No cambia GUID, tablas, migraciones, contraseñas existentes ni claves foráneas.

El registro pasa a ser administrativo. Los cuatro POST de Commercial y los POST
de mermas/conteos requieren identidad real. Las demás rutas conservan sus permisos anteriores: Inventory,
los GET de Operations, Monitoring, Reporting y los GET de Commercial no quedan protegidos
por esta tarea. No asumir que toda la API ya está protegida para producción.

## Login y respuesta

`POST /api/v1/auth/login`, sin token:

```json
{
  "email": "usuario@ejemplo.com",
  "password": "<contraseña real introducida localmente>"
}
```

Es un ejemplo de contrato, no una cuenta inicial. El correo se normaliza con
`EmailAddress`. `PasswordHasher.VerifyHashedPassword` comprueba el hash existente.
También acepta el resultado `SuccessRehashNeeded`, pero no reescribe hashes en
esta entrega. No se comparan contraseñas en Vue ni se utiliza JSON Server.

Respuesta 200, ejemplo ilustrativo:

```json
{
  "accessToken": "<JWT>",
  "tokenType": "Bearer",
  "expiresAt": "2026-11-11T20:15:00Z",
  "user": {
    "id": "11111111-1111-1111-1111-111111111111",
    "name": "Usuario de ejemplo",
    "email": "usuario@ejemplo.com",
    "roleId": 2,
    "role": "Empleado",
    "isAdministrator": false
  }
}
```

El GUID y vencimiento reales provienen del servidor. No devuelve contraseña ni
hash. `isAdministrator` es una ayuda de presentación; los permisos efectivos se
deciden en el backend con el rol consultado en MySQL.

- 400: campos requeridos ausentes o longitudes inválidas.
- 401: correo inexistente, contraseña incorrecta, usuario inactivo o rol no admitido.
  Mensaje genérico: `El correo o la contraseña no son válidos.`
- 429: más de cinco solicitudes de login por IP en una ventana de un minuto,
  incluidas las exitosas; incluye `Retry-After` cuando está disponible.
- Las respuestas de login y `me` llevan `Cache-Control: no-store`.

Para un correo inexistente se verifica un hash ficticio interno para evitar omitir
el coste del hashing. Nunca se registra la contraseña ni el token completo.

## JWT y User Secrets

Paquete incorporado: `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.12.
Se firma con HS256. El token contiene `sub` (GUID), `jti`, emisor, audiencia,
inicio de validez y expiración. No contiene contraseñas ni un rol autoritativo.

Configuración:

| Clave | Valor predeterminado o requisito |
| --- | --- |
| `Jwt:Issuer` | `BodeGo.API` |
| `Jwt:Audience` | `BodeGo.Vue` |
| `Jwt:LifetimeMinutes` | 15; permitido de 1 a 30 |
| `Jwt:SigningKeyBase64` | Obligatorio: al menos 32 bytes aleatorios codificados en Base64 |

La API rechaza arrancar con una configuración JWT inválida. No incluye una clave
de ejemplo funcional en archivos versionados. Las pruebas usan claves aleatorias
que sólo viven en memoria dentro de cada servidor de pruebas.
Al finalizar se configuró también una clave local de 32 bytes en User Secrets,
sin mostrarla y conservando exactamente DefaultConnection. No se escribió en MySQL.
Esa clave local no se incluye en Git y no debe compartirse con el equipo.

Desde PowerShell de Rider, en la raíz de la solución, ejecutar **localmente**
cuando falte configurar la API habitual. Si ya existe una clave válida, conservarla:
regenerarla invalida los tokens emitidos anteriormente.

```powershell
# El proyecto ya tiene UserSecretsId; no es necesario volver a inicializarlo.
$jwtRandom = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$jwtBytes = New-Object byte[] 32
$jwtRandom.GetBytes($jwtBytes)
$jwtConfiguration = @{
  Jwt = @{
    Issuer = 'BodeGo.API'
    Audience = 'BodeGo.Vue'
    LifetimeMinutes = 15
    SigningKeyBase64 = [Convert]::ToBase64String($jwtBytes)
  }
} | ConvertTo-Json -Depth 3

# Guarda mediante stdin; no imprime la clave ni la incluye en los argumentos.
$jwtConfiguration | dotnet user-secrets set --project BodeGo.API/BodeGo.API.csproj
$jwtRandom.Dispose()
[Array]::Clear($jwtBytes, 0, $jwtBytes.Length)
Remove-Variable jwtRandom, jwtBytes, jwtConfiguration
```

No usar `dotnet user-secrets list` para compartir capturas: también mostraría la
conexión MySQL. User Secrets es almacenamiento local de desarrollo, no un almacén
cifrado para producción. Conservar `DefaultConnection` en la fuente segura actual.
No colocar la clave en Vue, `.env` versionados, URLs, Swagger/Scalar preconfigurado,
`appsettings.json` ni `launchSettings.json`.

La validación comprueba firma, algoritmo HS256, emisor, audiencia, expiración y
presencia de firma. La tolerancia de reloj es de 15 segundos. El middleware se
ejecuta después de CORS y antes de los controladores: autenticación y autorización.
En producción se requiere HTTPS y una fuente de secretos apropiada.

## Identidad vigente y permisos

`GET /api/v1/auth/me` requiere:

```http
Authorization: Bearer <accessToken>
```

Devuelve el mismo objeto seguro de `user` del login, con el estado vigente.
`sub` se valida y se convierte a GUID en `DatabaseJwtEvents`. Cada petición
autenticada consulta usuario activo y rol actual en MySQL; después reconstruye
`ClaimsPrincipal` y la identidad usada por Application (`ICurrentUser`).
Un rol viejo o añadido por el cliente no concede permisos.

Un usuario desactivado recibe 401 en la siguiente petición protegida. Un cambio
de Administrador a Empleado elimina sus permisos administrativos aunque el JWT
todavía no haya vencido. Un rol inexistente o distinto de Administrador/Empleado
invalida la identidad. Las credenciales previas de usuarios registrados mediante
la API son reutilizables sin cambiar sus GUID, hashes ni roles.

| Operación | Anónimo | Empleado activo | Administrador activo |
| --- | --- | --- | --- |
| Login | Permitido con credenciales válidas | Permitido | Permitido |
| `GET /auth/me` | 401 | Su identidad | Su identidad |
| POST/GET `/users` | 401 | 403 | Permitido |
| GET `/users/{id}` | 401 | 403, incluso para su propio ID | Permitido |
| GET `/roles`, `/roles/{id}` | 401 | 403 | Permitido |
| POST `/purchases`, `/sales` | 401 | Crea con su GUID | Crea con su GUID |
| POST `/purchases/{id}/confirm`, `/sales/{id}/confirm` | 401 | Sólo documentos propios | Puede confirmar documentos ajenos |
| POST `/wastes`, `/inventory-counts` | 401 | Registra con su GUID | Registra con su GUID |

Los errores mantienen `ProblemDetails` y textos en español. Un token inexistente,
inválido o vencido produce 401; un usuario autenticado sin permiso, 403.

## Commercial: contrato y trazabilidad

Ahora los cuerpos de creación contienen sólo `items`:

```json
{
  "items": [
    {
      "productId": "22222222-2222-2222-2222-222222222222",
      "lotId": null,
      "quantity": 20,
      "unitAmount": 1.20
    }
  ]
}
```

Las rutas no cambian. `userId` deja de aceptarse en el body: enviarlo genera 400
porque el recurso rechaza propiedades desconocidas. El controlador toma el GUID
validado y construye el Command. Application también rechaza un Command que intente
crear en nombre de otra persona, incluso si el actor es Administrador.

La respuesta sigue incluyendo `userId`: representa al **creador**. Confirmar no
cambia ese creador. `inventory_movements.user_id` identifica al **confirmador
autenticado**. Si un Administrador confirma una venta de un Empleado, ambos GUID
pueden ser diferentes; es intencionado. No hizo falta una columna o migración nueva.

El permiso de confirmación se comprueba dentro de la coordinación transaccional,
después de cargar la cabecera y antes de modificar inventario. Se conserva
`SELECT ... FOR UPDATE`, `ReadCommitted`, Version, FIFO, lotes, rollback y protección
contra doble confirmación. Se comprueba que el confirmador continúe activo.
Un Administrador puede confirmar un borrador cuyo creador esté inactivo; no se
atribuye la acción a ese creador.

## Operations y formularios Vue

`POST /api/v1/wastes` y `POST /api/v1/inventory-counts` usan la política
`OperationsOperator` para Administrador/Empleado. El controlador obtiene el actor
de `ICurrentUser`, acreditado desde ClaimsPrincipal, y Application comprueba que
el Command use ese mismo GUID. Los Resources rechazan userId con 400.
Los recursos de respuesta conservan userId como dato de trazabilidad.
Contratos completos y reglas de transacción: [operations.md](operations.md).
La merma conserva el descuento atómico y sus movimientos; el conteo sólo registra
la fotografía física, sin ajustar stock.

## Primer Administrador: procedimiento manual, pendiente

La revisión anterior de `bodego_db` no encontró Administradores. **No se creó
ninguno ni se modificaron cuentas existentes.** Hasta inicializar uno de forma
autorizada no habrá acceso a las operaciones administrativas de IAM.

No hay endpoint público alternativo ni creación automática durante el arranque.
El mecanismo es una acción explícita de consola, sólo en Development.
Para usarlo posteriormente, con autorización para crear una cuenta:

1. Comprobar rama, conexión y base destino local. Respaldar la base según el
   procedimiento del equipo. No pegar la conexión ni credenciales en el chat.
2. Revisar en Workbench, mediante consultas de lectura:

```sql
SELECT DATABASE();
SELECT id, name FROM roles ORDER BY id;
SELECT COUNT(*) AS administradores_existentes
FROM users u JOIN roles r ON r.id = u.role_id
WHERE r.name = 'Administrador';
SELECT COUNT(*) AS administradores_activos
FROM users u JOIN roles r ON r.id = u.role_id
WHERE r.name = 'Administrador' AND u.active = TRUE;
```

3. Usar `DefaultConnection` de User Secrets en Development. La consola también
   ofrece una comprobación de lectura:

```powershell
dotnet run --project BodeGo.API --launch-profile http -- --check-iam-admin
```

4. Confirmar que el catálogo tiene Administrador y Empleado. Si no lo tiene,
   revisar el catálogo antes de autorizar la inicialización de roles existente;
   no ejecutar cambios de roles como parte de una comprobación.
5. Sólo después de la autorización correspondiente, ejecutar localmente:

```powershell
dotnet run --project BodeGo.API --launch-profile http -- --initialize-iam-admin
```

6. Verificar la base destino que muestra la consola. Para la base habitual,
   escribir exactamente `CREAR ADMINISTRADOR EN bodego_db`; después nombre, correo nuevo y una
   contraseña propia de 12 a 128 caracteres; repetirla. La contraseña no se muestra
   ni se pasa como argumento. El comando sólo crea una cuenta nueva con hash.
7. Si existe un Administrador, incluso inactivo, o el correo ya pertenece a una
   cuenta, el comando se detiene. No promueve usuarios, reactiva cuentas ni cambia
   contraseñas. Cualquier recuperación requiere autorización independiente.
8. Iniciar la API normalmente y probar login y `/auth/me`; luego comprobar acceso
   administrativo. No compartir el JWT obtenido.

**Este comando no fue ejecutado sobre `bodego_db`.** Las cuentas administradoras
de las pruebas sólo se insertaron en bases ficticias aisladas con claves aleatorias.

## Probar desde Scalar

1. Configurar JWT localmente; conservar la conexión segura existente.
2. Reiniciar la API desde Rider con el perfil `http` y abrir
   `http://localhost:5281/scalar`. No reutilizar una instancia compilada antes de
   estos cambios. No iniciar dos procesos en el mismo puerto.
3. Ejecutar login con una cuenta existente y copiar `accessToken` al esquema
   HTTP Bearer de Scalar. El esquema agrega el prefijo `Bearer`; no duplicarlo.
4. Probar `GET /auth/me`. Si se usa un Empleado, `GET /users` debe devolver 403.
5. Crear una compra con `items` y sin `userId`; confirmar usando su propietario.
6. Con otra identidad Empleado, confirmar un documento ajeno debe devolver 403.
   Con Administrador, puede devolver 200 y el movimiento debe señalarlo como actor.
7. Probar sin token (401), expirado (401), `userId` en body (400) y exceso de
   intentos de login (429).
8. Para pruebas que cambien datos usar la base aislada, no `bodego_db`.

## Verificación de la entrega

Los proyectos de comprobación se retiraron del repositorio. Para compilar y publicar
sin interrumpir una API abierta se pueden usar artefactos temporales:

```powershell
dotnet restore BodeGo.sln --artifacts-path "$env:TEMP/BodeGo-entrega-artifacts"
dotnet build BodeGo.sln --no-restore --artifacts-path "$env:TEMP/BodeGo-entrega-artifacts"
dotnet publish BodeGo.API -c Release --artifacts-path "$env:TEMP/BodeGo-entrega-artifacts" -o "$env:TEMP/BodeGo-entrega-publish"
```

Para verificar autenticación usar los casos manuales de Scalar de esta guía y
comprobar las 32 operaciones en OpenAPI desde Development. No escribir datos en la
base habitual para comprobar compilación o documentación. Las pruebas que necesitan
crear usuarios u operaciones requieren una base aislada y autorización expresa.

Los resultados de las secciones siguientes se conservan como evidencia histórica
anterior al retiro de las suites; los comandos automatizados ya no están disponibles.

## Resultado de la reanudación (11 de noviembre de 2026)

- Se inspeccionó sin escribir la base anterior
  `bodego_test_autenticacion_20261110212005_8dad812d`: 15 tablas,
  5 usuarios, 3 productos, 7 compras, 9 ventas y 16 movimientos.
- La ejecución real ampliada utilizó
  `bodego_test_autenticacion_20261111200206_ea819d69`.
- **74 comprobaciones HTTP/MySQL** de autenticación/autorización aprobadas:
  administrador ficticio con hash, login, errores de credenciales, inactivos,
  tokens alterados/vencidos, emisor/audiencia incorrectos, rol vigente,
  desactivación, IAM administrativo, creación autenticada, suplantación rechazada,
  confirmaciones propias/ajenas, actor del movimiento y CORS con Authorization.
- **131 comprobaciones Commercial HTTP/MySQL** aprobadas en la misma base:
  flujo 0→20→17, lotes/FIFO, varios detalles, movimientos, Version, rollback tras
  SaveChanges y confirmaciones concurrentes. Reporting incluye las ventas Confirmed.
- **107 comprobaciones nuevas sin MySQL** aprobadas: HTTP/JWT con dobles explícitos,
  Scalar, 32 operaciones y conservación de las 30 rutas originales.
- Las seis suites anteriores pasaron: 76 generales, 12 IAM, 63 Inventory,
  20 Commercial, 48 Operations y 29 Monitoring/Reporting.
- Build con salida temporal: correcto, sin advertencias ni errores.
- Se verificó también el arranque de Program.cs con User Secrets en el puerto
  temporal 63382: OpenAPI con 32 operaciones, Scalar 200 y `/auth/me` sin token 401.
  Esa instancia de comprobación se detuvo; la instancia habitual no fue reiniciada.
- No se escribió en `bodego_db`, no se ejecutó allí ninguna migración y no se creó
  una migración nueva. No se crearon commits ni se publicó la rama.

Para Workbench, seleccionar explícitamente la base aislada y comprobar:

```sql
USE bodego_test_autenticacion_20261111200206_ea819d69;
SHOW TABLES;
SELECT id, name FROM roles;
SELECT id, role_id, name, active FROM users;
SELECT id, stock, version FROM products;
SELECT id, user_id, status, total FROM purchases;
SELECT id, user_id, status, total FROM sales;
SELECT source_type, source_id, user_id, quantity_delta FROM inventory_movements;
SELECT s.id, s.user_id AS creador, m.user_id AS confirmador, m.quantity_delta
FROM sales s JOIN inventory_movements m ON m.source_id = s.id
WHERE m.source_type = 'Sale';
```

No consultar ni mostrar `password_hash` para una demostración.

## Cambios necesarios en Vue

No se modificó el repositorio frontend. La integración deberá:

1. Sustituir el login de JSON Server por `POST /auth/login`.
2. Guardar en Pinia el token, vencimiento y el objeto `user` con GUID y rol real.
   Mantener inicialmente el token sólo en memoria, no en `localStorage`.
3. Agregar `Authorization: Bearer <token>` al cliente Axios del backend; nunca
   enviarlo al cliente de JSON Server ni a otros destinos.
4. Usar `/auth/me` para obtener el estado vigente cuando ya dispone del token.
   El contenido de localStorage o un correo coincidente no acreditan identidad.
5. Retirar `userId` del JSON de compras/ventas. Mantener los GUID de productos/lotes.
   Retirarlo también de los formularios de mermas y conteos; usar el mismo Bearer.
6. Habilitar las escrituras comerciales con la identidad real, respetando 401/403.
7. Ante 401 limpiar estado y volver al login; ante 403 mostrar falta de permiso.
   No reintentar automáticamente POST de confirmación.
8. Los permisos visibles pueden guiar la interfaz, pero el backend decide siempre.

## Límites pendientes

- Inicializar el primer Administrador de `bodego_db` requiere autorización y acción
  manual; no se hace automáticamente ni se rebaja la seguridad mientras tanto.
- La clave JWT local de este equipo ya quedó configurada en User Secrets. Los demás
  integrantes deben configurar su propia clave local antes de iniciar la API.
- Reiniciar desde Rider para cargar estos cambios: una instancia antigua conserva
  el comportamiento anterior hasta detenerla y arrancar la compilación nueva.
- No hay refresh tokens, endpoint logout ni lista de revocación individual. Cerrar
  sesión limpia el token en Vue; un token copiado sigue siendo válido hasta vencer
  si el usuario sigue activo. La desactivación sí se comprueba en cada petición.
- Con token sólo en memoria, recargar la página requiere login de nuevo. Una sesión
  persistente con cookies o refresh tokens requiere diseño y autorización aparte.
- El límite de login es por IP y proceso, no distribuido. Varias personas bajo la
  misma IP comparten el cupo; un despliegue con proxy necesita configuración confiable
  de IP original y un control adicional antes de exponerse a Internet.
- No se comprobó inicio de sesión con contraseñas personales de usuarios habituales.
  Importaciones con hashes no válidos necesitarían recuperación autorizada.
- Falta integrar Vue y acordar protección futura de los demás módulos. No se afirma
  que toda la aplicación esté lista para producción.

## Ampliación de Operations (11 de noviembre de 2026)

Se recuperó `0a55f7f` sin reimplementar IAM. Las pruebas ampliadas aprobaron:
129 comprobaciones HTTP/JWT con dobles, 110 HTTP/MySQL de autenticación y operaciones,
131 de Commercial HTTP/MySQL, y rollback/concurrencia MySQL de Operations.
Toda escritura utilizó `bodego_test_autenticacion_20261111210853_a4b0cfea`.
Las bases aisladas anteriores se conservaron. No se ejecutó el inicializador
administrativo en `bodego_db`, ni se modificaron sus cuentas, datos o migraciones.
Los POST requieren identidad activa; los demás módulos conservan el alcance descrito.
