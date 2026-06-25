namespace DhaqaleysteApp.Models
{
    public class User
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = "";

        public string Phone { get; set; } = "";

        public string Email { get; set; } = "";

        public string Password { get; set; } = "";

        public string Role { get; set; } = "USER";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}