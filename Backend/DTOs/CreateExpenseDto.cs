using System.ComponentModel.DataAnnotations;

namespace ExpenseSplitterApi.DTOs
{
public class CreateExpenseDto
    {
        [Required]
        [MinLength(2, ErrorMessage = "Description must be at least 2 characters.")]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        public int TripId { get; set; }

        public int? PaidByUserId { get; set; }

        [MinLength(1, ErrorMessage = "At least one participant is required.")]
        public List<int> ParticipantUserIds { get; set; } = new();
    }
}
