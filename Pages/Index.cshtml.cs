using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StrideVault.Models;

public class IndexModel : PageModel
{
    private readonly ActivityService _activityService;
    private const int PageSize = 10;

    public List<Activity> Activities { get; set; } = new();
    public DashboardStats Stats { get; set; } = new();

    public List<string> WeeklyLabels { get; set; } = new();
    public List<double> WeeklyData { get; set; } = new();

    public Dictionary<DateTime, double> HeatmapData { get; set; } = new();
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? Type { get; set; }

    public int TotalPages { get; set; }

    public IndexModel(ActivityService activityService)
    {
        _activityService = activityService;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Stats = await _activityService.GetDashboardStatsAsync(cancellationToken);

        var (items, total) = await _activityService.GetPagedActivitiesAsync(Type, CurrentPage, PageSize, cancellationToken);
        Activities  = items;
        TotalPages  = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        CurrentPage = Math.Clamp(CurrentPage, 1, TotalPages);

        (WeeklyLabels, WeeklyData) = await _activityService.GetWeeklyChartAsync(Type, cancellationToken);

        HeatmapData = await _activityService.GetHeatmapDataAsync(cancellationToken);

        // Accurate all-time streak stats (separate lightweight query)
        (CurrentStreak, LongestStreak) = await _activityService.GetStreakStatsAsync(cancellationToken);
    }
}
