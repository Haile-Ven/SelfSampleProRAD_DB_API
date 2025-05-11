namespace SelfSampleProRAD_DB_API.Models
{
    public class JwtSettings
    {
        public string Secret { get; set; }
        public int TokenExpirationMinutes { get; set; }
    }
}
