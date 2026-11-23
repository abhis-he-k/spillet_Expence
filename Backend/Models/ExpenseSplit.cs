using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ExpenseSplitterApi.Models
{
public class ExpenseSplit
    {
        public int Id { get; set; }
        public int ExpenseId { get; set; }
        public Expense? Expense { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public decimal AmountOwed { get; set; }
        public bool IsPaid { get; set; } = false;
        public DateTime? SettledAt { get; set; }

        [NotMapped]
        public decimal Amount
        {
            get => AmountOwed;
            set => AmountOwed = value;
        }
    }
}
