using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseSplitterApi.Data;
using ExpenseSplitterApi.Models;
using Microsoft.AspNetCore.Authorization;   // NEW
using System.Security.Claims;                // NEW

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

        private int GetUserId()   // NEW
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user);
        }

        // NEW: DeleteUser with placeholder reassignment
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var currentUserId = GetUserId();

            // Only allow deleting your own account
            if (id != currentUserId)
                return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found");

            // Can't delete the placeholder itself
            if (user.IsDeletedPlaceholder)
                return BadRequest("Cannot delete the placeholder user.");

            // Always create a NEW placeholder, unique to this user
            var placeholder = new User
            {
                Name = $"#{user.Name}",
                Email = $"deleted-user-{Guid.NewGuid()}@placeholder.local",
                PasswordHash = "N/A",
                IsDeletedPlaceholder = true
            };
            _context.Users.Add(placeholder);
            await _context.SaveChangesAsync();

            // Reassign expenses paid by this user
            var paidExpenses = await _context.Expenses
                .Where(e => e.PaidByUserId == id)
                .ToListAsync();
            foreach (var e in paidExpenses)
                e.PaidByUserId = placeholder.Id;

            // Reassign expense splits owed by this user
            var splits = await _context.ExpenseSplits
                .Where(s => s.UserId == id)
                .ToListAsync();
            foreach (var s in splits)
                s.UserId = placeholder.Id;

            // Reassign settlements (both directions)
            var fromSettlements = await _context.Settlements
                .Where(s => s.FromUserId == id)
                .ToListAsync();
            foreach (var s in fromSettlements)
                s.FromUserId = placeholder.Id;

            var toSettlements = await _context.Settlements
                .Where(s => s.ToUserId == id)
                .ToListAsync();
            foreach (var s in toSettlements)
                s.ToUserId = placeholder.Id;

            // NEW: Reassign groups created by this user
            var createdGroups = await _context.Groups
                .Where(g => g.CreatedByUserId == id)
                .ToListAsync();
            foreach (var g in createdGroups)
                g.CreatedByUserId = placeholder.Id;

            // Remove user from all group memberships
            var groups = await _context.Groups
                .Include(g => g.Members)
                .Where(g => g.Members.Any(m => m.Id == id))
                .ToListAsync();
            foreach (var g in groups)
                g.Members.Remove(g.Members.First(m => m.Id == id));

            // Finally, delete the actual user
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            return Ok(new { message = "User deleted. Balances reassigned to placeholder.", placeholderId = placeholder.Id });
        }
    }
}