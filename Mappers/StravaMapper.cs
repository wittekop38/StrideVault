using System;
using StravaApiLib.DTOs;
using StravaApiLib.DTOs.Activities;
using StrideVault.Models;

public static class StravaMapper
{
    public static Activity ToActivity(ActivityDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        return new Activity
        {
            StravaId = dto.Id,
            Name = dto.Name ?? "Unnamed",
            Distance = dto.Distance,
            MovingTime = dto.MovingTime,
            ElapsedTime = dto.ElapsedTime,
            TotalElevationGain = dto.TotalElevationGain,
            Type = dto.Type ?? "Unknown",
            SportType = dto.SportType ?? "Unknown",
            StartDate = dto.StartDate,
            StartDateLocal = dto.StartDateLocal,
            Timezone = dto.Timezone ?? string.Empty,
            AverageSpeed = dto.AverageSpeed,
            MaxSpeed = dto.MaxSpeed,
            Trainer = dto.Trainer,
            Commute = dto.Commute,
            Manual = dto.Manual,
            Private = dto.Private,
            Flagged = dto.Flagged,
            GearId = dto.GearId ?? string.Empty,
            AchievementCount = dto.AchievementCount,
            KudosCount = dto.KudosCount,
            CommentCount = dto.CommentCount,
            AthleteCount = dto.AthleteCount,
            PhotoCount = dto.PhotoCount,
            TotalPhotoCount = dto.TotalPhotoCount,
            HasHeartrate = dto.HasHeartrate,
            DeviceWatts = dto.DeviceWatts,
            HasKudoed = dto.HasKudoed,
            WorkoutType = dto.WorkoutType,
            SufferScore = dto.SufferScore ?? 0,
            Description = "",
            DetailsId = null
        };
    }

    public static ActivityDetails ToDetails(ActivityDetailsDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        return new ActivityDetails
        {
            ActivityId = dto.Id,
            Distance = dto.Distance,
            MovingTime = dto.MovingTime,
            ElevationGain = dto.TotalElevationGain,
            AverageSpeed = dto.AverageSpeed,
            MaxSpeed = dto.MaxSpeed,
            AverageWatts = dto.AverageWatts,
            Calories = dto.Calories,
            Polyline = dto.Map?.Polyline ?? string.Empty
        };
    }
}