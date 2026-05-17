using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StrideVault.Models;

public class AnalyticsModel : PageModel
{
    private readonly ActivityService _activityService;

    public AnalyticsModel(ActivityService activityService)
    {
        _activityService = activityService;
    }

    public List<int> AvailableYears { get; set; } = new();

    public List<string> Labels { get; set; } = new();

    public List<double> RideTotals { get; set; } = new();
    public List<double> RunTotals  { get; set; } = new();
    public List<double> WalkTotals { get; set; } = new();

    public List<double> RideTotalsRaw { get; set; } = new();
    public List<double> RunTotalsRaw  { get; set; } = new();
    public List<double> WalkTotalsRaw { get; set; } = new();

    // Previous-year comparison — only populated when Range == "year"
    public List<double> RideCompare { get; set; } = new();
    public List<double> RunCompare  { get; set; } = new();
    public List<double> WalkCompare { get; set; } = new();
    public int          CompareYear { get; set; }
    public bool         HasCompare  => Range == "year" && RideCompare.Any(v => v > 0);

    [BindProperty(SupportsGet = true)]
    public string Range { get; set; } = "all";

    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    public async Task OnGetAsync()
    {
        AvailableYears = await _activityService.GetAvailableYearsAsync();
        var activities = await _activityService.GetActivitiesForAnalyticsAsync(Range, Year);

        if (!activities.Any()) return;

        if (Range == "month")
        {
            var grouped = activities
                .GroupBy(a => a.StartDateLocal.Date)
                .OrderBy(g => g.Key)
                .ToList();

            Labels        = grouped.Select(g => g.Key.ToString("MMM d")).ToList();
            RideTotalsRaw = grouped.Select(g => g.Where(a => a.Type == "Ride").Sum(a => a.Distance) / 1000.0).ToList();
            RunTotalsRaw  = grouped.Select(g => g.Where(a => a.Type == "Run" ).Sum(a => a.Distance) / 1000.0).ToList();
            WalkTotalsRaw = grouped.Select(g => g.Where(a => a.Type == "Walk").Sum(a => a.Distance) / 1000.0).ToList();
        }
        else if (Range == "year")
        {
            // Normalize to 12 fixed months so the comparison overlay aligns perfectly
            var selectedYear = Year ?? DateTime.UtcNow.Year;
            CompareYear      = selectedYear - 1;

            var months = Enumerable.Range(1, 12).ToArray();
            Labels = months.Select(m => new DateTime(selectedYear, m, 1).ToString("MMM")).ToList();

            RideTotalsRaw = MonthlyRaw(activities, months, "Ride");
            RunTotalsRaw  = MonthlyRaw(activities, months, "Run");
            WalkTotalsRaw = MonthlyRaw(activities, months, "Walk");

            // Load previous year and build comparison cumulative series
            var prev = await _activityService.GetActivitiesForAnalyticsAsync("year", CompareYear);
            RideCompare = ToCumulative(MonthlyRaw(prev, months, "Ride"));
            RunCompare  = ToCumulative(MonthlyRaw(prev, months, "Run"));
            WalkCompare = ToCumulative(MonthlyRaw(prev, months, "Walk"));
        }
        else // "all"
        {
            var grouped = activities
                .GroupBy(a => new DateTime(a.StartDateLocal.Year, a.StartDateLocal.Month, 1))
                .OrderBy(g => g.Key)
                .ToList();

            Labels        = grouped.Select(g => g.Key.ToString("MMM yyyy")).ToList();
            RideTotalsRaw = grouped.Select(g => g.Where(a => a.Type == "Ride").Sum(a => a.Distance) / 1000.0).ToList();
            RunTotalsRaw  = grouped.Select(g => g.Where(a => a.Type == "Run" ).Sum(a => a.Distance) / 1000.0).ToList();
            WalkTotalsRaw = grouped.Select(g => g.Where(a => a.Type == "Walk").Sum(a => a.Distance) / 1000.0).ToList();
        }

        RideTotals = ToCumulative(RideTotalsRaw);
        RunTotals  = ToCumulative(RunTotalsRaw);
        WalkTotals = ToCumulative(WalkTotalsRaw);
    }

    static List<double> MonthlyRaw(List<Activity> acts, int[] months, string type)
    {
        var byMonth = acts
            .Where(a => a.Type == type)
            .GroupBy(a => a.StartDateLocal.Month)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.Distance) / 1000.0);

        return months.Select(m => Math.Round(byMonth.GetValueOrDefault(m, 0), 2)).ToList();
    }

    static List<double> ToCumulative(List<double> source)
    {
        var result = new List<double>(source.Count);
        double sum = 0;
        foreach (var v in source) { sum += v; result.Add(Math.Round(sum, 2)); }
        return result;
    }
}
