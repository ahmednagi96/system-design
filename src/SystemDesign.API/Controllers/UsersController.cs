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

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    public record CreateUserRequest(string Name, string Email, string Password, string? Role, string? Status);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.Role,
                u.Status,
                u.CreatedAt,
                u.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Name, email, and password are required.");
        }

        var email = request.Email.Trim();
        var exists = await _db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (exists)
            return Conflict("A user with this email already exists.");

        var now = DateTime.UtcNow.ToString("O");
        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            Password = request.Password,
            Role = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return Created($"/api/users/{user.Id}", new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.Status,
            user.CreatedAt,
            user.UpdatedAt
        });
    }
}
