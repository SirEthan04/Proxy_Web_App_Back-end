# Inventario aprobado de tablas MySQL

Son 14 tablas previstas. Este documento no crea esquema, claves, migraciones,
relaciones EF ni conexiones. Los detalles de columnas y restricciones se acordarán
antes de implementar la persistencia. Una tabla no implica un Aggregate Root.

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

Las referencias entre contextos se modelarán por identificador. Definir integridad,
transacciones e índices al diseñar la persistencia, sin acoplar los agregados mediante
navegaciones entre contextos. No guardar contraseñas ni cadenas de conexión reales
en archivos versionados; usar configuración externa o secretos locales.
