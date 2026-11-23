using System.ComponentModel.DataAnnotations;

namespace ExpenseSplitterApi.DTOs
{
public class TripResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public List<TripMemberResponseDto> Members { get; set; } = new();
        public List<ExpenseSummaryDto> Expenses { get; set; } = new();
    }
}
