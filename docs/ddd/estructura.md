# Estructura DDD de BodeGo

Base organizativa dentro de un único proyecto ASP.NET Core: `BodeGo.API`.
Las carpetas delimitan responsabilidades; el compilador todavía no impide referencias
entre capas. La revisión de Pull Requests debe comprobar estas reglas.
No se implementan modelos, persistencia ni nuevos endpoints en esta base.

## Contextos y modelos previstos

| Contexto | Responsabilidad | Domain/Model/Aggregates | Domain/Model/Entities | Domain/Model/ValueObjects |
| --- | --- | --- | --- | --- |
| IAM | Usuarios y roles | User | Role: catálogo asociado a usuarios | EmailAddress |
| Inventory | Productos, categorías, lotes y existencias | Product | Category: catálogo; ProductLot: entidad relacionada con Product | ProductName, Money, StockLevel |
| Commercial | Compras y ventas | Purchase, Sale | PurchaseItem y SaleItem: entidades internas de su agregado | Quantity, Money |
| Operations | Mermas, conteos y trazabilidad de inventario | Waste, InventoryCount | InventoryCountItem: entidad interna; InventoryMovement: registro histórico inmutable | Por definir según reglas del negocio |
| Monitoring | Alertas y su atención | Alert | Por definir según reglas de atención y persistencia | Por definir |
| Reporting | Dashboard y reportes de consulta | No aplica | No aplica | No aplica |

Esta tabla describe ubicaciones futuras; no declara clases ni relaciones EF.
Role y Category son catálogos; no se convierten automáticamente en Aggregate Roots.
InventoryMovement es un registro inmutable, sin asignarle artificialmente un agregado.
Dashboard y los reportes irán en `Reporting/Application/Model/ReadModels`.

## Organización de los cinco contextos de negocio

```text
BodeGo.API/<IAM|Inventory|Commercial|Operations|Monitoring>/
  Domain/
    Model/
      Aggregates/
      Entities/
      ValueObjects/
      Commands/
      Queries/
    Repositories/
    Services/
  Application/
    Internal/
      CommandServices/
      QueryServices/
    Services/
  Infrastructure/
    Persistence/EFC/Repositories/
  Interfaces/
    REST/
      Resources/
      Transform/
```

- **Domain:** invariantes, modelos y contratos del negocio, sin ASP.NET Core ni EF.
  `Commands` y `Queries` contienen los mensajes según la convención solicitada de
  CatchUp; no contienen HTTP ni acceso a datos. `Repositories` define contratos
  orientados a agregados, no un repositorio por cada tabla. `Services` reserva los
  contratos de servicios del dominio y de comandos/consultas de esa convención.
- **Application:** ejecuta los casos de uso y coordina transacciones mediante
  abstracciones. `Internal/CommandServices` implementa escritura y
  `Internal/QueryServices` lectura. `Services` reserva contratos propios de
  aplicación y puertos de coordinación; no duplica contratos de `Domain/Services`.
  Las invariantes pertenecen a los modelos del dominio.
- **Infrastructure:** implementa contratos y acceso técnico. `Persistence/EFC`
  reserva la ubicación de EF Core, sin paquetes, DbContext ni migraciones todavía.
- **Interfaces/REST:** futuros controladores, recursos HTTP y transformaciones
  entre recursos y mensajes/resultados de aplicación. No expone entidades EF ni
  concentra reglas de negocio. Los controladores irán en `REST` cuando existan.

## Reporting

```text
Reporting/
  Application/
    Model/Queries/
    Model/ReadModels/
    Internal/QueryServices/
    Services/
  Infrastructure/Persistence/EFC/Queries/
  Interfaces/REST/Resources/
  Interfaces/REST/Transform/
```

Reporting sólo consulta y proyecta datos existentes. No tiene Domain, agregados,
servicios de comandos ni tablas propias inicialmente. Sus contratos de lectura
se ubican en `Application/Services` y sus implementaciones técnicas en Infrastructure.

## Dependencias e integración

1. Domain no depende de Application, Infrastructure ni Interfaces.
2. Application depende de su Domain y de contratos explícitos; no depende de
   controladores, implementaciones EF ni entidades de persistencia.
3. Infrastructure implementa los contratos de Domain/Application. Interfaces llama
   servicios de aplicación a través de sus contratos. `Program.cs` será el punto
   de composición para registrar implementaciones cuando corresponda.
4. Referenciar otros agregados por identificadores, sin navegaciones de objetos
   entre contextos. No acceder directamente al repositorio privado de otro contexto.
5. IAM aporta la identidad del actor. Commercial y Operations coordinan los cambios
   de existencias con Inventory desde Application mediante contratos explícitos.
   Monitoring procesa señales del negocio para gestionar Alert. Reporting consume
   proyecciones o consultas de lectura sobre los datos aprobados, sin modificarlos.
6. Definir contratos, límites transaccionales e idempotencia antes de implementar
   coordinación. No se presupone mensajería ni microservicios en esta base.
7. `Money` pertenece inicialmente a Inventory y Commercial por separado: las
   reglas de cada contexto pueden variar. No moverlo a Shared sólo por repetir nombre.
8. Evitar ciclos y referencias a clases internas de otros contextos. Compartir sólo
   contratos deliberados y revisar cualquier nueva dependencia en el PR.

Las carpetas vacías se versionan con `.gitkeep`; retirarlo cuando tengan contenido
real es opcional. El ejemplo existente de weatherforecast permanece sin cambios.
