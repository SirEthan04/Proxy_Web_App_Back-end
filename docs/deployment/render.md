# Backend BodeGo en Render

## Imagen y servicio

El contexto de Docker es la raíz del repositorio. El Dockerfile restaura y publica
BodeGo.API en Release con SDK 10 y ejecuta el resultado con ASP.NET 10, como usuario
sin privilegios. No usa launchSettings ni ejecuta migraciones al arrancar.
El script de entrada escucha en `http://0.0.0.0:$PORT`; el valor predeterminado es
10000. Acepta puertos de 1024 a 65535. No usar 5281 como puerto público de Render.

En Render: New → Web Service → repositorio del backend → Docker. Mantener la raíz
del repositorio como Root Directory y `./Dockerfile` como Dockerfile Path; dejar
Docker Command vacío. Elegir la región de la base de datos. Publicar la rama solo
después de revisar estos cambios; esta preparación no publica ni crea servicios.

## Variables de Render

| Variable | Valor |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` (ya definido en la imagen) |
| `DOTNET_ENVIRONMENT` | `Production` (ya definido en la imagen) |
| `PORT` | Lo proporciona Render; normalmente `10000` |
| `ConnectionStrings__DefaultConnection` | Conexión MySQL remota completa, como secreto del servicio |
| `Jwt__SigningKeyBase64` | Secreto nuevo: al menos 32 bytes aleatorios codificados en Base64 |
| `Jwt__Issuer` | `BodeGo.API` |
| `Jwt__Audience` | `BodeGo.Vue` |
| `Jwt__LifetimeMinutes` | `15`; permitido entre 1 y 30 |
| `Cors__AllowedOrigins__0` | `https://NOMBRE-FRONTEND.onrender.com` |
| `ReverseProxy__KnownProxies__0` | IP exacta del proxy inmediato, verificada con Render |
| `ReverseProxy__KnownIPNetworks__0` | Alternativa: red CIDR específica y verificada del proxy |
| `ReverseProxy__ForwardLimit` | `1`; cambiar solo con una cadena de proxies verificada |

Render proporciona `RENDER=true`. En otros proveedores hay que revisar su terminación
TLS antes de usar esa opción. Las claves JWT usan exactamente las mismas claves de
configuración que IAM; User Secrets no se cargan en Production ni se copian a la imagen.
Generar la clave en una terminal privada y guardarla en Environment de Render; no
pegarla en documentación, Git, argumentos de construcción ni conversaciones.
No reutilizar la clave local ni publicar un archivo `.env` con valores reales.

CORS acepta únicamente los orígenes configurados, con HTTPS, sin ruta ni barra final.
Para otro dominio usar `Cors__AllowedOrigins__1`, etc. Permite métodos y encabezados,
incluido `Authorization`, sin cookies/AllowCredentials ni AllowAnyOrigin. En
Development se conserva el origen predeterminado `http://localhost:5174`.

## HTTPS y proxy inverso

Render termina TLS en su borde y comunica HTTP al contenedor. En Production con
`RENDER=true` la aplicación deja la redirección HTTPS al borde para evitar bucles.
Forwarded Headers se procesa antes del resto del pipeline y solo acepta proxies
conocidos. No se confía en `X-Forwarded-Host` arbitrario.

Antes de la demostración, obtener con Render las IP/redes reales del proxy inmediato
y configurar KnownProxies o KnownIPNetworks. No usar redes genéricas ni `/0`, ni
`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`. Las IP de salida usadas para autorizar
MySQL no son necesariamente las IP del proxy de entrada.
Sin esa configuración se ignoran los encabezados de proxies desconocidos: la API
puede seguir funcionando detrás del TLS de Render, pero la IP observada y el límite
de intentos de login pueden compartirse y los enlaces Location podrían indicar HTTP.
Verificar HTTPS/IP real y Location después del despliegue; no se han inventado rangos
de Render para ocultar esta limitación.

Los JWT y permisos siguen activos: 401 sin identidad válida y 403 sin permiso.
Scalar y `/openapi/v1.json` permanecen únicamente en Development; en Production
responden 404. No se agrega una ruta de salud ni una operación REST adicional.

