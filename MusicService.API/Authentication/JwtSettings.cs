namespace MusicService.API.Authentication
{
    public sealed class JwtSettings
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public int ExpiresMinutes { get; set; } = 60;
    }
}
