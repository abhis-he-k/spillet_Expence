using System.ComponentModel.DataAnnotations;

namespace ExpenseSplitterApi.DTOs
{
public class TripMemberResponseDto
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }
}
