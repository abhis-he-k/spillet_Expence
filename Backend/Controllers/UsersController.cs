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
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
        {
            var users = await _context.Users
                .OrderBy(u => u.Id)
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponseDto>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            });
        }

        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> CreateUser(RegisterDto dto)
        {
            var emailLower = dto.Email.Trim().ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == emailLower))
            {
                return BadRequest("Email already registered.");
            }

            var user = new User
            {
                Name = dto.Name.Trim(),
                Email = emailLower,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var response = new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, response);
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserResponseDto>> Register(RegisterDto dto)
        {
            return await CreateUser(dto);
        }

        [HttpGet("{id}/dashboard")]
        public async Task<ActionResult<UserDashboardDto>> GetUserDashboard(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var splitsWhereDebtor = await _context.ExpenseSplits
                .Include(s => s.Expense)
                    .ThenInclude(e => e!.Trip)
                .Include(s => s.Expense)
                    .ThenInclude(e => e!.PaidByUser)
                .Where(s => s.UserId == id && s.Expense != null && s.Expense.PaidByUserId != id)
                .OrderByDescending(s => s.Expense!.CreatedAt)
                .ToListAsync();

            var splitsWhereCreditor = await _context.ExpenseSplits
                .Include(s => s.User)
                .Include(s => s.Expense)
                    .ThenInclude(e => e!.Trip)
                .Where(s => s.Expense != null && s.Expense.PaidByUserId == id && s.UserId != id)
                .OrderByDescending(s => s.Expense!.CreatedAt)
                .ToListAsync();

            var whatYouOwe = splitsWhereDebtor.Select(s => new UserOwedExpenseDto
            {
                SplitId = s.Id,
                ExpenseId = s.ExpenseId,
                ExpenseDescription = s.Expense?.Description ?? string.Empty,
                TripId = s.Expense?.TripId ?? 0,
                TripTitle = s.Expense?.Trip?.Title ?? string.Empty,
                AmountOwed = s.AmountOwed,
                PaidByUserId = s.Expense?.PaidByUserId ?? 0,
                PaidByUserName = s.Expense?.PaidByUser?.Name ?? string.Empty,
                IsPaid = s.IsPaid,
                Status = s.IsPaid ? "Settled" : "Pending",
                CreatedAt = s.Expense?.CreatedAt ?? DateTime.UtcNow,
                SettledAt = s.SettledAt
            }).ToList();

            var whatOthersOweYou = splitsWhereCreditor.Select(s => new UserLentExpenseDto
            {
                SplitId = s.Id,
                ExpenseId = s.ExpenseId,
                ExpenseDescription = s.Expense?.Description ?? string.Empty,
                TripId = s.Expense?.TripId ?? 0,
                TripTitle = s.Expense?.Trip?.Title ?? string.Empty,
                AmountOwed = s.AmountOwed,
                DebtorUserId = s.UserId,
                DebtorUserName = s.User?.Name ?? string.Empty,
                IsPaid = s.IsPaid,
                Status = s.IsPaid ? "Settled" : "Pending",
                CreatedAt = s.Expense?.CreatedAt ?? DateTime.UtcNow,
                SettledAt = s.SettledAt
            }).ToList();

            var allBalances = new List<DashboardBalanceItemDto>();

            foreach (var s in splitsWhereDebtor)
            {
                allBalances.Add(new DashboardBalanceItemDto
                {
                    SplitId = s.Id,
                    ExpenseId = s.ExpenseId,
                    ExpenseDescription = s.Expense?.Description ?? string.Empty,
                    TripId = s.Expense?.TripId ?? 0,
                    TripTitle = s.Expense?.Trip?.Title ?? string.Empty,
                    AmountOwed = s.AmountOwed,
                    PaidByUserId = s.Expense?.PaidByUserId ?? 0,
                    PaidByUserName = s.Expense?.PaidByUser?.Name ?? string.Empty,
                    DebtorUserId = s.UserId,
                    DebtorUserName = user.Name,
                    IsPaid = s.IsPaid,
                    Status = s.IsPaid ? "Settled" : "Pending",
                    CreatedAt = s.Expense?.CreatedAt ?? DateTime.UtcNow,
                    SettledAt = s.SettledAt
                });
            }

            foreach (var s in splitsWhereCreditor)
            {
                allBalances.Add(new DashboardBalanceItemDto
                {
                    SplitId = s.Id,
                    ExpenseId = s.ExpenseId,
                    ExpenseDescription = s.Expense?.Description ?? string.Empty,
                    TripId = s.Expense?.TripId ?? 0,
                    TripTitle = s.Expense?.Trip?.Title ?? string.Empty,
                    AmountOwed = s.AmountOwed,
                    PaidByUserId = user.Id,
                    PaidByUserName = user.Name,
                    DebtorUserId = s.UserId,
                    DebtorUserName = s.User?.Name ?? string.Empty,
                    IsPaid = s.IsPaid,
                    Status = s.IsPaid ? "Settled" : "Pending",
                    CreatedAt = s.Expense?.CreatedAt ?? DateTime.UtcNow,
                    SettledAt = s.SettledAt
                });
            }

            var totalOwed = whatYouOwe.Where(w => !w.IsPaid).Sum(w => w.AmountOwed);
            var totalReceivable = whatOthersOweYou.Where(w => !w.IsPaid).Sum(w => w.AmountOwed);

            var dashboard = new UserDashboardDto
            {
                UserId = user.Id,
                UserName = user.Name,
                TotalOwed = totalOwed,
                TotalReceivable = totalReceivable,
                NetBalance = totalReceivable - totalOwed,
                WhatYouOwe = whatYouOwe,
                WhatOthersOweYou = whatOthersOweYou,
                AllBalances = allBalances.OrderByDescending(b => b.CreatedAt).ToList()
            };

            return Ok(dashboard);
        }

        [HttpGet("{id}/balances")]
        public async Task<ActionResult<UserDashboardDto>> GetUserBalances(int id)
        {
            return await GetUserDashboard(id);
        }
    }
}
