using Epsilon.Web.Auth;
using Epsilon.Web.Contracts;
using Epsilon.Web.Data;
using Epsilon.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Epsilon.Web.Endpoints;

public static class FlashcardEndpoints
{
    public static void MapFlashcardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/flashcards").RequireAuthorization().WithTags("Flashcards");

        group.MapGet("/", async (UserContext user, AppDbContext db) =>
        {
            var cards = await db.Flashcards
                .Where(f => f.UserId == user.UserId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
            return cards.Select(MapFlashcard);
        });

        group.MapGet("/due", async (UserContext user, AppDbContext db) =>
        {
            var now = DateTime.UtcNow;
            var cards = await db.Flashcards
                .Where(f => f.UserId == user.UserId && f.NextReview <= now)
                .OrderBy(f => f.NextReview)
                .ToListAsync();
            return cards.Select(MapFlashcard);
        });

        group.MapPost("/", async (CreateFlashcardRequest req, UserContext user, AppDbContext db) =>
        {
            var card = new FlashcardEntity
            {
                UserId = user.UserId,
                Front = req.Front,
                Back = req.Back,
                Category = req.Category,
            };
            db.Flashcards.Add(card);
            await db.SaveChangesAsync();
            return Results.Created($"/api/flashcards/{card.Id}", MapFlashcard(card));
        });

        group.MapDelete("/{id:guid}", async (Guid id, UserContext user, AppDbContext db) =>
        {
            var card = await db.Flashcards
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == user.UserId);
            if (card == null) return Results.NotFound();
            db.Flashcards.Remove(card);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/review", async (Guid id, ReviewFlashcardRequest req,
            UserContext user, AppDbContext db) =>
        {
            var card = await db.Flashcards
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == user.UserId);
            if (card == null) return Results.NotFound();

            // SM-2 algorithm
            var quality = Math.Clamp(req.Quality, 0, 5);

            if (quality >= 3)
            {
                if (card.Repetitions == 0)
                    card.IntervalDays = 1;
                else if (card.Repetitions == 1)
                    card.IntervalDays = 6;
                else
                    card.IntervalDays = (int)Math.Round(card.IntervalDays * card.EaseFactor);

                card.Repetitions++;
            }
            else
            {
                card.Repetitions = 0;
                card.IntervalDays = 1;
            }

            card.EaseFactor = Math.Max(1.3,
                card.EaseFactor + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02)));

            card.NextReview = DateTime.UtcNow.AddDays(card.IntervalDays);
            await db.SaveChangesAsync();

            return Results.Ok(MapFlashcard(card));
        });
    }

    private static FlashcardDto MapFlashcard(FlashcardEntity f) => new(
        f.Id.ToString(), f.Front, f.Back, f.Category,
        f.EaseFactor, f.IntervalDays, f.Repetitions,
        f.NextReview, f.CreatedAt);
}
