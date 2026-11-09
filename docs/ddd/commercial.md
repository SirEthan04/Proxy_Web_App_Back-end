# Commercial: compras y ventas

Las rutas aparecen en Scalar (entorno Development) bajo la etiqueta **Commercial**. Estos seis endpoints crean y consultan borradores; crear borradores no modifica productos, lotes ni `inventory_movements`.

## Endpoints

- `POST /api/v1/purchases`: registra una compra Draft y sus detalles.
- `GET /api/v1/purchases`: lista compras y totales.
- `GET /api/v1/purchases/{id}`: obtiene compra y detalles; 404 si no existe.
- `POST /api/v1/sales`: registra una venta Draft y sus detalles.
- `GET /api/v1/sales`: lista ventas y totales.
- `GET /api/v1/sales/{id}`: obtiene venta y detalles; 404 si no existe.

## JSON para Scalar

Compra (mismo contrato para venta):

```json
{
  "userId": "GUID-DE-USUARIO-EXISTENTE",
  "items": [
    {
      "productId": "GUID-DE-PRODUCTO-EXISTENTE",
      "lotId": null,
      "quantity": 3,
      "unitAmount": 2.50
    }
  ]
}
```

Para `POST /api/v1/sales`, el importe representa precio unitario. Para compras representa costo unitario. `userId`, `productId`, `quantity`, `unitAmount` y al menos un detalle son obligatorios; `lotId` es opcional y cuando se especifica debe existir y pertenecer al producto. Cantidad debe ser mayor que cero; importe admite hasta dos decimales y no puede ser negativo. Éxito: 201 con recurso y GUID; validación/referencia inválida: 400; consulta inexistente: 404.

Prueba en Scalar: iniciar la API en Development, abrir `/scalar/v1`, seleccionar POST y pegar el JSON con GUID reales obtenidos de usuarios/productos existentes. Copiar el ID de la respuesta para probar GET por ID y revisar las listas.

## Verificación en MySQL Workbench

```sql
SELECT id, user_id, registered_at, status, total FROM purchases ORDER BY registered_at DESC;
SELECT purchase_id, product_id, lot_id, quantity, unit_cost, subtotal FROM purchase_items;
SELECT id, user_id, registered_at, status, total FROM sales ORDER BY registered_at DESC;
SELECT sale_id, product_id, lot_id, quantity, unit_price, subtotal FROM sale_items;
```

Los totales se calculan sumando cada subtotal `cantidad * importe unitario`; el dominio persiste ambos. Crear un borrador no escribe en inventario. Las confirmaciones y la verificación de sus movimientos quedan pendientes de una etapa coordinada posterior.

No se han ejecutado pruebas HTTP/MySQL en esta implementación; requieren la base configurada localmente. Los errores de FK y reglas de datos enviados por los servicios se presentan en español.
