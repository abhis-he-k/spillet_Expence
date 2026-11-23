using System.ComponentModel.DataAnnotations;

namespace ExpenseSplitterApi.DTOs
{
public class AddMemberDto
    {
        [Required]
        public int UserId { get; set; }
    }
}
