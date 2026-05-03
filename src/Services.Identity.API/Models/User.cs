namespace Services.Identity.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; }
        public string? FullName { get; set; }
        public bool IsActive { get; set; } = true;


    }
}
