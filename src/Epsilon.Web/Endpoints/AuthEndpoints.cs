using Epsilon.Web.Auth;
using Epsilon.Web.Contracts;
using Epsilon.Web.Data;
using Epsilon.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Epsilon.Web.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest req, AppDbContext db, JwtService jwt) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "Email and password are required" });

            if (req.Password.Length < 8)
                return Results.BadRequest(new { error = "Password must be at least 8 characters" });

            var exists = await db.Users.AnyAsync(u => u.Email == req.Email.ToLower().Trim());
            if (exists)
                return Results.Conflict(new { error = "Email already registered" });

            var user = new User
            {
                Email = req.Email.ToLower().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                DisplayName = req.DisplayName,
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var accessToken = jwt.GenerateAccessToken(user.Id, user.Email);
            var expiresAt = DateTime.UtcNow.AddMinutes(60);

            return Results.Ok(new TokenResponse(accessToken, jwt.GenerateRefreshToken(), expiresAt));
        });

        group.MapPost("/login", async (LoginRequest req, AppDbContext db, JwtService jwt) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLower().Trim());
            if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return Results.Unauthorized();

            var accessToken = jwt.GenerateAccessToken(user.Id, user.Email);
            var expiresAt = DateTime.UtcNow.AddMinutes(60);

            return Results.Ok(new TokenResponse(accessToken, jwt.GenerateRefreshToken(), expiresAt));
        });
    }
}
