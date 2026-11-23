using System.ComponentModel.DataAnnotations;

namespace ExpenseSplitterApi.DTOs
{
public class UserDashboardDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public decimal TotalOwed { get; set; }
        public decimal TotalReceivable { get; set; }
        public decimal NetBalance { get; set; }
        public List<UserOwedExpenseDto> WhatYouOwe { get; set; } = new();
        public List<UserLentExpenseDto> WhatOthersOweYou { get; set; } = new();
        public List<DashboardBalanceItemDto> AllBalances { get; set; } = new();
    }
}
