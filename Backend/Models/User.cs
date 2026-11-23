using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ExpenseSplitterApi.Models
{
public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<TripMember> TripMembers { get; set; } = new();
        public List<Expense> ExpensesPaid { get; set; } = new();
        public List<ExpenseSplit> ExpenseSplits { get; set; } = new();
    }
}
