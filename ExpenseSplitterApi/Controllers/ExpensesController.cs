using ExpenseSplitterApi.Data;
using ExpenseSplitterApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace ExpenseSplitterApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ExpensesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ExpensesController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        private async Task<bool> IsGroupMember(int groupId, int userId)
        {
            return await _context.Groups
                .Where(g => g.Id == groupId)
                .SelectMany(g => g.Members)
                .AnyAsync(m => m.Id == userId);
        }

        // GET: api/expenses/group/5
        [HttpGet("group/{groupId}")]
        public async Task<ActionResult<IEnumerable<Expense>>> GetExpensesForGroup(int groupId)
        {

            var userId = GetUserId();                          // ADD THIS
            if (!await IsGroupMember(groupId, userId))          // ADD THIS
                return Forbid();

            var expenses = await _context.Expenses
                .Where(e => e.GroupId == groupId)
                .Include(e => e.PaidByUser)
                .Include(e => e.Splits)
                    .ThenInclude(s => s.User)
                .ToListAsync();

            return Ok(expenses);
        }

        // GET: api/expenses/settle-up/5
    [HttpGet("settle-up/{groupId}")]
    public async Task<ActionResult<IEnumerable<SettlementDto>>> GetSettleUp(int groupId)
    {
        var group = await _context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) return NotFound("Group not found");
        var currentUserId = GetUserId();                              // ADD THIS
        if (!group.Members.Any(m => m.Id == currentUserId))           // ADD THIS
            return Forbid();     

        // Step 1: calculate net balance per user
        var balances = group.Members.ToDictionary(m => m.Id, m => 0m);

        var expenses = await _context.Expenses
            .Where(e => e.GroupId == groupId)
            .Include(e => e.Splits)
            .ToListAsync();

        foreach (var expense in expenses)
        {
            // Payer gets credited the full amount
            if (balances.ContainsKey(expense.PaidByUserId))
                balances[expense.PaidByUserId] += expense.Amount;

            // Each participant gets debited their share
            foreach (var split in expense.Splits)
            {
                if (balances.ContainsKey(split.UserId))
                    balances[split.UserId] -= split.AmountOwed;
            }
        }

        var pastsettlements = await _context.Settlements
            .Where(s => s.GroupId == groupId)
            .ToListAsync();

        foreach (var settlement in pastsettlements)
        {
            if (balances.ContainsKey(settlement.FromUserId))
                balances[settlement.FromUserId] += settlement.Amount;

            if (balances.ContainsKey(settlement.ToUserId))
                balances[settlement.ToUserId] -= settlement.Amount;
        }

        // Step 2: separate into creditors and debtors
        var creditors = balances.Where(b => b.Value > 0)
            .Select(b => new { UserId = b.Key, Amount = b.Value })
            .OrderByDescending(b => b.Amount)
            .ToList();

        var debtors = balances.Where(b => b.Value < 0)
            .Select(b => new { UserId = b.Key, Amount = -b.Value }) // flip to positive for easier math
            .OrderByDescending(b => b.Amount)
            .ToList();

        // Step 3: greedy matching
        var settlements = new List<SettlementDto>();
        int i = 0, j = 0;

        // Use mutable copies since we need to adjust amounts as we go
        var creditorAmounts = creditors.Select(c => c.Amount).ToList();
        var debtorAmounts = debtors.Select(d => d.Amount).ToList();

        while (i < debtors.Count && j < creditors.Count)
        {
            decimal payment = Math.Min(debtorAmounts[i], creditorAmounts[j]);

            if (payment > 0)
            {
                var fromUser = await _context.Users.FindAsync(debtors[i].UserId);
                var toUser = await _context.Users.FindAsync(creditors[j].UserId);

                settlements.Add(new SettlementDto
                {
                    FromUserId = debtors[i].UserId,
                    FromUserName = fromUser?.Name ?? "Unknown",
                    ToUserId = creditors[j].UserId,
                    ToUserName = toUser?.Name ?? "Unknown",
                    Amount = Math.Round(payment, 2)
                });
            }

            debtorAmounts[i] -= payment;
            creditorAmounts[j] -= payment;

            if (debtorAmounts[i] == 0) i++;
            if (creditorAmounts[j] == 0) j++;
        }

        return Ok(settlements);
    }

        // GET: api/expenses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Expense>> GetExpense(int id)
        {
            var expense = await _context.Expenses
                .Include(e => e.PaidByUser)
                .Include(e => e.Splits)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (expense == null) return NotFound();

            var userId = GetUserId();
            if (!await IsGroupMember(expense.GroupId, userId))
                return Forbid();

            return Ok(expense);
        }

        // POST: api/expenses
        [HttpPost]
        public async Task<ActionResult<Expense>> CreateExpense(CreateExpenseDto dto)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == dto.GroupId);

            if (group == null) return NotFound("Group not found");

            var userId = GetUserId();
            if (!group.Members.Any(m => m.Id == userId))
                return Forbid();

            var payer = await _context.Users.FindAsync(dto.PaidByUserId);
            if (payer == null) return NotFound("Payer not found");

            if (dto.ParticipantUserIds == null || dto.ParticipantUserIds.Count == 0)
                return BadRequest("At least one participant is required");

            var expense = new Expense
            {
                Description = dto.Description,
                Amount = dto.Amount,
                PaidByUserId = dto.PaidByUserId,
                GroupId = dto.GroupId,
                CreatedAt = DateTime.UtcNow
            };

            decimal splitAmount = Math.Round(dto.Amount / dto.ParticipantUserIds.Count, 2);

            foreach (var pid in dto.ParticipantUserIds)
            {
                expense.Splits.Add(new ExpenseSplit
                {
                    UserId = pid,
                    AmountOwed = splitAmount
                });
            }

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, expense);
        }

        // DELETE: api/expenses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null) return NotFound();

            var userId = GetUserId();
            if (!await IsGroupMember(expense.GroupId, userId))
                return Forbid();

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

public class CreateExpenseDto
{
    [Required]
    [MinLength(2, ErrorMessage = "Description must be at least 2 characters.")]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    public int PaidByUserId { get; set; }
    public int GroupId { get; set; }

    [MinLength(1, ErrorMessage = "At least one participant is required.")]
    public List<int> ParticipantUserIds { get; set; } = new();
}
public class SettlementDto
{
    public int FromUserId { get; set; }
    public string FromUserName { get; set; } = string.Empty;
    public int ToUserId { get; set; }
    public string ToUserName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

}