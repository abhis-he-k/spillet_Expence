using System.ComponentModel.DataAnnotations;

namespace ExpenseSplitterApi.DTOs
{
public class ExpenseResponseDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TripId { get; set; }
        public string TripTitle { get; set; } = string.Empty;
        public int PaidByUserId { get; set; }
        public string PaidByUserName { get; set; } = string.Empty;
        public List<ExpenseSplitResponseDto> Splits { get; set; } = new();
    }
}
