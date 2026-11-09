# IAM: usuarios y roles

## Revision del modelo

User es el agregado: genera Guid, valida nombre (120), rol positivo y hash (255),
inicia activo y registra CreatedAt en UTC. Role es un catalogo con INT asignado
y nombre unico de hasta 30 caracteres. EmailAddress valida, recorta y normaliza
a minusculas el correo (180); EF lo convierte a una columna con indice unico.
No se necesita modificar entidades, DbContext, tablas ni migraciones para IAM.

Application comprueba existencia de rol, unicidad de correo y contraseña antes
de crear User. La contraseña se transforma en hash con ASP.NET Core; ninguna
respuesta REST incluye PasswordHash. Los errores del indice unico de correo se
traducen tambien a conflicto, sin exponer excepciones SQL.

Los endpoints iniciales son anonimos para el laboratorio, sin JWT ni autorizacion.
La seleccion de rol NO equivale a conceder autorizacion verificada. Antes de
publicar la aplicacion para uso real se debe acordar autenticacion y restringir
registro administrativo y consultas de usuarios.

## Flujo y archivos

- `Domain/Repositories`: IUserRepository e IRoleRepository, sin EF ni MySQL.
- `Domain/Model/Commands`: CreateUserCommand. Es una clase normal para evitar que
  el ToString generado de un record incluya la contraseña.
- `Domain/Model/Queries`: GetUsersQuery, GetUserByIdQuery, GetRolesQuery y GetRoleByIdQuery.
- `Application/Internal/CommandServices`: UserCommandService valida el registro,
  consulta el rol y correo, solicita el hash y guarda el agregado.
- `Application/Internal/QueryServices`: UserQueryService y RoleQueryService llaman
  a los repositorios y devuelven resultados sin datos de contraseña.
- `Infrastructure/Persistence/EFC/Repositories`: implementaciones reales con el
  DbContext compartido. Las consultas son AsNoTracking; una insercion usa un unico
  SaveChanges. Los errores MySQL 1062 del correo y 1452 del rol se traducen a errores
  del caso de uso, tambien ante carreras entre solicitudes.
- `Infrastructure/Hashing`: PasswordHashService utiliza PasswordHasher de ASP.NET
  Core, con PBKDF2 y salt aleatorio por contraseña. No implementa un algoritmo propio
  ni instala paquetes. La contraseña admite de 12 a 128 caracteres y no se recorta.
- `Interfaces/REST`: controladores pequeños, Resources y ensambladores explicitos.
  AddIam registra dependencias; Program.cs agrega controladores y sus rutas.

## Contrato HTTP

| Metodo | Ruta | Resultado |
| --- | --- | --- |
| POST | /api/v1/users | 201 con recurso sin hash y Location; 400 por datos/rol; 409 por correo |
| GET | /api/v1/users | 200 con lista, incluida lista vacia |
| GET | /api/v1/users/{id} | 200, 400 por ID invalido o 404 si no existe |
| GET | /api/v1/roles | 200 con lista |
| GET | /api/v1/roles/{id} | 200, 400 por ID invalido o 404 si no existe |

El POST recibe `name`, `email`, `password`, `roleId`. El correo se recorta y convierte
a minusculas; su indice unico sigue siendo la proteccion final. El recurso de usuario
contiene id, name, email, roleId, active y createdAt. UserResult y UserResource no
declaran PasswordHash. Las validaciones de entrada devuelven ProblemDetails; los
errores de base no previstos no se disfrazan de registros exitosos.

## Inicializar roles sin crear usuarios

Usar DefaultConnection en User Secrets como se explica en [base de datos](../database/tablas.md).
En la raiz de la solucion, con MySQL local y la migracion existente aplicada:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project BodeGo.API --no-launch-profile -- --initialize-iam-roles
```

El comando termina sin levantar el servidor. En una transaccion crea solamente los
roles faltantes: 1 Administrador y 2 Empleado. Se puede repetir; no modifica roles
existentes ni crea usuarios. Si encuentra otros nombres en esos IDs o los nombres
en otros IDs, se detiene para que el equipo revise el catalogo sin sobrescribirlo.
No se ejecuta automaticamente al arrancar la API ni se permite fuera de Development.
No cambia el esquema ni crea migraciones.

## Compilar y comprobar

```powershell
dotnet restore
dotnet build
dotnet run --project tests/BodeGo.Checks
dotnet run --project tests/BodeGo.IamChecks
```

Las comprobaciones originales validan dominio y modelo sin conexion. El comando IAM
sin argumentos prueba Application con dobles en memoria y verifica hashing; esos
resultados NO prueban HTTP ni MySQL. No requieren paquetes de pruebas adicionales.

Para probar la integracion real, primero verificar el destino sin escribir datos:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project tests/BodeGo.IamChecks -- --preflight
```

