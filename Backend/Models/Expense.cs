using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ExpenseSplitterApi.Models
{
public class Expense
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int TripId { get; set; }
        public Trip? Trip { get; set; }

        public int PaidByUserId { get; set; }
        public User? PaidByUser { get; set; }

        public List<ExpenseSplit> Splits { get; set; } = new();
    }
}
