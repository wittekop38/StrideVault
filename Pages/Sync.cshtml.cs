using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class SyncModel : PageModel
{
    private readonly ActivityService _activityService;
    private readonly ILogger<SyncModel> _logger;

    public SyncModel(ActivityService activityService, ILogger<SyncModel> logger)
    {
        _activityService = activityService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _activityService.SyncActivitiesAsync(cancellationToken);
            TempData["SyncSuccess"] = "Activities synced successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed");
            TempData["SyncError"] = "Sync failed. Check logs for details.";
        }

        return RedirectToPage("/Index");
    }
}
