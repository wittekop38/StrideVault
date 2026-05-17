using Microsoft.EntityFrameworkCore;
using StravaApiLib;
using StrideVault.Data;
using StrideVault.Models;

public class ActivityService
{
    private readonly AppDbContext _db;
    private readonly StravaApi _strava;
    private readonly ILogger<ActivityService> _logger;

    public ActivityService(AppDbContext db, StravaApi strava, ILogger<ActivityService> logger)
    {
        _db = db;
        _strava = strava;
        _logger = logger;
    }

    // ── Sync ────────────────────────────────────────────────────────────────

    public async Task SyncActivitiesAsync(CancellationToken cancellationToken = default)
    {
        var existingIds = new HashSet<long>(await _db.Activities.Select(a => a.StravaId).ToListAsync(cancellationToken));
        var page = 1;
        var bufferCount = 0;
        const int flushThreshold = 100;

        while (!cancellationToken.IsCancellationRequested)
        {
            var batch = await _strava.GetActivitiesAsync(page);
            if (batch == null || batch.Count == 0) break;

            foreach (var act in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (existingIds.Contains(act.Id)) continue;

                _db.Activities.Add(StravaMapper.ToActivity(act));
                existingIds.Add(act.Id);
                bufferCount++;
            }

            if (bufferCount >= flushThreshold)
            {
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Persisted {Count} new activities", bufferCount);
                bufferCount = 0;
            }

            page++;
        }

        if (bufferCount > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Persisted final {Count} activities", bufferCount);
        }
    }

    // ── Single activity ──────────────────────────────────────────────────────

    public async Task<Activity?> GetActivityAsync(long id, CancellationToken cancellationToken = default)
        => await _db.Activities.FirstOrDefaultAsync(a => a.StravaId == id, cancellationToken);

    public async Task<ActivityDetails> GetOrFetchDetailsAsync(long id, CancellationToken cancellationToken = default)
    {
        var existing = await _db.ActivityDetails.FirstOrDefaultAsync(d => d.ActivityId == id, cancellationToken);
        if (existing != null) return existing;

        var dto = await _strava.GetActivityDetailsAsync(id);
        var entity = StravaMapper.ToDetails(dto);

        _db.ActivityDetails.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return entity;
    }

    // ── Dashboard ────────────────────────────────────────────────────────────

    /// <summary>
    /// Computes all dashboard stats via two DB aggregation queries — no full table scan in memory.
    /// </summary>
    public async Task<DashboardStats> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        var weekAgo = DateTime.Today.AddDays(-7);

        var byType = await _db.Activities
            .GroupBy(a => a.Type)
            .Select(g => new
            {
                Type      = g.Key,
                Distance  = g.Sum(a => a.Distance),
                Count     = g.Count(),
                Elevation = g.Sum(a => a.TotalElevationGain)
            })
            .ToListAsync(ct);

        var weekly = await _db.Activities
            .Where(a => a.StartDateLocal >= weekAgo)
            .GroupBy(a => a.StartDateLocal.Date)
            .Select(g => new
            {
                Distance  = g.Sum(a => a.Distance),
                Elevation = g.Sum(a => a.TotalElevationGain)
            })
            .ToListAsync(ct);

        var run     = byType.FirstOrDefault(t => t.Type == "Run");
        var ride    = byType.FirstOrDefault(t => t.Type == "Ride");
        var walk    = byType.FirstOrDefault(t => t.Type == "Walk");
        var workout = byType.FirstOrDefault(t => t.Type == "WeightTraining");

