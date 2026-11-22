using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using School.Api.Data;
using School.Api.Models;
using School.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<SchoolDb>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("School"))
);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Access>();
builder.Services.AddScoped<Ledger>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder
    .Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "MissionSchool.Session";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.Events.OnRedirectToLogin = c =>
        {
            c.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        o.Events.OnRedirectToAccessDenied = c =>
        {
            c.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
        o.Events.OnValidatePrincipal = async c =>
        {
            var db = c.HttpContext.RequestServices.GetRequiredService<SchoolDb>();
            var id = int.Parse(c.Principal!.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var u = await db.Users.FindAsync(id);
            if (
                u == null
                || !u.Active
                || u.SessionVersion.ToString() != c.Principal!.FindFirstValue("version")
            )
                c.RejectPrincipal();
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");
builder
    .Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.ReferenceHandler = System
            .Text
            .Json
            .Serialization
            .ReferenceHandler
            .IgnoreCycles
    );
var app = builder.Build();
if (args.Contains("--seed") || args.Contains("--migrate"))
{
    using var s = app.Services.CreateScope();
    var db = s.ServiceProvider.GetRequiredService<SchoolDb>();
    await db.Database.MigrateAsync();
    if (args.Contains("--seed"))
        await Seed.Run(db, s.ServiceProvider.GetRequiredService<IPasswordHasher<User>>());
    return;
}
if (args.Contains("--create-admin"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SchoolDb>();
    await db.Database.MigrateAsync();
    if (await db.Users.AnyAsync())
        throw new InvalidOperationException("Bootstrap is only allowed on an empty installation.");
    var email =
        builder.Configuration["Admin:Email"] ?? throw new ArgumentException("Set Admin__Email");
    var password =
        builder.Configuration["Admin:Password"]
        ?? throw new ArgumentException("Set Admin__Password");
    School.Api.Controllers.AuthController.ValidatePassword(password);
    var admin = new User
    {
        Name = "School Administrator",
        Email = email.Trim().ToLowerInvariant(),
        Role = "ADMIN",
    };
    admin.PasswordHash = scope
        .ServiceProvider.GetRequiredService<IPasswordHasher<User>>()
        .HashPassword(admin, password);
    db.Users.Add(admin);
    await db.SaveChangesAsync();
    return;
}
app.Use(
    async (ctx, next) =>
    {
        try
        {
            await next();
        }
        catch (UnauthorizedAccessException e)
        {
            ctx.Response.StatusCode = 403;
            await ctx.Response.WriteAsJsonAsync(new { error = e.Message });
        }
        catch (System.Text.Json.JsonException)
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.WriteAsJsonAsync(
                new { error = "Invalid field format. Check numbers, dates and required fields." }
            );
        }
        catch (ArgumentException e)
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.WriteAsJsonAsync(new { error = e.Message });
        }
        catch (DbUpdateException)
        {
            ctx.Response.StatusCode = 409;
            await ctx.Response.WriteAsJsonAsync(
                new
                {
                    error = "The operation conflicts with an existing record or a related record. Refresh and check the values.",
                }
            );
        }
        catch (Npgsql.PostgresException e) when (e.SqlState == "40001")
        {
            ctx.Response.StatusCode = 409;
            await ctx.Response.WriteAsJsonAsync(
                new
                {
                    error = "Another financial operation occurred at the same time. Refresh and retry.",
                }
            );
        }
    }
);
app.Use(
    async (ctx, next) =>
    {
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Response.Headers["X-Frame-Options"] = "DENY";
        ctx.Response.Headers["Referrer-Policy"] = "same-origin";
        await next();
    }
);
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.Use(
    async (ctx, next) =>
    {
        if (
            ctx.Request.Path.StartsWithSegments("/api")
            && !HttpMethods.IsGet(ctx.Request.Method)
            && !HttpMethods.IsHead(ctx.Request.Method)
        )
        {
            try
            {
                await ctx
                    .RequestServices.GetRequiredService<IAntiforgery>()
                    .ValidateRequestAsync(ctx);
            }
            catch (AntiforgeryValidationException)
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.WriteAsJsonAsync(
                    new { error = "Security token expired. Refresh the page." }
                );
                return;
            }
        }
        await next();
    }
);
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();

public partial class Program { }
