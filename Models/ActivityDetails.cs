namespace StrideVault.Models
{
    public class ActivityDetails
    {
        public long Id { get; set; }
        public long ActivityId { get; set; }   // FK

        public double Distance { get; set; }
        public int MovingTime { get; set; }
        public double ElevationGain { get; set; }

        public double AverageSpeed { get; set; }
        public double MaxSpeed { get; set; }

        public double? AverageWatts { get; set; }
        public double? Calories { get; set; }

        public string Polyline { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Activity? Activity { get; set; }
    }
}
