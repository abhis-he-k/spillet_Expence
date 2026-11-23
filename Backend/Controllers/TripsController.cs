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
    public class TripsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TripsController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : null;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TripResponseDto>>> GetTrips([FromQuery] int? userId)
        {
            var targetUserId = userId ?? GetCurrentUserId();

            var query = _context.Trips
                .Include(t => t.CreatedByUser)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .Include(t => t.Expenses)
                    .ThenInclude(e => e.PaidByUser)
                .AsQueryable();

            if (targetUserId.HasValue)
            {
                query = query.Where(t => t.Members.Any(m => m.UserId == targetUserId.Value) || t.CreatedByUserId == targetUserId.Value);
            }

            var trips = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TripResponseDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    CreatedByUserId = t.CreatedByUserId,
                    CreatedByUserName = t.CreatedByUser != null ? t.CreatedByUser.Name : string.Empty,
                    Members = t.Members.Select(m => new TripMemberResponseDto
                    {
                        Id = m.Id,
                        TripId = m.TripId,
                        UserId = m.UserId,
                        UserName = m.User != null ? m.User.Name : string.Empty,
                        Email = m.User != null ? m.User.Email : string.Empty,
                        JoinedAt = m.JoinedAt
                    }).ToList(),
                    Expenses = t.Expenses.Select(e => new ExpenseSummaryDto
                    {
                        Id = e.Id,
                        Description = e.Description,
                        Amount = e.Amount,
                        PaidByUserId = e.PaidByUserId,
                        PaidByUserName = e.PaidByUser != null ? e.PaidByUser.Name : string.Empty,
                        CreatedAt = e.CreatedAt
                    }).ToList()
                })
                .ToListAsync();

            return Ok(trips);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TripResponseDto>> GetTrip(int id)
        {
            var trip = await _context.Trips
                .Include(t => t.CreatedByUser)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .Include(t => t.Expenses)
                    .ThenInclude(e => e.PaidByUser)
                .Include(t => t.Expenses)
                    .ThenInclude(e => e.Splits)
                        .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trip == null)
            {
                return NotFound("Trip not found.");
            }

            var response = new TripResponseDto
            {
                Id = trip.Id,
                Title = trip.Title,
                Description = trip.Description,
                CreatedAt = trip.CreatedAt,
                CreatedByUserId = trip.CreatedByUserId,
                CreatedByUserName = trip.CreatedByUser != null ? trip.CreatedByUser.Name : string.Empty,
                Members = trip.Members.Select(m => new TripMemberResponseDto
                {
                    Id = m.Id,
                    TripId = m.TripId,
                    UserId = m.UserId,
                    UserName = m.User != null ? m.User.Name : string.Empty,
                    Email = m.User != null ? m.User.Email : string.Empty,
                    JoinedAt = m.JoinedAt
                }).ToList(),
                Expenses = trip.Expenses.Select(e => new ExpenseSummaryDto
                {
                    Id = e.Id,
                    Description = e.Description,
                    Amount = e.Amount,
                    PaidByUserId = e.PaidByUserId,
                    PaidByUserName = e.PaidByUser != null ? e.PaidByUser.Name : string.Empty,
                    CreatedAt = e.CreatedAt,
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
                }).ToList()
            };

            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<TripResponseDto>> CreateTrip(CreateTripDto dto)
        {
            var creatorId = dto.CreatedByUserId ?? GetCurrentUserId();

            if (!creatorId.HasValue)
            {
                return BadRequest("CreatedByUserId is required.");
            }

            var creator = await _context.Users.FindAsync(creatorId.Value);
            if (creator == null)
            {
                return BadRequest("Creator user not found.");
            }

            var trip = new Trip
            {
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                CreatedByUserId = creatorId.Value,
                CreatedAt = DateTime.UtcNow
            };

            trip.Members.Add(new TripMember
            {
                UserId = creatorId.Value,
                JoinedAt = DateTime.UtcNow
            });

            if (dto.InitialMemberUserIds != null && dto.InitialMemberUserIds.Count > 0)
            {
                var distinctMemberIds = dto.InitialMemberUserIds
                    .Where(id => id != creatorId.Value)
                    .Distinct()
                    .ToList();

                foreach (var memberId in distinctMemberIds)
                {
                    var userExists = await _context.Users.AnyAsync(u => u.Id == memberId);
                    if (userExists)
                    {
                        trip.Members.Add(new TripMember
                        {
                            UserId = memberId,
                            JoinedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();

            return await GetTrip(trip.Id);
        }

        [HttpPost("{id}/members")]
        public async Task<ActionResult<TripMemberResponseDto>> AddMember(int id, AddMemberDto dto)
        {
            var trip = await _context.Trips
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trip == null)
            {
                return NotFound("Trip not found.");
            }

            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (trip.Members.Any(m => m.UserId == dto.UserId))
            {
                return BadRequest("User is already a member of this trip.");
            }

            var member = new TripMember
            {
                TripId = id,
                UserId = dto.UserId,
                JoinedAt = DateTime.UtcNow
            };

            _context.TripMembers.Add(member);
            await _context.SaveChangesAsync();

            var response = new TripMemberResponseDto
            {
                Id = member.Id,
                TripId = member.TripId,
                UserId = member.UserId,
                UserName = user.Name,
                Email = user.Email,
                JoinedAt = member.JoinedAt
            };

            return Ok(response);
        }

        [HttpGet("{id}/members")]
        public async Task<ActionResult<IEnumerable<TripMemberResponseDto>>> GetMembers(int id)
        {
            var tripExists = await _context.Trips.AnyAsync(t => t.Id == id);
            if (!tripExists)
            {
                return NotFound("Trip not found.");
            }

            var members = await _context.TripMembers
                .Where(m => m.TripId == id)
                .Include(m => m.User)
                .Select(m => new TripMemberResponseDto
                {
                    Id = m.Id,
                    TripId = m.TripId,
                    UserId = m.UserId,
                    UserName = m.User != null ? m.User.Name : string.Empty,
                    Email = m.User != null ? m.User.Email : string.Empty,
                    JoinedAt = m.JoinedAt
                })
                .ToListAsync();

            return Ok(members);
        }

        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(int id, int userId)
        {
            var member = await _context.TripMembers.FirstOrDefaultAsync(m => m.TripId == id && m.UserId == userId);
            if (member == null)
            {
                return NotFound("Member not found in trip.");
            }

            _context.TripMembers.Remove(member);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
