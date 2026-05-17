using Microsoft.AspNetCore.Mvc.RazorPages;
using StrideVault.Models;

public class AchievementsModel : PageModel
{
    private readonly ActivityService _activityService;

    public List<Activity> LongestRides { get; set; } = new();
    public List<Activity> LongestRuns  { get; set; } = new();
    public List<Activity> LongestWalks { get; set; } = new();

    public List<Activity> FastestRides { get; set; } = new();
    public List<Activity> FastestRuns  { get; set; } = new();

    public Activity? FastestRide { get; set; }
    public Activity? FastestRun  { get; set; }
    public Activity? FastestWalk { get; set; }

    public List<MilestoneItem> Milestones { get; set; } = new();

    public AchievementsModel(ActivityService activityService)
    {
        _activityService = activityService;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var activities = await _activityService.GetAllActivitiesAsync(cancellationToken);
        if (!activities.Any()) return;

        var rides = activities.Where(a => a.Type == "Ride").ToList();
        var runs  = activities.Where(a => a.Type == "Run").ToList();
        var walks = activities.Where(a => a.Type == "Walk").ToList();

        LongestRides = rides.OrderByDescending(a => a.Distance).Take(5).ToList();
        LongestRuns  = runs.OrderByDescending(a => a.Distance).Take(5).ToList();
        LongestWalks = walks.OrderByDescending(a => a.Distance).Take(5).ToList();

        FastestRides = rides.Where(HasSpeed).OrderByDescending(GetSpeed).Take(5).ToList();
        FastestRuns  = runs.Where(HasSpeed).OrderByDescending(GetSpeed).Take(5).ToList();

        FastestRide = FastestRides.FirstOrDefault();
        FastestRun  = FastestRuns.FirstOrDefault();
        FastestWalk = walks.Where(HasSpeed).OrderByDescending(GetSpeed).FirstOrDefault();

        Milestones = BuildMilestones(activities, rides, runs, walks);
    }

    static List<MilestoneItem> BuildMilestones(
        List<Activity> all, List<Activity> rides, List<Activity> runs, List<Activity> walks)
    {
        var ms = new List<MilestoneItem>();

        // ── Single-activity milestones ─────────────────────────────────────

        var first = all.MinBy(a => a.StartDateLocal);
        ms.Add(M("🏁", "First Step", "Completed your very first activity",
            first, first?.StartDateLocal));

        var ride50km = rides.Where(a => a.Distance >= 50_000).MinBy(a => a.StartDateLocal);
        ms.Add(M("💪", "50 km Ride", "Rode 50+ km in a single outing",
            ride50km, ride50km?.StartDateLocal));

        var ride100km = rides.Where(a => a.Distance >= 100_000).MinBy(a => a.StartDateLocal);
        ms.Add(M("🌟", "Century Ride", "Rode 100+ km in a single outing",
            ride100km, ride100km?.StartDateLocal));

        var halfMarathon = runs.Where(a => a.Distance >= 21_097).MinBy(a => a.StartDateLocal);
        ms.Add(M("🏅", "Half Marathon", "Ran a half marathon (21+ km)",
            halfMarathon, halfMarathon?.StartDateLocal));

        var marathon = runs.Where(a => a.Distance >= 42_195).MinBy(a => a.StartDateLocal);
        ms.Add(M("🏆", "Marathon", "Ran a full marathon (42+ km)",
            marathon, marathon?.StartDateLocal));

        // ── Count milestones ───────────────────────────────────────────────

        var ride100th = rides.Count >= 100
            ? rides.OrderBy(a => a.StartDateLocal).Skip(99).First() : null;
        ms.Add(M("🚴", "Century Rider", "Completed 100 rides",
            ride100th, ride100th?.StartDateLocal));

        var run100th = runs.Count >= 100
            ? runs.OrderBy(a => a.StartDateLocal).Skip(99).First() : null;
        ms.Add(M("🏃", "100 Runs", "Completed 100 runs",
            run100th, run100th?.StartDateLocal));

        // ── Cumulative distance milestones ─────────────────────────────────

        ms.Add(Cumulative("🌍", "1,000 km by Bike", "Cycled 1,000 km total",
            rides, 1_000_000));

        ms.Add(Cumulative("🚀", "2,500 km by Bike", "Cycled 2,500 km total",
            rides, 2_500_000));

        ms.Add(Cumulative("🦶", "500 km on Foot", "Ran 500 km total",
            runs, 500_000));

        ms.Add(Cumulative("🔥", "1,000 km on Foot", "Ran 1,000 km total",
            runs, 1_000_000));

        // ── Streak milestones ──────────────────────────────────────────────

        var activeDates = new HashSet<DateTime>(all.Select(a => a.StartDateLocal.Date));
        var longestStreak = ComputeLongestStreak(activeDates);

        ms.Add(new MilestoneItem("🗓️", "Week Warrior",
            "Kept a 7-day activity streak", longestStreak >= 7,
            longestStreak >= 7 ? $"{longestStreak} day best" : null));

        ms.Add(new MilestoneItem("📅", "Month of Motion",
            "Kept a 30-day activity streak", longestStreak >= 30,
            longestStreak >= 30 ? $"{longestStreak} day best" : null));

        return ms;
    }

    // Builds a milestone for a cumulative distance threshold.
    static MilestoneItem Cumulative(string icon, string title, string desc,
        List<Activity> source, double thresholdMetres)
    {
        double cum = 0;
        Activity? trigger = null;
        foreach (var a in source.OrderBy(a => a.StartDateLocal))
        {
            cum += a.Distance;
            if (cum >= thresholdMetres) { trigger = a; break; }
        }
        return M(icon, title, desc, trigger, trigger?.StartDateLocal);
    }

    static MilestoneItem M(string icon, string title, string desc,
        object? condition, DateTime? date)
        => new(icon, title, desc, condition != null,
               date?.ToString("MMM d, yyyy"));

    static int ComputeLongestStreak(HashSet<DateTime> dates)
    {
        var longest = 0; var streak = 0;
        DateTime? prev = null;
        foreach (var d in dates.OrderBy(x => x))
        {
            streak = prev.HasValue && d == prev.Value.AddDays(1) ? streak + 1 : 1;
            if (streak > longest) longest = streak;
            prev = d;
        }
        return longest;
    }

    static bool   HasSpeed(Activity a) => a.MovingTime > 0 && a.Distance > 0;
    static double GetSpeed(Activity a) => (a.Distance / 1000.0) / (a.MovingTime / 3600.0);

    public record MilestoneItem(
        string  Icon,
        string  Title,
        string  Description,
        bool    Unlocked,
        string? AchievedOn);
}
