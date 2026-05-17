using Microsoft.AspNetCore.Mvc.RazorPages;
using StrideVault.Models;
using System.Globalization;

public class PlannerModel : PageModel
{
    private readonly ActivityService _activityService;

    public PlannerModel(ActivityService activityService)
    {
        _activityService = activityService;
    }

    public Dictionary<DateTime, DaySummary> Days    { get; set; } = new();
    public List<WeekSummary>                Weekly  { get; set; } = new();
    public List<Activity>                   Activities { get; set; } = new();
    public DateTime                         StartDate { get; set; }
    public double                           MonthAvgDistanceKm { get; set; }

    public async Task OnGetAsync(int? month, int? year, CancellationToken cancellationToken)
    {
        StartDate = new DateTime(year ?? DateTime.Today.Year, month ?? DateTime.Today.Month, 1);
        var endDate = StartDate.AddMonths(1);

        Activities = await _activityService.GetActivitiesInRangeAsync(StartDate, endDate, cancellationToken);
        var cachedDetails = await _activityService.GetCachedDetailsForAsync(
            Activities.Select(a => a.StravaId), cancellationToken);

        BuildSummaries(cachedDetails);
    }

    private void BuildSummaries(Dictionary<long, ActivityDetails> details)
    {
        if (Activities.Count == 0)
        {
            Days   = new();
            Weekly = new();
            return;
        }

        var daysDict  = new Dictionary<DateTime, DaySummary>(Activities.Count);
        var weeksDict = new Dictionary<int, WeekSummary>();

        foreach (var a in Activities)
        {
            var date = a.StartDateLocal.Date;

            if (!daysDict.TryGetValue(date, out var day))
            {
                day = new DaySummary();
                daysDict[date] = day;
            }

            day.ActivityCount++;
            day.TotalTime     += a.MovingTime;
            day.TotalDistance += a.Distance;
            day.Entries.Add(new DaySummary.Entry(
                a.StravaId, a.Name ?? "Unnamed", a.Type ?? "Other", a.Distance, a.MovingTime));

            var week    = ISOWeek.GetWeekOfYear(a.StartDateLocal);
            var yr      = a.StartDateLocal.Year;
            var weekKey = yr * 100 + week;

            if (!weeksDict.TryGetValue(weekKey, out var ws))
            {
                ws = new WeekSummary { Week = week, Year = yr };
                weeksDict[weekKey] = ws;
            }

            ws.TotalTime     += a.MovingTime;
            ws.TotalDistance += a.Distance;

            if (details.TryGetValue(a.StravaId, out var d))
                ws.Calories = (ws.Calories ?? 0) + (d.Calories ?? 0);
        }

        Days   = daysDict;
        Weekly = weeksDict.Values.OrderBy(w => w.Year).ThenBy(w => w.Week).ToList();

        // Average distance per active day this month (used for training load indicator)
        MonthAvgDistanceKm = Days.Count > 0
            ? Days.Values.Average(d => d.TotalDistance) / 1000.0
            : 0;
    }

    public string FormatTime(int seconds)
    {
        var t = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return $"{(int)t.TotalHours}h {t.Minutes}m";
    }

    public class DaySummary
    {
        public int             TotalTime     { get; set; }
        public double          TotalDistance { get; set; }
        public int             ActivityCount { get; set; }
        public List<Entry>     Entries       { get; set; } = new();

        public record Entry(long StravaId, string Name, string Type, double Distance, int MovingTime);
    }

    public class WeekSummary
    {
        public int     Year          { get; set; }
        public int     Week          { get; set; }
        public int     TotalTime     { get; set; }
        public double  TotalDistance { get; set; }
        public double? Calories      { get; set; }
    }
}
