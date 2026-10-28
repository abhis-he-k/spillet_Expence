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
    public class SettlementsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SettlementsController(AppDbContext context)
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

        // POST: api/settlements
        [HttpPost]
        public async Task<ActionResult<Settlement>> CreateSettlement(CreateSettlementDto dto)
        {
            var fromUser = await _context.Users.FindAsync(dto.FromUserId);
            var toUser = await _context.Users.FindAsync(dto.ToUserId);
            var group = await _context.Groups.FindAsync(dto.GroupId);

            if (fromUser == null) return NotFound("FromUser not found");
            if (toUser == null) return NotFound("ToUser not found");
            if (group == null) return NotFound("Group not found");

            var userId = GetUserId();
            if (!await IsGroupMember(dto.GroupId, userId))
                return Forbid();

            var settlement = new Settlement
            {
                FromUserId = dto.FromUserId,
                ToUserId = dto.ToUserId,
                GroupId = dto.GroupId,
                Amount = dto.Amount,
                SettledAt = DateTime.UtcNow
            };

            _context.Settlements.Add(settlement);
            await _context.SaveChangesAsync();

            return Ok(settlement);
        }

        // GET: api/settlements/group/5
        [HttpGet("group/{groupId}")]
        public async Task<ActionResult<IEnumerable<Settlement>>> GetSettlementsForGroup(int groupId)
        {

            var userId = GetUserId();
            if (!await IsGroupMember(groupId, userId))
                return Forbid();
                
            var settlements = await _context.Settlements
                .Where(s => s.GroupId == groupId)
                .ToListAsync();

            return Ok(settlements);
        }
    }

public class CreateSettlementDto
{
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public int GroupId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }
}
}