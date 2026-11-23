using System.ComponentModel.DataAnnotations;

namespace ExpenseSplitterApi.DTOs
{
public class UserLentExpenseDto
    {
        public int SplitId { get; set; }
        public int ExpenseId { get; set; }
        public string ExpenseDescription { get; set; } = string.Empty;
        public int TripId { get; set; }
        public string TripTitle { get; set; } = string.Empty;
        public decimal AmountOwed { get; set; }
        public int DebtorUserId { get; set; }
        public string DebtorUserName { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? SettledAt { get; set; }
    }
}
