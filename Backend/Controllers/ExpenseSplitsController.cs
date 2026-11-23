using ExpenseSplitterApi.Data;
using ExpenseSplitterApi.DTOs;
using ExpenseSplitterApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseSplitterApi.Controllers
{
[ApiController]
    [Route("api/[controller]")]
    public class ExpenseSplitsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ExpenseSplitsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseSplitResponseDto>> GetSplit(int id)
        {
            var split = await _context.ExpenseSplits
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (split == null)
            {
                return NotFound("Expense split not found.");
            }

            return Ok(new ExpenseSplitResponseDto
            {
                Id = split.Id,
                ExpenseId = split.ExpenseId,
                UserId = split.UserId,
                UserName = split.User?.Name ?? string.Empty,
                AmountOwed = split.AmountOwed,
                IsPaid = split.IsPaid,
                Status = split.IsPaid ? "Settled" : "Pending",
                SettledAt = split.SettledAt
            });
        }

        [HttpPut("{id}/settle")]
        public async Task<ActionResult<ExpenseSplitResponseDto>> Settle(int id)
        {
            var split = await _context.ExpenseSplits
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (split == null)
            {
                return NotFound("Expense split not found.");
            }

            split.IsPaid = true;
            split.SettledAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new ExpenseSplitResponseDto
            {
                Id = split.Id,
                ExpenseId = split.ExpenseId,
                UserId = split.UserId,
                UserName = split.User?.Name ?? string.Empty,
                AmountOwed = split.AmountOwed,
                IsPaid = split.IsPaid,
                Status = "Settled",
                SettledAt = split.SettledAt
            });
        }

        [HttpPatch("{id}/settle")]
        public async Task<ActionResult<ExpenseSplitResponseDto>> SettlePatch(int id)
        {
            return await Settle(id);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ExpenseSplitResponseDto>> Update(int id, UpdateSplitStatusDto dto)
        {
            var split = await _context.ExpenseSplits
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (split == null)
            {
                return NotFound("Expense split not found.");
            }

            split.IsPaid = dto.IsPaid;
            split.SettledAt = dto.IsPaid ? DateTime.UtcNow : null;

            await _context.SaveChangesAsync();

            return Ok(new ExpenseSplitResponseDto
            {
                Id = split.Id,
                ExpenseId = split.ExpenseId,
                UserId = split.UserId,
                UserName = split.User?.Name ?? string.Empty,
                AmountOwed = split.AmountOwed,
                IsPaid = split.IsPaid,
                Status = split.IsPaid ? "Settled" : "Pending",
                SettledAt = split.SettledAt
            });
        }
    }
}
