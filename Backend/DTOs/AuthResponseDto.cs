using System.ComponentModel.DataAnnotations;

namespace ExpenseSplitterApi.DTOs
{
public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserResponseDto User { get; set; } = new();
    }
}
