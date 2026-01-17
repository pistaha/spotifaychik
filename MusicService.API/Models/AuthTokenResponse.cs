namespace MusicService.API.Models
{
    public sealed class AuthTokenResponse
    {
        /// <summary>access token для api</summary>
        public string AccessToken { get; set; } = string.Empty;
        /// <summary>тип токена</summary>
        public string TokenType { get; set; } = "Bearer";
        /// <summary>срок жизни в минутах</summary>
        public int ExpiresInMinutes { get; set; }
    }
}