## MySQL en la nube

Usar MySQL compatible con el proveedor oficial existente (por ejemplo MySQL 8.4),
administrado externamente o como Private Service de Render. Nunca usar localhost,
una copia de bodego_db ni root como usuario normal de la aplicación.

Para un Private Service MySQL en Render: misma región y workspace, hostname privado
y puerto 3306, con disco persistente montado en `/var/lib/mysql`. Sin disco se pueden
perder los datos al reemplazar el contenedor. No publicar MySQL como Web Service HTTP.
Revisar costes, persistencia y recuperación del proveedor antes de almacenar datos.

Plantilla (sustituir únicamente en configuración privada):

```text
Server=<host-remoto>;Port=3306;Database=bodego_demo;User=<usuario-app>;Password=<secreto>;SslMode=VerifyFull;SslCa=/etc/secrets/mysql-ca.pem;
```

Con un proveedor externo, usar su CA como Secret File y su hostname certificado.
`VerifyFull` verifica CA y nombre; no sustituirlo por Preferred o desactivar TLS para
resolver errores. Required cifra, pero no equivale a verificar el hostname. En red
privada seguir la política TLS del proveedor y documentar cualquier excepción.
Separar la cuenta propietaria de migraciones de la cuenta de ejecución con los
permisos mínimos necesarios. No incluir certificados privados ni credenciales en Git.

## Preparación manual del esquema

No se ejecuta durante el arranque. Un operador debe comprobar primero host y nombre
de una base NUEVA de demostración con consultas de solo lectura. No apuntar a
`bodego_db`. La migración existente es `20261108232740_CrearBaseDatosInicial`; no se
modifica ni se añade otra para desplegar.

Desde un entorno de confianza con SDK .NET 10 y dotnet-ef 10.0.12, proporcionar la
conexión de la nueva base mediante configuración privada de entorno y ejecutar:

```powershell
$env:DOTNET_ENVIRONMENT = "Production"
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet ef database update --project BodeGo.API
```

Usar Production para esta operación: la fábrica de diseño exige conexión remota
explícita y no usa el fallback local de Development. No pasar contraseñas mediante
`--connection` ni guardarlas en scripts. Inspeccionar el SQL y el destino antes de
aplicar. Esta preparación NO ejecutó ese comando.

Para MySQL privado, el ejecutor debe estar en la red privada: trabajo administrativo
con SDK o un bundle EF generado para Linux, transferido por un canal administrativo
seguro y ejecutado manualmente. La imagen runtime de esta API no contiene SDK ni
dotnet-ef. No añadir migraciones al entrypoint para resolver esa diferencia.

## Roles y primer Administrador

El inicializador existente sigue exigiendo Development y ejecución interactiva;
no existe registro público de administradores ni credenciales predeterminadas.
Después de preparar el esquema, un propietario debe ejecutar los siguientes comandos
en una sesión administrativa privada con la configuración de la NUEVA base ya
verificada. Para un servicio privado puede usarse una shell administrativa de Render
si el plan la permite:

```sh
env ASPNETCORE_ENVIRONMENT=Development DOTNET_ENVIRONMENT=Development dotnet /app/BodeGo.API.dll --initialize-iam-roles
env ASPNETCORE_ENVIRONMENT=Development DOTNET_ENVIRONMENT=Development dotnet /app/BodeGo.API.dll --initialize-iam-admin
```

Development se aplica solo al proceso CLI, que termina antes de abrir HTTP. No cambiar
el entorno del Web Service ni habilitar Scalar públicamente. El administrador debe
confirmar `CREAR ADMINISTRADOR EN bodego_demo`, introducir nombre/correo propios y
escribir dos veces una contraseña nueva de 12 a 128 caracteres, oculta en consola.
No usar entrada redirigida. Se rechaza un administrador existente y no se promueven
usuarios ni se cambian contraseñas existentes.

Para una base externa accesible desde el equipo también se puede ejecutar el
inicializador en un contenedor temporal interactivo, sin publicar puertos, con un
archivo privado `.env.bootstrap.local` que apunte SOLO a esa nueva base y establezca
ambos entornos en Development:

