using Microsoft.EntityFrameworkCore;
using StravaApiLib;
using StrideVault.Data;
using StrideVault.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<StravaOptions>(builder.Configuration.GetSection("Strava"));
var stravaOptions = builder.Configuration.GetSection("Strava").Get<StravaOptions>();
if (stravaOptions == null || string.IsNullOrEmpty(stravaOptions.ClientId) || string.IsNullOrEmpty(stravaOptions.ClientSecret) || string.IsNullOrEmpty(stravaOptions.RefreshToken))
{
    throw new InvalidOperationException("Strava configuration is missing. Set Strava:ClientId, Strava:ClientSecret and Strava:RefreshToken in configuration or user secrets.");
}

builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<StravaOptions>>().Value;
    return new StravaApi(
        clientId: opts.ClientId,
        clientSecret: opts.ClientSecret,
        refreshToken: opts.RefreshToken,
        timeOutSeconds: opts.TimeoutSeconds
    );
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)
    ));

builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ActivityService>();

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Database migration failed at startup.");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
