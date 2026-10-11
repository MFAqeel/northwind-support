using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Services;
namespace SupportDesk.Presentation.Controllers;
public sealed record LoginRequest(string Email, string Password);
public sealed record AgentSession(int Id, string FullName, string Email);
public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, AgentSession Agent);
[ApiController]
[Route("api/auth")]
public sealed class AuthController(SupportDbContext db, PasswordService passwords, IConfiguration config) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email?.Trim();
        var agent = string.IsNullOrWhiteSpace(email) ? null : await db.Set<SupportDesk.Domain.Aggregates.Agents.Agent>()
            .AsNoTracking().FirstOrDefaultAsync(a => a.Email == email, ct);
        if (agent is null || !agent.IsActive || !passwords.Verify(request.Password, agent.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });
        var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");
        var issuer = config["Jwt:Issuer"] ?? "NorthwindSupport";
        var audience = config["Jwt:Audience"] ?? "NorthwindSupportWeb";
        var expiry = DateTime.UtcNow.AddHours(2);
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, agent.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, agent.Id.ToString()),
            new Claim(ClaimTypes.Name, agent.FullName), new Claim(JwtRegisteredClaimNames.Email, agent.Email) };
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expiry,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expiry,
            new AgentSession(agent.Id, agent.FullName, agent.Email)));
    }
    [HttpGet("me")]
    public async Task<ActionResult<AgentSession>> Me(CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Unauthorized();
        var agent = await db.Set<SupportDesk.Domain.Aggregates.Agents.Agent>().AsNoTracking()
            .Where(a => a.Id == id && a.IsActive)
            .Select(a => new AgentSession(a.Id, a.FullName, a.Email)).FirstOrDefaultAsync(ct);
        return agent is null ? Unauthorized() : Ok(agent);
    }
}
