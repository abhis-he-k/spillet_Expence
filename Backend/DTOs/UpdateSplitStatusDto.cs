using System.ComponentModel.DataAnnotations;

namespace ExpenseSplitterApi.DTOs
{
public class UpdateSplitStatusDto
    {
        public bool IsPaid { get; set; } = true;
    }
}