Solo acepta localhost o 127.0.0.1, puerto 3306, base bodego_db, con la migracion
ya aplicada. No muestra la cadena de conexion ni las excepciones SQL completas.
En una terminal levantar la API y en otra ejecutar las pruebas:

```powershell
# Terminal 1: entorno local de desarrollo
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project BodeGo.API --no-launch-profile --urls http://127.0.0.1:5287

# Terminal 2
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project tests/BodeGo.IamChecks -- --integration http://127.0.0.1:5287
```

La integracion comprueba los cinco endpoints y los codigos 201/200/400/404/409.
Inicializa roles dos veces para verificar repetibilidad. Crea mediante POST un solo
usuario identificable, `iam.verificacion@bodego.example`, nombre Prueba IAM desarrollo,
rol Empleado, con contraseña aleatoria en memoria. Comprueba su insercion directamente
con EF y verifica el hash con la contraseña enviada. La contraseña no se imprime ni
se guarda como archivo y este usuario no se prepara como cuenta de acceso manual.
Si el correo ya existe, reutiliza el registro para GET y duplicados; informa expresamente
que no hubo nueva insercion ni nueva verificacion de su contraseña en esa ejecucion.
No se eliminan usuarios. Un fallo no se reporta como exito; investigar localmente
sin compartir secretos. Para repetir una prueba de insercion nueva, acordar otro
identificador de desarrollo, sin borrar registros existentes.

## Resultado comprobado

- Build sin advertencias ni errores; las 76 comprobaciones existentes pasaron.
- Primera ejecucion real: 49 comprobaciones totales, incluyendo 12 unitarias,
  POST 201 con Location, insercion y hash verificados en MySQL, GET y errores esperados.
- Segunda ejecucion: 47 comprobaciones, reutilizando el mismo usuario sin duplicarlo.
- No se devolvio password ni password_hash en los recursos de usuario.
- No se modificaron Shared, modelos EF, migraciones ni otros contextos.

## Consultas de Workbench

```sql
USE bodego_db;
SELECT id, name FROM roles ORDER BY id;
SELECT id, role_id, name, email, active, created_at
FROM users WHERE email = 'iam.verificacion@bodego.example';
SELECT u.id, u.name, u.email, r.name AS role_name
FROM users u JOIN roles r ON r.id = u.role_id
WHERE u.email = 'iam.verificacion@bodego.example';
SELECT email, COUNT(*) AS total FROM users
WHERE email = 'iam.verificacion@bodego.example' GROUP BY email;
```

Las consultas no muestran hashes. La prueba automatizada verifica el hash en memoria.
Las anotaciones de CreateUserResource y las validaciones de Application generan
mensajes en español, incluidos los límites originales de 120/180 caracteres para
nombre/correo y 12 a 128 para contraseña. IamValidationResponse personaliza la
respuesta automática de ApiController solo para IAM: conserva ProblemDetails,
errors, claves de campos y códigos HTTP, y traduce título, JSON e identificadores
inválidos. Las respuestas 404 de usuario/rol y el conflicto de correo también están
en español. Los controles de Scalar y los nombres del protocolo HTTP no se traducen.
La integración usa además `iam.validacion-es@bodego.example` para verificar un POST
válido después de los cambios de mensajes, con contraseña aleatoria y sin borrar
registros. Si existe, lo reutiliza para evitar duplicados y no afirma una nueva alta.

Queda pendiente acordar autenticacion/autorizacion, restringir roles administrativos,
paginacion de listados y limites de solicitudes antes de un despliegue real. La carrera
de correos duplicados se maneja en el repositorio, pero no se ha probado con cargas
concurrentes. No se implementan JWT, login, eliminacion ni cambios de contraseña.
