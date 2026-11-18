using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using ExpenseSplitterApi.Controllers;
using ExpenseSplitterApi.Data;
using ExpenseSplitterApi.DTOs;
using ExpenseSplitterApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=ExpenseSplitter.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase) || 
        (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) && !connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong123!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ExpenseSplitterApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ExpenseSplitterApiUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

namespace ExpenseSplitterApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<TripMember> TripMembers { get; set; } = new();
        public List<Expense> ExpensesPaid { get; set; } = new();
        public List<ExpenseSplit> ExpenseSplits { get; set; } = new();
    }

    public class Trip
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        [NotMapped]
        public string Name
        {
            get => Title;
            set => Title = value;
        }

        public List<TripMember> Members { get; set; } = new();
        public List<Expense> Expenses { get; set; } = new();
    }

    public class TripMember
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        public Trip? Trip { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    public class Expense
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int TripId { get; set; }
        public Trip? Trip { get; set; }

        public int PaidByUserId { get; set; }
        public User? PaidByUser { get; set; }

        public List<ExpenseSplit> Splits { get; set; } = new();
    }

    public class ExpenseSplit
    {
        public int Id { get; set; }
        public int ExpenseId { get; set; }
        public Expense? Expense { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public decimal AmountOwed { get; set; }
        public bool IsPaid { get; set; } = false;
        public DateTime? SettledAt { get; set; }

        [NotMapped]
        public decimal Amount
        {
            get => AmountOwed;
            set => AmountOwed = value;
        }
    }
}

namespace ExpenseSplitterApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<TripMember> TripMembers { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<ExpenseSplit> ExpenseSplits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<Trip>(entity =>
            {
                entity.HasOne(t => t.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(t => t.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TripMember>(entity =>
            {
                entity.HasKey(tm => tm.Id);

                entity.HasIndex(tm => new { tm.TripId, tm.UserId }).IsUnique();

                entity.HasOne(tm => tm.Trip)
                    .WithMany(t => t.Members)
                    .HasForeignKey(tm => tm.TripId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tm => tm.User)
                    .WithMany(u => u.TripMembers)
                    .HasForeignKey(tm => tm.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Expense>(entity =>
            {
                entity.Property(e => e.Amount)
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Trip)
                    .WithMany(t => t.Expenses)
                    .HasForeignKey(e => e.TripId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.PaidByUser)
                    .WithMany(u => u.ExpensesPaid)
                    .HasForeignKey(e => e.PaidByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ExpenseSplit>(entity =>
            {
                entity.Property(es => es.AmountOwed)
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(es => es.Expense)
                    .WithMany(e => e.Splits)
                    .HasForeignKey(es => es.ExpenseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(es => es.User)
                    .WithMany(u => u.ExpenseSplits)
                    .HasForeignKey(es => es.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}

namespace ExpenseSplitterApi.DTOs
{
    public class RegisterDto
    {
        [Required]
        [MinLength(2, ErrorMessage = "Name must be at least 2 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserResponseDto User { get; set; } = new();
    }

    public class CreateTripDto
    {
        [Required]
        [MinLength(2, ErrorMessage = "Trip title must be at least 2 characters.")]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int? CreatedByUserId { get; set; }

        public List<int> InitialMemberUserIds { get; set; } = new();
    }

    public class AddMemberDto
    {
        [Required]
        public int UserId { get; set; }
    }

    public class TripMemberResponseDto
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }

    public class ExpenseSummaryDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int PaidByUserId { get; set; }
        public string PaidByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<ExpenseSplitResponseDto> Splits { get; set; } = new();
    }

    public class TripResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public List<TripMemberResponseDto> Members { get; set; } = new();
        public List<ExpenseSummaryDto> Expenses { get; set; } = new();
    }

    public class CreateExpenseDto
    {
        [Required]
        [MinLength(2, ErrorMessage = "Description must be at least 2 characters.")]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        public int TripId { get; set; }

        public int? PaidByUserId { get; set; }

        [MinLength(1, ErrorMessage = "At least one participant is required.")]
        public List<int> ParticipantUserIds { get; set; } = new();
    }

    public class ExpenseSplitResponseDto
    {
        public int Id { get; set; }
        public int ExpenseId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public decimal AmountOwed { get; set; }
        public bool IsPaid { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? SettledAt { get; set; }
    }

    public class ExpenseResponseDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TripId { get; set; }
        public string TripTitle { get; set; } = string.Empty;
        public int PaidByUserId { get; set; }
        public string PaidByUserName { get; set; } = string.Empty;
        public List<ExpenseSplitResponseDto> Splits { get; set; } = new();
    }

    public class UpdateSplitStatusDto
    {
        public bool IsPaid { get; set; } = true;
    }

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

    public class UserOwedExpenseDto
    {
        public int SplitId { get; set; }
        public int ExpenseId { get; set; }
        public string ExpenseDescription { get; set; } = string.Empty;
        public int TripId { get; set; }
        public string TripTitle { get; set; } = string.Empty;
        public decimal AmountOwed { get; set; }
        public int PaidByUserId { get; set; }
        public string PaidByUserName { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? SettledAt { get; set; }
    }

    public class UserLentExpenseDto
    {
        public int SplitId { get; set; }
        public int ExpenseId { get; set; }
        public string ExpenseDescription { get; set; } = string.Empty;
        public int TripId { get; set; }
        public string TripTitle { get; set; } = string.Empty;
        public decimal AmountOwed { get; set; }
        public int DebtorUserId { get; set; }
        public string DebtorUserName { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? SettledAt { get; set; }
    }

    public class DashboardBalanceItemDto
    {
        public int SplitId { get; set; }
        public int ExpenseId { get; set; }
        public string ExpenseDescription { get; set; } = string.Empty;
        public int TripId { get; set; }
        public string TripTitle { get; set; } = string.Empty;
        public decimal AmountOwed { get; set; }
        public int PaidByUserId { get; set; }
        public string PaidByUserName { get; set; } = string.Empty;
        public int DebtorUserId { get; set; }
        public string DebtorUserName { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? SettledAt { get; set; }
    }
}

namespace ExpenseSplitterApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
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

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                User = new UserResponseDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                }
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            var emailLower = dto.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid email or password.");
            }

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                User = new UserResponseDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                }
            });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiryMinutes = 600.0;
            if (double.TryParse(_config["Jwt:ExpiryMinutes"], out var parsedExpiry))
            {
                expiryMinutes = parsedExpiry;
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

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

    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<UserDashboardDto>> GetDashboard(int userId)
        {
            var usersController = new UsersController(_context);
            return await usersController.GetUserDashboard(userId);
        }
    }
}
