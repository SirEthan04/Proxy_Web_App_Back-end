# Trabajo del equipo

`main` conserva versiones estables; `develop` integra el trabajo revisado.
Esta estructura se propone desde `feature/estructura-ddd` mediante PR a `develop`.
No hacer merge directo ni reescribir el historial. Usar nombres nuevos de ramas y
mensajes de commits en español, claros y sencillos; ramas sin tildes ni espacios.

| Responsable funcional | Rama prevista | Carpetas principales |
| --- | --- | --- |
| Usuarios | feature/usuarios | IAM |
| Inventario | feature/inventario | Inventory |
| Compras y ventas | feature/compras-ventas | Commercial |
| Movimientos y mermas | feature/movimientos-mermas | Operations |
| Alertas y reportes | feature/alertas-reportes | Monitoring y Reporting |

Estas cinco ramas en español son nombres para el trabajo posterior: crearlas desde
`develop` después de integrar la estructura. No se crean en esta tarea para evitar
que nazcan sin la base compartida. Si existen entonces, inspeccionar su historial
antes de utilizarlas y no sobrescribir sus avances.

Al revisar el remoto para esta tarea, las ramas `feature/iam`, `feature/inventory`,
`feature/commercial`, `feature/operations` y `feature/monitoring-reporting` apuntaban
a `10bd9e9`, igual que main y develop, sin commits adicionales. Se conservan:
no renombrarlas ni eliminarlas sin autorización. Esta observación corresponde a
ese momento; volver a consultar el remoto antes de cualquier decisión futura.

## Flujo práctico

1. Guardar el trabajo propio y comprobar `git status`. No continuar sobre cambios ajenos.
2. Ejecutar `git fetch origin`; actualizar `develop` con `git pull --ff-only`
   desde esa rama. Si diverge, revisar antes de integrar.
3. Crear la rama asignada desde el develop actualizado, comprobando primero si ya existe.
4. Limitar cambios al contexto asignado; coordinar cambios de Shared, Program.cs,
   dependencias y contratos entre contextos con el equipo.
5. Revisar el diff y posibles secretos, ejecutar `dotnet build` y las pruebas del
   comportamiento implementado. Crear commits en español, por ejemplo
   `Agregar validacion del correo de usuario`.
6. Publicar la rama con upstream y abrir PR con base `develop`: describir cambios,
   validación y efectos sobre otros contextos. Solicitar revisión de un compañero.
7. Integrar tras la revisión y los controles acordados. Publicar versiones estables
   en main mediante PR; no usar push forzado ni sobrescribir conflictos sin acuerdo.

Las protecciones de ramas y CI deben configurarse por separado en GitHub.
Esta estructura no las activa automáticamente.

## Primer desarrollo funcional

Acordar primero las invariantes y los contratos del primer caso de uso de Inventory
(Category/Product). Implementar el modelo y sus pruebas, luego repositorios y
servicios de aplicación. Seleccionar un proveedor EF Core compatible con .NET 10
y MySQL, configurar secretos, DbContext y migraciones en una tarea posterior.
Completar recursos, transformaciones y un endpoint vertical con pruebas de integración.
Coordinar el contrato de identidad con IAM antes de añadir autorización.
No instalar paquetes ni implementar estas piezas como parte de la estructura inicial.
