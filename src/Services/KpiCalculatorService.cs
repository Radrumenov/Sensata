using Sensata.Models;

namespace Sensata.Services
{
    // ── KpiCalculatorService ───────────────────────────────────────────────────────
    // Отговорност: САМО изчислява KPI от сурови данни.
    // Не знае нищо за база данни, не знае нищо за AI.
    // Приема List<ProductionReport> → връща List<ShiftKpi> или LineSummaryKpi.
    // ──────────────────────────────────────────────────────────────────────────────

    public class ShiftKpi
    {
        // Входни данни (копирани от репорта за удобство)
        public string Date            { get; init; } = "";
        public string ShiftName       { get; init; } = "";
        public int    TotalProduced   { get; init; }
        public int    DefectiveUnits  { get; init; }
        public double AvgTemperature  { get; init; }
        public double AvgPressure     { get; init; }
        public double EnergyConsumed  { get; init; }
        public string OperatorComment { get; init; } = "";
        public int    TokenScore      { get; init; }

        // Изчислени KPI
        public double DefectRate      { get; init; }   // % брак
        public double Yield           { get; init; }   // % добив
        public double Availability    { get; init; }   // OEE компонент A
        public double Performance     { get; init; }   // OEE компонент P
        public double Quality         { get; init; }   // OEE компонент Q (= Yield)
        public double OEE             { get; init; }   // A × P × Q / 10000
        public double EnergyIntensity { get; init; }   // kWh / брой произведени единици
    }

    public class LineSummaryKpi
    {
        public int    LineId              { get; init; }
        public string LineName            { get; init; } = "";
        public bool   IsAutomated         { get; init; }
        public int    TotalShifts         { get; init; }
        public double AvgProducedPerShift { get; init; }
        public double AvgDefectRate       { get; init; }
        public double AvgOEE              { get; init; }
        public double AvgTemperature      { get; init; }
        public double AvgEnergyIntensity  { get; init; }
        public int    BadShiftsCount      { get; init; }   // смени с коментар != "OK"
        public int    MaxTokenScore       { get; init; }   // най-висок token score за периода
        public int    TotalAlerts         { get; init; }
    }

    public static class KpiCalculatorService
    {
        // Нормативни стойности за OEE изчислението
        // Максимален теоретичен капацитет на смяна (използва се за Performance)
        private const int MaxTheoreticalOutput = 375;

        // ── Изчислява KPI за единична смяна ──────────────────────────────────────
        public static ShiftKpi Calculate(ProductionReport r)
        {
            double defectRate = r.TotalProduced > 0
                ? Math.Round((double)r.DefectiveUnits / r.TotalProduced * 100, 2)
                : 0;

            double yield = r.TotalProduced > 0
                ? Math.Round((double)(r.TotalProduced - r.DefectiveUnits) / r.TotalProduced * 100, 2)
                : 0;

            // Availability: колко от теоретичното работно време е използвано
            // Опростена формула базирана на производителност спрямо максимума
            double availability = Math.Round(Math.Min(100, 88 + (double)r.TotalProduced / MaxTheoreticalOutput * 12), 2);

            // Performance: реално производство спрямо теоретичния максимум
            double performance = Math.Round(Math.Min(110, (double)r.TotalProduced / MaxTheoreticalOutput * 100), 2);

            // Quality = Yield (производство без брак / общо производство)
            double quality = yield;

            // OEE = A × P × Q / 10000 (защото всеки е в %)
            double oee = Math.Round(availability * performance * quality / 10_000, 2);

            double energyIntensity = r.TotalProduced > 0
                ? Math.Round(r.EnergyConsumed / r.TotalProduced, 4)
                : 0;

            return new ShiftKpi
            {
                Date            = r.Date.ToString("dd.MM.yyyy"),
                ShiftName       = r.ShiftName,
                TotalProduced   = r.TotalProduced,
                DefectiveUnits  = r.DefectiveUnits,
                AvgTemperature  = r.AvgTemperature,
                AvgPressure     = r.AvgPressure,
                EnergyConsumed  = r.EnergyConsumed,
                OperatorComment = r.OperatorComment,
                TokenScore      = r.CommentTokenScore,
                DefectRate      = defectRate,
                Yield           = yield,
                Availability    = availability,
                Performance     = performance,
                Quality         = quality,
                OEE             = oee,
                EnergyIntensity = energyIntensity,
            };
        }

        // ── Изчислява KPI за списък от смени (един период) ───────────────────────
        public static List<ShiftKpi> CalculateAll(IEnumerable<ProductionReport> reports)
            => reports.Select(Calculate).ToList();

        // ── Обобщен summary за линията за периода ─────────────────────────────────
        public static LineSummaryKpi Summarize(
            ProductionLine       line,
            List<ProductionReport> reports,
            List<LineAlert>      alerts)
        {
            if (!reports.Any())
                return new LineSummaryKpi
                {
                    LineId    = line.Id,
                    LineName  = line.Name,
                    IsAutomated = line.IsAutomated,
                };

            var kpis = CalculateAll(reports);

            return new LineSummaryKpi
            {
                LineId              = line.Id,
                LineName            = line.Name,
                IsAutomated         = line.IsAutomated,
                TotalShifts         = kpis.Count,
                AvgProducedPerShift = Math.Round(kpis.Average(k => k.TotalProduced), 0),
                AvgDefectRate       = Math.Round(kpis.Average(k => k.DefectRate), 2),
                AvgOEE              = Math.Round(kpis.Average(k => k.OEE), 2),
                AvgTemperature      = Math.Round(kpis.Average(k => k.AvgTemperature), 1),
                AvgEnergyIntensity  = Math.Round(kpis.Average(k => k.EnergyIntensity), 4),
                BadShiftsCount      = kpis.Count(k => k.OperatorComment != "OK"),
                MaxTokenScore       = kpis.Max(k => k.TokenScore),
                TotalAlerts         = alerts.Count,
            };
        }
    }
}
