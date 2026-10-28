namespace ExpenseSplitterApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsDeletedPlaceholder { get; set; } = false;   // NEW

        public List<Group> Groups { get; set; } = new List<Group>();
    }
}