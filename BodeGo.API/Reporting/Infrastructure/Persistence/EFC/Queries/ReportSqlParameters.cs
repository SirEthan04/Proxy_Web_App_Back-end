using BodeGo.API.Commercial.Domain.Model.ValueObjects;
using BodeGo.API.Reporting.Application.Model.Queries;
using MySql.Data.MySqlClient;
namespace BodeGo.API.Reporting.Infrastructure.Persistence.EFC.Queries;
internal static class ReportSqlParameters
{
    public static object[] Create(ReportDateRange range) =>
    [
        new MySqlParameter("@from", MySqlDbType.DateTime) { Value = (object?)range.StartUtc ?? DBNull.Value },
        new MySqlParameter("@to", MySqlDbType.DateTime) { Value = (object?)range.EndExclusiveUtc ?? DBNull.Value },
        new MySqlParameter("@status", MySqlDbType.VarChar) { Value = CommercialStatus.Confirmed.ToString() }
    ];
}
