using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class MapModel : PageModel
{
    private readonly ActivityService _activityService;
    private readonly IConfiguration _config;

    public string CesiumToken { get; private set; } = "";

    public MapModel(ActivityService activityService, IConfiguration config)
    {
        _activityService = activityService;
        _config = config;
    }

    public void OnGet()
    {
        CesiumToken = _config["Cesium:IonToken"] ?? "";
    }

    public async Task<IActionResult> OnGetPolylinesAsync()
    {
        var data = await _activityService.GetPolylineDataAsync();
        return new JsonResult(data);
    }
}