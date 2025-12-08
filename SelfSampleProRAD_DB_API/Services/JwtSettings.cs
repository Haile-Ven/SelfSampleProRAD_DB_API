namespace SelfSampleProRAD_DB_API.Services
{
    public class JwtSettings
    {
        public string Secret { get; set; }
        public int TokenExpirationMinutes { get; set; }
    }
}
