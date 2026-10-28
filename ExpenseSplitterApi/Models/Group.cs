namespace ExpenseSplitterApi.Models
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }

        public User? CreatedBy { get; set; }
        public List<User> Members { get; set; } = new List<User>();
    }
}