        return new DashboardStats
        {
            TotalDistanceKm     = Math.Round(byType.Sum(t => t.Distance) / 1000.0, 2),
            WeeklyDistanceKm    = Math.Round(weekly.Sum(w => w.Distance) / 1000.0, 2),
            TotalRunDistanceKm  = Math.Round((run?.Distance  ?? 0) / 1000.0, 2),
            TotalRideDistanceKm = Math.Round((ride?.Distance ?? 0) / 1000.0, 2),
            TotalWalkDistanceKm = Math.Round((walk?.Distance ?? 0) / 1000.0, 2),
            TotalRuns           = run?.Count     ?? 0,
            TotalRides          = ride?.Count    ?? 0,
            TotalWalks          = walk?.Count    ?? 0,
            TotalWeightTraining = workout?.Count ?? 0,
            TotalElevation      = byType.Sum(t => t.Elevation),
            WeeklyElevation     = weekly.Sum(w => w.Elevation),
            ActiveDaysLast7     = weekly.Count
        };
    }

    /// <summary>
    /// Returns a single page of activities ordered by date, filtered by type. DB-only, no Strava API call.
    /// </summary>
    public async Task<(List<Activity> Items, int Total)> GetPagedActivitiesAsync(
        string? type, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Activities.AsQueryable();

        if (!string.IsNullOrEmpty(type))
            query = query.Where(a => a.Type == type);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.StartDateLocal)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    /// <summary>
    /// Returns 7-day distance chart data grouped by date. DB-only.
    /// </summary>
    public async Task<(List<string> Labels, List<double> Data)> GetWeeklyChartAsync(
        string? type, CancellationToken ct = default)
    {
        var today     = DateTime.Today;
        var sevenAgo  = today.AddDays(-6);

        var query = _db.Activities.Where(a => a.StartDateLocal >= sevenAgo);
        if (!string.IsNullOrEmpty(type))
            query = query.Where(a => a.Type == type);

        var grouped = await query
            .GroupBy(a => a.StartDateLocal.Date)
            .Select(g => new { Date = g.Key, Distance = g.Sum(a => a.Distance) })
            .ToListAsync(ct);

        var byDate = grouped.ToDictionary(g => g.Date, g => g.Distance);

        var labels = new List<string>(7);
        var data   = new List<double>(7);

        for (var i = 6; i >= 0; i--)
        {
            var d = today.AddDays(-i);
            labels.Add(d.ToString("ddd"));
            data.Add(Math.Round(byDate.GetValueOrDefault(d) / 1000.0, 2));
        }

        return (labels, data);
    }

    /// <summary>
    /// Computes current and longest-ever activity streaks from all distinct active dates.
    /// One lightweight DB query (distinct dates only).
    /// </summary>
    public async Task<(int Current, int Longest)> GetStreakStatsAsync(CancellationToken ct = default)
    {
        var dates = new HashSet<DateTime>(
            await _db.Activities
                .Select(a => a.StartDateLocal.Date)
                .Distinct()
                .ToListAsync(ct));

        // Current streak
        var current = 0;
        var day = DateTime.Today;
        if (!dates.Contains(day)) day = day.AddDays(-1);
        while (dates.Contains(day)) { current++; day = day.AddDays(-1); }

        // Longest streak (requires sorted pass)
        var longest = 0;
        var streak  = 0;
        DateTime? prev = null;
        foreach (var d in dates.OrderBy(x => x))
        {
            streak = prev.HasValue && d == prev.Value.AddDays(1) ? streak + 1 : 1;
            if (streak > longest) longest = streak;
            prev = d;
        }

        return (current, longest);
    }

    /// <summary>
    /// Returns daily distance totals for the last 52 weeks, keyed by date. DB-only.
    /// </summary>
    public async Task<Dictionary<DateTime, double>> GetHeatmapDataAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.Today.AddDays(-365);

        var rows = await _db.Activities
            .Where(a => a.StartDateLocal >= cutoff)
            .GroupBy(a => a.StartDateLocal.Date)
            .Select(g => new { Date = g.Key, Distance = g.Sum(a => a.Distance) / 1000.0 })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.Date, r => Math.Round(r.Distance, 1));
    }

    // ── Analytics ────────────────────────────────────────────────────────────

    public async Task<List<int>> GetAvailableYearsAsync(CancellationToken cancellationToken = default)
        => await _db.Activities
            .Select(a => a.StartDateLocal.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(cancellationToken);

    public async Task<List<Activity>> GetActivitiesForAnalyticsAsync(string range, int? year, CancellationToken cancellationToken = default)
    {
        var query = _db.Activities.AsQueryable();

        if (range == "month")
        {
            var cutoff = DateTime.UtcNow.AddDays(-30);
            query = query.Where(a => a.StartDateLocal >= cutoff);
        }
        else if (range == "year")
        {
            var selectedYear = year ?? DateTime.UtcNow.Year;
            query = query.Where(a => a.StartDateLocal.Year == selectedYear);
        }

        return await query.ToListAsync(cancellationToken);
    }

    // ── Multi-activity with Details ──────────────────────────────────────────

    public async Task<(List<Activity> activities, Dictionary<long, ActivityDetails> details)> GetActivitiesWithDetailsAsync(
        DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var activities = await _db.Activities
            .Where(a => a.StartDateLocal >= startDate && a.StartDateLocal < endDate)
            .ToListAsync(cancellationToken);

        var ids     = activities.Select(a => a.StravaId).ToList();
        var details = await _db.ActivityDetails
            .Where(d => ids.Contains(d.ActivityId))
            .ToDictionaryAsync(d => d.ActivityId, cancellationToken);

        var missing = activities.Where(a => !details.ContainsKey(a.StravaId)).ToList();
        if (!missing.Any()) return (activities, details);

        var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
        var tasks = missing.Select(async activity =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dto = await _strava.GetActivityDetailsAsync(activity.StravaId);
                return StravaMapper.ToDetails(dto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch details for activity {Id}", activity.StravaId);
                return null;
            }
            finally
            {
                semaphore.Release();
            }
        }).ToArray();

        var fetched = await Task.WhenAll(tasks);

        foreach (var entity in fetched.OfType<ActivityDetails>())
        {
            details[entity.ActivityId] = entity;
            _db.ActivityDetails.Add(entity);
        }

        if (fetched.Any(e => e != null))
            await _db.SaveChangesAsync(cancellationToken);

        return (activities, details);
    }

    // ── DB-only helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns activities in a date range without touching the Strava API.
    /// </summary>
    public Task<List<Activity>> GetActivitiesInRangeAsync(
        DateTime start, DateTime end, CancellationToken cancellationToken = default)
        => _db.Activities
            .Where(a => a.StartDateLocal >= start && a.StartDateLocal < end)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Returns already-cached details for a set of activity IDs.
    /// Never calls the Strava API — missing entries are simply absent from the result.
    /// </summary>
    public Task<Dictionary<long, ActivityDetails>> GetCachedDetailsForAsync(
        IEnumerable<long> stravaIds, CancellationToken cancellationToken = default)
    {
        var ids = stravaIds.ToList();
        return _db.ActivityDetails
            .Where(d => ids.Contains(d.ActivityId))
            .ToDictionaryAsync(d => d.ActivityId, cancellationToken);
    }

    /// <summary>
    /// Loads all activities into memory — used by the Achievements page which needs full rankings.
    /// For anything else, prefer the paged or aggregated query methods above.
    /// </summary>
    public Task<List<Activity>> GetAllActivitiesAsync(CancellationToken cancellationToken = default)
        => _db.Activities.OrderByDescending(a => a.StartDateLocal).ToListAsync(cancellationToken);

    // ── Map ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns polylines for the 500 most active routes (by distance) for the globe map.
    /// </summary>
    public async Task<List<PolylineDto>> GetPolylineDataAsync(CancellationToken cancellationToken = default)
        => await _db.ActivityDetails
            .Where(d => !string.IsNullOrEmpty(d.Polyline))
            .OrderByDescending(d => d.Distance)
            .Take(500)
            .Select(d => new PolylineDto
            {
                Polyline    = d.Polyline,
                MovingTime  = d.MovingTime,
                Distance    = d.Distance
            })
            .ToListAsync(cancellationToken);

    public class PolylineDto
    {
        public string Polyline { get; set; } = "";
        public int    MovingTime { get; set; }
        public double Distance   { get; set; }
    }
}
