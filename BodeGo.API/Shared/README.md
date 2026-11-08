# Shared

Espacio para abstracciones técnicas mínimas compartidas; no es un Bounded Context.
Por ahora sólo contiene esta guía, sin clases ni paquetes adicionales.

Cuando exista una necesidad real, pueden ubicarse contratos técnicos de unidad de
trabajo, infraestructura transversal y utilidades sin significado de negocio,
organizados por capa. Mantener las dependencias dirigidas hacia abstracciones.

No colocar aquí User, Product, Money, reglas de existencias, modelos de reportes
ni repositorios de negocio. No convertir Shared en una colección de código sin dueño.
Cada incorporación debe justificar sus consumidores y evitar ciclos.
