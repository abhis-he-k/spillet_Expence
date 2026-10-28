namespace ExpenseSplitterApi.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Who paid
        public int PaidByUserId { get; set; }
        public User? PaidByUser { get; set; }

        // Which group this expense belongs to
        public int GroupId { get; set; }
        public Group? Group { get; set; }

        // The split — who owes how much
        public List<ExpenseSplit> Splits { get; set; } = new();
    }
}