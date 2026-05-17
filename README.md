# StrideVault

A personal fitness dashboard built because Strava's own interface never showed data the way I wanted to see it. StrideVault pulls your activities from the Strava API and gives you a clean, fast, dark-themed view of everything — a 52-week activity heatmap, cumulative analytics charts, a monthly planner calendar, a 3D globe of all your routes, and an achievements page with milestones and leaderboards.

It's still early and intentionally simple. The goal isn't to clone Strava — it's to have a personal space to explore your own data in ways that actually make sense to you. Contributions and ideas are welcome.

---

## Features

| Page | What it shows |
|---|---|
| **Dashboard** | Stat cards, 52-week activity heatmap with streak badges, 7-day bar chart, paginated activity list with type filter |
| **Achievements** | Milestone badges, personal bests (fastest/longest per type), top-5 leaderboards |
| **Analytics** | Cumulative + per-period distance charts for rides, runs, and walks; year-vs-year comparison overlay |
| **Planner** | Monthly calendar with per-day activity dots, training load indicator, clickable entries, and weekly totals |
| **Map** | Cesium.js 3D globe showing all your routes as lines or a heatmap of points |
| **Details** | Per-activity Leaflet map with route polyline, stat cards (distance, time, elevation, speed, power, calories) |

---

## Tech stack

- **ASP.NET Core 8** — Razor Pages (no MVC, no React)
- **Entity Framework Core 8** — SQL Server, code-first migrations
- **StravaApiLib** — NuGet package for the Strava v3 REST API
- **Leaflet.js** — activity route maps (CartoDB dark tiles)
- **Cesium.js** — 3D globe map
- **Chart.js** — dashboard bar chart and analytics line charts
- No Bootstrap, no jQuery — custom CSS design system

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB works fine for local dev — ships with Visual Studio)
- A Strava account with API access
- A [Cesium Ion](https://cesium.com/ion/) account (free tier) for the globe map

---

## Local setup

### 1. Clone the repo

```bash
git clone https://github.com/YOUR_USERNAME/StrideVault.git
cd StrideVault
```

### 2. Configure secrets

Copy the example config and fill in your credentials:

```bash
cp appsettings.Example.json appsettings.json
```

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=StrideVault;Trusted_Connection=True;"
  },
  "Strava": {
    "ClientId":     "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "RefreshToken": "YOUR_REFRESH_TOKEN"
  },
  "Cesium": {
    "IonToken": "YOUR_CESIUM_ION_TOKEN"
  }
}
```

> **`appsettings.json` is gitignored** — your credentials will never be committed. Only `appsettings.Example.json` (with empty placeholders) is tracked.

### 3. Apply database migrations

```bash
dotnet ef database update
```

This creates the database and all tables. If you don't have the EF CLI tool yet:

```bash
dotnet tool install --global dotnet-ef
```

### 4. Run

```bash
dotnet run
```

Open `https://localhost:5001` (or whatever port is shown). Hit **Sync** on the dashboard to import your Strava activities.

---

## Strava API setup

1. Go to [strava.com/settings/api](https://www.strava.com/settings/api) and create an application.  
   - **Authorization Callback Domain**: `localhost`
2. Note your **Client ID** and **Client Secret**.
3. Get a refresh token with the `activity:read_all` scope. The easiest way:
   - Visit `http://www.strava.com/oauth/authorize?client_id=YOUR_CLIENT_ID&response_type=code&redirect_uri=http://localhost/exchange_token&approval_prompt=force&scope=activity:read_all`
   - Approve the app — you'll be redirected to a URL like `http://localhost/exchange_token?code=SOME_CODE`
   - Exchange the code for tokens:
     ```bash
     curl -X POST https://www.strava.com/oauth/token \
       -d client_id=YOUR_CLIENT_ID \
       -d client_secret=YOUR_CLIENT_SECRET \
       -d code=SOME_CODE \
       -d grant_type=authorization_code
     ```
   - Copy the `refresh_token` from the response into `appsettings.json`.

The app uses the refresh token to silently obtain a new access token on every sync — you never need to log in again.

---

## Database

StrideVault uses two tables:

| Table | Contents |
|---|---|
| `Activities` | Summary of every synced activity (distance, time, elevation, type, speed, etc.) |
| `ActivityDetails` | Extended data fetched on demand when you open an activity (polyline, calories, power, etc.) |

Details are cached locally on first view — the Strava API is only called once per activity.

### Migrations

```bash
# Create a new migration after model changes
dotnet ef migrations add MigrationName

# Apply pending migrations
dotnet ef database update

# Roll back one migration
dotnet ef database update PreviousMigrationName
```

---

## Project structure

```
StrideVault/
├── Data/
│   └── AppDbContext.cs          # EF Core DB context
├── Mappers/
│   └── StravaMapper.cs          # Maps Strava API DTOs → EF entities
├── Models/
│   ├── Activity.cs
│   ├── ActivityDetails.cs
│   └── DashboardStats.cs
├── Pages/
│   ├── Index.cshtml(.cs)        # Dashboard
│   ├── Achievements.cshtml(.cs)
│   ├── Analytics.cshtml(.cs)
│   ├── Details.cshtml(.cs)
│   ├── Map.cshtml(.cs)
│   ├── Planner.cshtml(.cs)
│   ├── Sync.cshtml(.cs)         # POST endpoint — triggers Strava sync
│   └── Shared/_Layout.cshtml
├── Services/
│   └── ActivityService.cs       # All DB queries and Strava API calls
├── wwwroot/
│   ├── css/site.css
│   └── js/site.js               # Shared polyline decoder
├── appsettings.Example.json     # Template — copy to appsettings.json
└── appsettings.json             # ⚠ gitignored — contains your secrets
```

---

## Roadmap / ideas

- Elevation profile chart on activity detail page
- Per-km split table
- Search / filter activities by name
- Route colour on the globe by activity type
- Export activities as CSV
- Public profile sharing

---

## Contributing

This is a personal project but PRs and issues are welcome. If something is broken or you have an idea that fits the "show your own data, your way" goal, open an issue.

---

## License

MIT
