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
    }
}