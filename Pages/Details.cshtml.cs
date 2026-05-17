using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StrideVault.Models;

namespace StrideVault.Pages
{
    public class DetailsModel : PageModel
    {
        private readonly ActivityService _activityService;

        public Activity Activity { get; set; } = null!;
        public ActivityDetails Details { get; set; } = null!;

        public DetailsModel(ActivityService activityService)
        {
            _activityService = activityService;
        }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Activity = await _activityService.GetActivityAsync(id) ?? null!;
            if (Activity == null) return NotFound();

            Details = await _activityService.GetOrFetchDetailsAsync(id);
            return Page();
        }
    }
}