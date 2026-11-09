# IAM: usuarios y roles

## Revision del modelo

User es el agregado: genera Guid, valida nombre (120), rol positivo y hash (255),
inicia activo y registra CreatedAt en UTC. Role es un catalogo con INT asignado
y nombre unico de hasta 30 caracteres. EmailAddress valida, recorta y normaliza
a minusculas el correo (180); EF lo convierte a una columna con indice unico.
No se necesita modificar entidades, DbContext, tablas ni migraciones para IAM.

Application comprobara existencia de rol, unicidad de correo y contraseña antes
de crear User. La contraseña se transformara en hash con ASP.NET Core; ninguna
respuesta REST incluira PasswordHash. Los errores de unicidad concurrentes deben
traducirse tambien a conflicto, sin exponer excepciones SQL.

Los endpoints iniciales son anonimos para el laboratorio, sin JWT ni autorizacion.
La seleccion de rol NO equivale a conceder autorizacion verificada. Antes de
publicar la aplicacion para uso real se debe acordar autenticacion y restringir
registro administrativo y consultas de usuarios.
