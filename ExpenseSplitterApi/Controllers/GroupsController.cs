using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseSplitterApi.Data;
using ExpenseSplitterApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ExpenseSplitterApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class GroupsController : ControllerBase
    {
        public class AddMemberDto
        {
            public int UserId{ get; set; }
        }
        private readonly AppDbContext _context;

        public GroupsController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<Group>>> GetGroups()
        {
            var userId = GetUserId();

            var groups = await _context.Groups
                .Include(g => g.Members)
                .Where(g => g.Members.Any(m => m.Id == userId))
                .ToListAsync();

            return Ok(groups);
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<Group>> GetGroup(int id)
        {
            var group = await _context.Groups
            .Include(g => g.Members)
            .Include(g => g.CreatedBy)
            .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound("Group not found");
            var userId = GetUserId();
            if (!group.Members.Any(m => m.Id == userId))
                return Forbid();

            return Ok(group);
        }

        [HttpPost]
        public async Task<ActionResult<Group>> CreateGroup(Group group)
        {
            group.CreatedByUserId = GetUserId();
            var creator = await _context.Users.FindAsync(group.CreatedByUserId);
            if (creator == null)
        {
            return BadRequest("CreatedByUserId does not match any existing user.");
        }

        group.Members = new List<User> { creator };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetGroups), new { id = group.Id }, group);
        }


        [HttpPost("{groupId}/members")]
        public async Task<ActionResult<Group>> AddMember(int groupId, [FromBody] AddMemberDto dto)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return NotFound("Group not found");

            var userId = GetUserId();                                  // ADD THIS
            if (!group.Members.Any(m => m.Id == userId))                // ADD THIS
            return Forbid();  
            
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound("User not found");

            if (group.Members.Any(m => m.Id == user.Id))
                return BadRequest("User is already a member of this group");

            group.Members.Add(user);
            await _context.SaveChangesAsync();

            return Ok(group);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(int id, [FromQuery] bool confirm = false)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound("Group not found");

            var userId = GetUserId();

            // Only the creator can delete the group
            if (group.CreatedByUserId != userId)
                return Forbid();

            // Check for unsettled balances
            var expenses = await _context.Expenses
                .Where(e => e.GroupId == id)
                .Include(e => e.Splits)
                .ToListAsync();

            var settlements = await _context.Settlements
                .Where(s => s.GroupId == id)
                .ToListAsync();

            if (expenses.Any() && !confirm)
            {
                var totalExpenses = expenses.Sum(e => e.Amount);
                var totalSettled = settlements.Sum(s => s.Amount);
                var totalOwed = expenses.SelectMany(e => e.Splits).Sum(s => s.AmountOwed);

                bool isFullySettled = totalSettled >= totalOwed;

                if (!isFullySettled)
                {
                    return BadRequest(new
                    {
                        warning = "This group has unpaid expenses. Deleting it will permanently remove all expense and settlement records for this group.",
                        totalExpenses,
                        totalOwed,
                        totalSettled,
                        message = "To proceed anyway, resend this request with ?confirm=true"
                    });
                }
            }

            // Clean up dependent records first (Restrict prevents cascade delete)
            var expenseIds = expenses.Select(e => e.Id).ToList();
            var splits = await _context.ExpenseSplits.Where(s => expenseIds.Contains(s.ExpenseId)).ToListAsync();

            _context.ExpenseSplits.RemoveRange(splits);
            _context.Expenses.RemoveRange(expenses);
            _context.Settlements.RemoveRange(settlements);
            _context.Groups.Remove(group);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{groupId}/leave")]
