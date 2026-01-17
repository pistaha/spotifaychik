namespace MusicService.API.Authentication
{
    public sealed class JwtSettings
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public int AccessTokenExpirationMinutes { get; set; } = 20;
        public int RefreshTokenExpirationDays { get; set; } = 14;
    }
}
