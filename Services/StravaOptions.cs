namespace StrideVault.Services
{
    public class StravaOptions
    {
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public int TimeoutSeconds { get; set; } = 30;
    }
}
