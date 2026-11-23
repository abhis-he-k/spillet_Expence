using System.ComponentModel.DataAnnotations;

namespace ExpenseSplitterApi.DTOs
{
public class ExpenseSplitResponseDto
    {
        public int Id { get; set; }
        public int ExpenseId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public decimal AmountOwed { get; set; }
        public bool IsPaid { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? SettledAt { get; set; }
    }
}
