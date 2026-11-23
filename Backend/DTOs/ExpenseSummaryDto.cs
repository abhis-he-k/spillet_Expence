using System.ComponentModel.DataAnnotations;

namespace ExpenseSplitterApi.DTOs
{
public class ExpenseSummaryDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int PaidByUserId { get; set; }
        public string PaidByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<ExpenseSplitResponseDto> Splits { get; set; } = new();
    }
}
