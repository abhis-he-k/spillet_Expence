using System.ComponentModel.DataAnnotations;

namespace ExpenseSplitterApi.DTOs
{
public class CreateTripDto
    {
        [Required]
        [MinLength(2, ErrorMessage = "Trip title must be at least 2 characters.")]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int? CreatedByUserId { get; set; }

        public List<int> InitialMemberUserIds { get; set; } = new();
    }
}
