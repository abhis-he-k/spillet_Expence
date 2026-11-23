using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ExpenseSplitterApi.Models
{
public class Trip
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        [NotMapped]
        public string Name
        {
            get => Title;
            set => Title = value;
        }

        public List<TripMember> Members { get; set; } = new();
        public List<Expense> Expenses { get; set; } = new();
    }
}
