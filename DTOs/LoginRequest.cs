namespace SafeWayAPI.DTOs
{
    public class LoginRequest
    {
        public string UniqueID { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}