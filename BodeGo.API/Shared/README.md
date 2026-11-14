# Shared

Espacio para abstracciones técnicas mínimas compartidas; no es un Bounded Context.
Incluye `Domain/Validation`, un ayudante interno de validación de argumentos,
y `Infrastructure/Persistence/EFC`, con BodeGoDbContext, su fábrica de diseño y
migraciones. El contexto común es la composición técnica de los mapeos de los
cinco contextos persistentes, no un modelo de negocio compartido.

Cuando exista una necesidad real, pueden ubicarse contratos técnicos de unidad de
trabajo, infraestructura transversal y utilidades sin significado de negocio,
organizados por capa. Mantener las dependencias dirigidas hacia abstracciones.

No colocar aquí User, Product, Money, reglas de existencias, modelos de reportes
ni repositorios de negocio. No convertir Shared en una colección de código sin dueño.
Cada incorporación debe justificar sus consumidores y evitar ciclos.
