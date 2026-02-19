namespace SafeWayAPI.Helpers
{
    public class HashHelper
    {
        public static string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}