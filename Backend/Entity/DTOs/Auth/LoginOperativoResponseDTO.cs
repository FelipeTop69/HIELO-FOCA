namespace Entity.DTOs.Auth
{
    public class LoginOperativoResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
    }
}
