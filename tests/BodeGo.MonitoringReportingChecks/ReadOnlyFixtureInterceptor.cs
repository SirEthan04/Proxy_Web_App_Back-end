using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

// Fixtures virtuales solo para probar SQL: CTE no inserta ni modifica ninguna tabla.
internal sealed class ReadOnlyFixtureInterceptor : DbCommandInterceptor
{
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        command.CommandText = """
            WITH sales AS (
              SELECT 's1' id, 'Confirmed' status, 12.00 total, CAST('2026-11-10 00:00:00' AS DATETIME(6)) registered_at
              UNION ALL SELECT 's2', 'Confirmed', 7.50, CAST('2026-11-10 23:59:59.999999' AS DATETIME(6))
              UNION ALL SELECT 's3', 'Draft', 100.00, CAST('2026-11-10 12:00:00' AS DATETIME(6))
              UNION ALL SELECT 's4', 'Cancelled', 999.00, CAST('2026-11-10 12:00:00' AS DATETIME(6))
              UNION ALL SELECT 's5', 'Confirmed', 20.00, CAST('2026-11-11 00:00:00' AS DATETIME(6))
            ), products AS (
              SELECT '00000000-0000-0000-0000-000000000001' id, 'Arroz unitario' name
              UNION ALL SELECT '00000000-0000-0000-0000-000000000002', 'Azucar unitaria'
            ), sale_items AS (
              SELECT 's1' sale_id, '00000000-0000-0000-0000-000000000001' product_id, 2 quantity
              UNION ALL SELECT 's1', '00000000-0000-0000-0000-000000000002', 7
              UNION ALL SELECT 's2', '00000000-0000-0000-0000-000000000001', 3
              UNION ALL SELECT 's3', '00000000-0000-0000-0000-000000000001', 100
              UNION ALL SELECT 's4', '00000000-0000-0000-0000-000000000001', 999
              UNION ALL SELECT 's5', '00000000-0000-0000-0000-000000000001', 20
            )
            """ + "\n" + command.CommandText;
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}
