namespace BodeGo.API.Reporting.Application.Model.Queries;
public sealed record ReportDateRange
{
    public DateTime? StartUtc { get; }
    public DateTime? EndExclusiveUtc { get; }
    public ReportDateRange(DateOnly? from, DateOnly? to)
    {
        if (from > to) throw new ArgumentException("La fecha inicial no puede ser posterior a la fecha final.");
        if (from?.Year < 1000 || to?.Year < 1000)
            throw new ArgumentException("Las fechas deben ser del año 1000 o posterior, según el rango DATETIME de MySQL.");
        StartUtc = from.HasValue ? DateTime.SpecifyKind(from.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc) : null;
        EndExclusiveUtc = to.HasValue && to.Value != DateOnly.MaxValue
            ? DateTime.SpecifyKind(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc) : null;
    }
}