```powershell
docker run --rm -it --env-file .env.bootstrap.local --entrypoint dotnet bodego-render:local BodeGo.API.dll --initialize-iam-admin
```

Inicializar roles antes con el mismo comando y `--initialize-iam-roles`. No copiar
usuarios, hashes ni contraseñas de la base local. Estos pasos requieren ejecución
manual posterior; no se ejecutaron durante la preparación.

## Comprobar imagen y servicio

```powershell
docker build -t bodego-render:local .
docker run --rm --name bodego-render-local --env-file .env.render.local -e PORT=10000 -e RENDER=true -p 127.0.0.1:8080:10000 bodego-render:local
```

El archivo privado debe contener las variables Production, un JWT de prueba y una
conexión remota de pruebas. No usar bodego_db ni versionar ese archivo. Para verificar
solo arranque y rechazos sin MySQL puede usarse un hostname reservado inexistente;
las consultas que requieren persistencia no funcionarán con esa configuración.

Comprobar sin escribir datos:

```powershell
curl.exe -i http://localhost:8080/api/v1/auth/me
curl.exe -i http://localhost:8080/scalar
curl.exe -i -X OPTIONS http://localhost:8080/api/v1/products -H "Origin: https://NOMBRE-FRONTEND.onrender.com" -H "Access-Control-Request-Method: GET" -H "Access-Control-Request-Headers: authorization"
```

Esperar 401, 404 y preflight 204 con el origen permitido, respectivamente. Un origen
ajeno no debe recibir Access-Control-Allow-Origin. Con esquema listo en la nube,
GET `/api/v1/categories` permite verificar lectura y sirve como Health Check Path
de Render. Hasta entonces usar la comprobación de puerto predeterminada; no elegir
`/auth/me` como health check porque responde 401 sin token.

En Vue configurar su URL base con el HTTPS del backend según el cliente Axios
existente, sin duplicar `/api/v1`. El login continúa enviando JSON y las operaciones
protegidas usan `Authorization: Bearer <token>`. No cambiar contratos por Docker.

## Verificación de la entrega

Los ocho proyectos de comprobación se retiraron de esta entrega. Compilar y publicar
únicamente la solución y BodeGo.API; el Dockerfile no depende de proyectos de pruebas:

```powershell
dotnet restore
dotnet build
dotnet publish BodeGo.API -c Release
```

Para comprobación manual usar los comandos HTTP de la sección anterior. Revisar las
32 operaciones en `/openapi/v1.json` únicamente en Development; en Production la
documentación sigue deshabilitada. Una publicación para Linux no sustituye construir
y ejecutar una imagen Docker. Los resultados siguientes son históricos, anteriores
al retiro de los ejecutables de pruebas.

## Resultado de esta preparación

- Restore y build correctos, sin errores ni advertencias.
- Siete suites anteriores: 381 comprobaciones aprobadas, sin MySQL.
- DeploymentChecks: 61 comprobaciones aprobadas, incluido arranque HTTP Production.
- Se verificaron las 32 operaciones OpenAPI en Development.
- Publicación Release para linux-x64 correcta y sintaxis del script validada con Bash.
- Docker no está instalado en el equipo: quedan pendientes construir y ejecutar la
  imagen real, verificar los proxies efectivos de Render y conectar la nueva base
  en la nube. No se desplegó ni se ejecutaron migraciones o inicializadores.
- No se modificaron MySQL, frontend, contratos, GUID ni migraciones existentes.

## Referencias oficiales

- [Render: puertos y Web Services](https://render.com/docs/web-services).
- [Render: Docker](https://render.com/docs/docker).
- [Render: MySQL privado](https://render.com/docs/deploy-mysql) y [discos persistentes](https://render.com/docs/disks).
- [ASP.NET Core: proxies y Forwarded Headers](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0).
- [Oracle Connector/NET: TLS y opciones](https://dev.mysql.com/doc/connector-net/en/connector-net-8-0-connection-options.html).
