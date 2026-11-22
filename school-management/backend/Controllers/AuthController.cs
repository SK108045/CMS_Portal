using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.Api.Data;
using School.Api.Models;
using School.Api.Services;

namespace School.Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(SchoolDb db, IPasswordHasher<User> hasher, IAntiforgery csrf)
    : ControllerBase
{
    [HttpGet("csrf")]
    public object Token() => new { token = csrf.GetAndStoreTokens(HttpContext).RequestToken };

    public record LoginInput(string Email, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginInput input)
    {
        var email = input.Email.Trim().ToLowerInvariant();
        var u = await db.Users.SingleOrDefaultAsync(x => x.Email == email);
        if (u == null || !u.Active || u.LockedUntil > DateTime.UtcNow)
            return Unauthorized(
                new { error = "Invalid credentials or account temporarily locked." }
            );
        if (
            hasher.VerifyHashedPassword(u, u.PasswordHash, input.Password)
            == PasswordVerificationResult.Failed
        )
        {
            u.FailedLogins++;
            if (u.FailedLogins >= 5)
                u.LockedUntil = DateTime.UtcNow.AddMinutes(15);
            await db.SaveChangesAsync();
            return Unauthorized(new { error = "Invalid credentials." });
        }
        u.FailedLogins = 0;
        u.LockedUntil = null;
        await db.SaveChangesAsync();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new Claim(ClaimTypes.Name, u.Name),
            new Claim(ClaimTypes.Role, u.Role),
            new Claim("department", u.DepartmentId?.ToString() ?? ""),
            new Claim("version", u.SessionVersion.ToString()),
        };
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(
                new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)
            )
        );
        db.Audit.Add(
            new AuditLog
            {
                Actor = u.Name,
                CreatedBy = u.Id,
                Action = "Logged in",
                Entity = "users",
                EntityId = u.Id,
                DepartmentId = u.DepartmentId,
            }
        );
        await db.SaveChangesAsync();
        return Ok(
            new
            {
                u.Id,
                u.Name,
                u.Email,
                u.Role,
                u.DepartmentId,
            }
        );
    }

    [Authorize, HttpGet("me")]
    public async Task<object> Me()
    {
        var u = await db.Users.FindAsync(
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
        );
        return new
        {
            u!.Id,
            u.Name,
            u.Email,
            u.Role,
            u.DepartmentId,
        };
    }

    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Ok();
    }

    public record PasswordInput(string CurrentPassword, string Password);

    [Authorize, HttpPost("password")]
    public async Task<IActionResult> Password(PasswordInput i)
    {
        var u = await db.Users.FindAsync(
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
        );
        if (
            hasher.VerifyHashedPassword(u!, u!.PasswordHash, i.CurrentPassword)
            == PasswordVerificationResult.Failed
        )
            return BadRequest(new { error = "Current password is incorrect." });
        ValidatePassword(i.Password);
        u.PasswordHash = hasher.HashPassword(u, i.Password);
        u.SessionVersion++;
        await db.SaveChangesAsync();
        await HttpContext.SignOutAsync();
        return Ok();
    }

    public static void ValidatePassword(string p)
    {
        if (
            p.Length < 10
            || !p.Any(char.IsUpper)
            || !p.Any(char.IsDigit)
            || !p.Any(c => !char.IsLetterOrDigit(c))
        )
            throw new ArgumentException(
                "Password needs 10 characters, an uppercase letter, a digit and a symbol."
            );
    }
}
