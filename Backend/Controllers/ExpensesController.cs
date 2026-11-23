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
    public class ExpensesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ExpensesController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : null;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseResponseDto>> GetExpense(int id)
        {
            var expense = await _context.Expenses
                .Include(e => e.Trip)
                .Include(e => e.PaidByUser)
                .Include(e => e.Splits)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (expense == null)
            {
                return NotFound("Expense not found.");
            }

            var response = new ExpenseResponseDto
            {
                Id = expense.Id,
                Description = expense.Description,
                Amount = expense.Amount,
                CreatedAt = expense.CreatedAt,
                TripId = expense.TripId,
                TripTitle = expense.Trip?.Title ?? string.Empty,
                PaidByUserId = expense.PaidByUserId,
                PaidByUserName = expense.PaidByUser?.Name ?? string.Empty,
                Splits = expense.Splits.Select(s => new ExpenseSplitResponseDto
                {
                    Id = s.Id,
                    ExpenseId = s.ExpenseId,
                    UserId = s.UserId,
                    UserName = s.User?.Name ?? string.Empty,
                    AmountOwed = s.AmountOwed,
                    IsPaid = s.IsPaid,
                    Status = s.IsPaid ? "Settled" : "Pending",
                    SettledAt = s.SettledAt
                }).ToList()
            };

            return Ok(response);
        }

        [HttpGet("trip/{tripId}")]
        public async Task<ActionResult<IEnumerable<ExpenseResponseDto>>> GetExpensesForTrip(int tripId)
        {
            var tripExists = await _context.Trips.AnyAsync(t => t.Id == tripId);
            if (!tripExists)
            {
                return NotFound("Trip not found.");
            }

            var expenses = await _context.Expenses
                .Where(e => e.TripId == tripId)
                .Include(e => e.Trip)
                .Include(e => e.PaidByUser)
                .Include(e => e.Splits)
                    .ThenInclude(s => s.User)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new ExpenseResponseDto
                {
                    Id = e.Id,
                    Description = e.Description,
                    Amount = e.Amount,
                    CreatedAt = e.CreatedAt,
                    TripId = e.TripId,
                    TripTitle = e.Trip != null ? e.Trip.Title : string.Empty,
                    PaidByUserId = e.PaidByUserId,
                    PaidByUserName = e.PaidByUser != null ? e.PaidByUser.Name : string.Empty,
                    Splits = e.Splits.Select(s => new ExpenseSplitResponseDto
                    {
                        Id = s.Id,
                        ExpenseId = s.ExpenseId,
                        UserId = s.UserId,
                        UserName = s.User != null ? s.User.Name : string.Empty,
                        AmountOwed = s.AmountOwed,
                        IsPaid = s.IsPaid,
                        Status = s.IsPaid ? "Settled" : "Pending",
                        SettledAt = s.SettledAt
                    }).ToList()
                })
                .ToListAsync();

            return Ok(expenses);
        }

        [HttpPost]
        public async Task<ActionResult<ExpenseResponseDto>> CreateExpense(CreateExpenseDto dto)
        {
            var trip = await _context.Trips
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == dto.TripId);

            if (trip == null)
            {
                return NotFound("Trip not found.");
            }

            var payerId = dto.PaidByUserId ?? GetCurrentUserId();
            if (!payerId.HasValue)
            {
                return BadRequest("PaidByUserId is required.");
            }

            var payer = await _context.Users.FindAsync(payerId.Value);
            if (payer == null)
            {
                return BadRequest("Payer not found.");
            }

            if (!trip.Members.Any(m => m.UserId == payerId.Value))
            {
                return BadRequest("Payer must be a member of the trip.");
            }

            if (dto.ParticipantUserIds == null || dto.ParticipantUserIds.Count == 0)
            {
                return BadRequest("At least one participant is required.");
            }

            var distinctParticipants = dto.ParticipantUserIds.Distinct().ToList();

            var tripMemberIds = trip.Members.Select(m => m.UserId).ToHashSet();
            foreach (var participantId in distinctParticipants)
            {
                if (!tripMemberIds.Contains(participantId))
                {
                    return BadRequest($"Participant with user ID {participantId} is not a member of this trip.");
                }
            }

            var expense = new Expense
            {
                Description = dto.Description.Trim(),
                Amount = dto.Amount,
                TripId = dto.TripId,
                PaidByUserId = payerId.Value,
                CreatedAt = DateTime.UtcNow
            };

            var count = distinctParticipants.Count;
            var totalCents = (int)Math.Round(dto.Amount * 100m, MidpointRounding.AwayFromZero);
            var baseCents = totalCents / count;
            var remainderCents = totalCents % count;

            for (var i = 0; i < count; i++)
            {
                var participantId = distinctParticipants[i];
                var shareCents = baseCents + (i < remainderCents ? 1 : 0);
                var shareAmount = shareCents / 100m;

                var isPaid = participantId == payerId.Value;

                expense.Splits.Add(new ExpenseSplit
                {
                    UserId = participantId,
                    AmountOwed = shareAmount,
                    IsPaid = isPaid,
                    SettledAt = isPaid ? DateTime.UtcNow : null
                });
            }

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            return await GetExpense(expense.Id);
        }

        [HttpPut("splits/{splitId}/settle")]
        public async Task<ActionResult<ExpenseSplitResponseDto>> SettleSplit(int splitId)
        {
            var split = await _context.ExpenseSplits
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == splitId);

            if (split == null)
            {
                return NotFound("Expense split not found.");
            }

            split.IsPaid = true;
            split.SettledAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new ExpenseSplitResponseDto
            {
                Id = split.Id,
                ExpenseId = split.ExpenseId,
                UserId = split.UserId,
                UserName = split.User?.Name ?? string.Empty,
                AmountOwed = split.AmountOwed,
                IsPaid = split.IsPaid,
                Status = "Settled",
                SettledAt = split.SettledAt
            };

            return Ok(response);
        }

        [HttpPatch("splits/{splitId}/settle")]
        public async Task<ActionResult<ExpenseSplitResponseDto>> SettleSplitPatch(int splitId)
        {
            return await SettleSplit(splitId);
        }

        [HttpPut("splits/{splitId}")]
        public async Task<ActionResult<ExpenseSplitResponseDto>> UpdateSplit(int splitId, UpdateSplitStatusDto dto)
        {
            var split = await _context.ExpenseSplits
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == splitId);

            if (split == null)
            {
                return NotFound("Expense split not found.");
            }

            split.IsPaid = dto.IsPaid;
            split.SettledAt = dto.IsPaid ? DateTime.UtcNow : null;

            await _context.SaveChangesAsync();

            var response = new ExpenseSplitResponseDto
            {
                Id = split.Id,
                ExpenseId = split.ExpenseId,
                UserId = split.UserId,
                UserName = split.User?.Name ?? string.Empty,
                AmountOwed = split.AmountOwed,
                IsPaid = split.IsPaid,
                Status = split.IsPaid ? "Settled" : "Pending",
                SettledAt = split.SettledAt
            };

            return Ok(response);
        }

        [HttpGet("dashboard/{userId}")]
        public async Task<ActionResult<UserDashboardDto>> GetDashboardBalances(int userId)
        {
            var usersController = new UsersController(_context);
            return await usersController.GetUserDashboard(userId);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
            {
                return NotFound("Expense not found.");
            }

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
