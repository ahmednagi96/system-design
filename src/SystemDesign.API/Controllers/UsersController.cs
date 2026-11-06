using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemDesign.Domain.Entities;
using SystemDesign.Infrastructure.Data;

namespace SystemDesign.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    public record CreateUserRequest(
        [Required, StringLength(100, MinimumLength = 2)] string Name,
        [Required, EmailAddress, StringLength(256)] string Email,
        [Required, StringLength(100, MinimumLength = 6)] string Password,
        string? Role,
        string? Status);

    public record UserResponse(
        int Id,
        string Name,
        string Email,
        string Role,
        string Status,
        string CreatedAt,
        string UpdatedAt);

    // GET api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new UserResponse(u.Id, u.Name, u.Email, u.Role, u.Status, u.CreatedAt, u.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    // GET api/users/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserResponse(u.Id, u.Name, u.Email, u.Role, u.Status, u.CreatedAt, u.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return user is null ? NotFound() : Ok(user);
    }

    // POST api/users
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Name, email, and password are required.");
        }

        // نخزن الإيميل lowercase عشان "Ahmed@x.com" و "ahmed@x.com" يتعاملوا كإيميل واحد
        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await _db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (exists)
            return Conflict("A user with this email already exists.");

        var now = DateTime.UtcNow.ToString("O");
        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            Role = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        // عمرنا ما نخزن الباسورد كـ plain text — بنخزن hash بس
        user.Password = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var response = new UserResponse(user.Id, user.Name, user.Email, user.Role, user.Status, user.CreatedAt, user.UpdatedAt);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, response);
    }
}
