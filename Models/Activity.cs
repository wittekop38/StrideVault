namespace StrideVault.Models
{
    public class Activity
    {
        public int Id { get; set; }
        public long StravaId { get; set; }
        public string Name { get; set; } = "";
        public double Distance { get; set; }
        public int MovingTime { get; set; }
        public int ElapsedTime { get; set; }
        public double TotalElevationGain { get; set; }
        public string Type { get; set; } = "";
        public string SportType { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime StartDateLocal { get; set; }
        public string Timezone { get; set; } = "";
        public double AverageSpeed { get; set; }
        public double MaxSpeed { get; set; }
        public bool Trainer { get; set; }
        public bool Commute { get; set; }
        public bool Manual { get; set; }
        public bool Private { get; set; }
        public bool Flagged { get; set; }
        public string GearId { get; set; } = "";
        public int AchievementCount { get; set; }
        public int KudosCount { get; set; }
        public int CommentCount { get; set; }
        public int AthleteCount { get; set; }
        public int PhotoCount { get; set; }
        public int TotalPhotoCount { get; set; }
        public bool HasHeartrate { get; set; }
        public bool DeviceWatts { get; set; }
        public bool HasKudoed { get; set; }
        public int? WorkoutType { get; set; }
        public decimal? SufferScore { get; set; }
        public string Description { get; set; } = "";
        public long? DetailsId { get; set; }
        public ActivityDetails? Details { get; set; }
    }
}
