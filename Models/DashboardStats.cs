namespace StrideVault.Models
{
    public class DashboardStats
    {
        public double TotalDistanceKm { get; set; }
        public double WeeklyDistanceKm { get; set; }

        public int TotalRides { get; set; }
        public int TotalRuns { get; set; }
        public int TotalWalks { get; set; }
        public int TotalWeightTraining { get; set; }

        public double TotalRunDistanceKm { get; set; }
        public double TotalRideDistanceKm { get; set; }
        public double TotalWalkDistanceKm { get; set; }

        public double TotalElevation { get; set; }
        public double WeeklyElevation { get; set; }

        public int ActiveDaysLast7 { get; set; }

        public int TotalActivities { get; set; }
        public long TotalMovingTimeSecs { get; set; }
    }
}