public async Task<IActionResult> LeaveGroup(int groupId, [FromQuery] bool confirm = false, [FromQuery] int? transferOwnershipTo = null)
{
    var group = await _context.Groups
        .Include(g => g.Members)
        .FirstOrDefaultAsync(g => g.Id == groupId);

    if (group == null) return NotFound("Group not found");

    var userId = GetUserId();

    var member = group.Members.FirstOrDefault(m => m.Id == userId);
    if (member == null) 
        return BadRequest("You are not a member of this group.");


    var groupExpenses = await _context.Expenses
        .Where(e => e.GroupId == groupId)
        .Include(e => e.Splits)
        .ToListAsync();

    var groupSettlements = await _context.Settlements
        .Where(s => s.GroupId == groupId)
        .ToListAsync();

    decimal paid = groupExpenses.Where(e => e.PaidByUserId == userId).Sum(e => e.Amount);
    decimal owed = groupExpenses.SelectMany(e => e.Splits).Where(s => s.UserId == userId).Sum(s => s.AmountOwed);
    decimal settledOut = groupSettlements.Where(s => s.FromUserId == userId).Sum(s => s.Amount);
    decimal settledIn = groupSettlements.Where(s => s.ToUserId == userId).Sum(s => s.Amount);

    decimal netBalance = paid - owed - settledOut + settledIn;

    if (netBalance != 0)
    {
        return BadRequest(new
        {
            warning = "You have an unsettled balance in this group. You must settle up before leaving.",
            netBalance,
            message = netBalance > 0
                ? $"You are owed ₹{netBalance}. Settle up before leaving."
                : $"You owe ₹{Math.Abs(netBalance)}. Settle up before leaving."
        });
    }

    bool isCreator = group.CreatedByUserId == userId;

    if (!isCreator)
    {
        // Regular member leaving — existing simple flow
        if (!confirm)
        {
            return Ok(new
            {
                warning = $"Are you sure you want to leave '{group.Name}'? You will lose access to this group's expenses and history.",
                message = "To proceed, resend this request with ?confirm=true"
            });
        }

        group.Members.Remove(member);
        await _context.SaveChangesAsync();
        return Ok(new { message = "You have left the group." });
    }

    // Creator is leaving — figure out if other real (non-placeholder) members exist
    var otherRealMembers = group.Members
        .Where(m => m.Id != userId && !m.IsDeletedPlaceholder)
        .ToList();

    if (otherRealMembers.Any())
    {
        // Must transfer ownership to a real member
        if (transferOwnershipTo == null)
        {
            return BadRequest(new
            {
                warning = "You are the group creator. You must transfer ownership before leaving.",
                eligibleMembers = otherRealMembers.Select(m => new { m.Id, m.Name }),
                message = "Resend this request with ?transferOwnershipTo={userId}&confirm=true"
            });
        }

        var newOwner = otherRealMembers.FirstOrDefault(m => m.Id == transferOwnershipTo);
        if (newOwner == null)
            return BadRequest("The specified transferOwnershipTo user is not a valid, real member of this group.");

        if (!confirm)
        {
            return Ok(new
            {
                warning = $"Transfer ownership of '{group.Name}' to {newOwner.Name} and leave the group?",
                message = "To proceed, resend this request with ?confirm=true"
            });
        }

        group.CreatedByUserId = newOwner.Id;
        group.Members.Remove(member);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Ownership transferred to {newOwner.Name}. You have left the group." });
    }
    else
    {
        // Creator is the only real member left — delete the whole group
        var expenses = await _context.Expenses
            .Where(e => e.GroupId == groupId)
            .Include(e => e.Splits)
            .ToListAsync();

        var settlements = await _context.Settlements
            .Where(s => s.GroupId == groupId)
            .ToListAsync();

        if (!confirm)
        {
            return Ok(new
            {
                warning = $"You are the last real member of '{group.Name}'. Leaving will permanently delete this group and all its records.",
                message = "To proceed, resend this request with ?confirm=true"
            });
        }

        var expenseIds = expenses.Select(e => e.Id).ToList();
        var splits = await _context.ExpenseSplits.Where(s => expenseIds.Contains(s.ExpenseId)).ToListAsync();

        _context.ExpenseSplits.RemoveRange(splits);
        _context.Expenses.RemoveRange(expenses);
        _context.Settlements.RemoveRange(settlements);
        _context.Groups.Remove(group);

        await _context.SaveChangesAsync();

        return Ok(new { message = "You were the last member. The group and all its records have been deleted." });
    }
}
}
}
       