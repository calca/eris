namespace eris.Core.ExportServices;

public sealed record ExportMetricsSnapshot(
    double WeeklyHours,
    double PercentBase,
    ExportSummaryTotals Totals,
    IReadOnlyList<ExportSummaryRow> SummaryRows,
    IReadOnlyList<ExportTagSummaryRow> SummaryByTagRows);
