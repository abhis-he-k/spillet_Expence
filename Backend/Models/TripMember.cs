using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ExpenseSplitterApi.Models
{
public class TripMember
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        public Trip? Trip { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
