using Epsilon.Web.Auth;
using Epsilon.Web.Contracts;
using Epsilon.Web.Services;

namespace Epsilon.Web.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/documents").RequireAuthorization().WithTags("Documents");

        group.MapGet("/", async (UserContext user, DocumentStorageService storage) =>
        {
            var docs = await storage.ListAsync(user.UserId);
            return docs.Select(d => new DocumentDto(
                d.Id.ToString(), d.FileName, d.MimeType, d.SizeBytes,
                d.PageCount, d.ChunkCount, d.Status, d.IngestedAt));
        });

        group.MapPost("/upload", async (IFormFile file, UserContext user, DocumentStorageService storage) =>
        {
            if (file.Length == 0)
                return Results.BadRequest(new { error = "Empty file" });
            if (file.Length > 50 * 1024 * 1024)
                return Results.BadRequest(new { error = "File too large (max 50MB)" });

            var allowedTypes = new[]
            {
                "application/pdf", "text/plain", "text/markdown",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            };
            if (!allowedTypes.Contains(file.ContentType))
                return Results.BadRequest(new { error = "Unsupported file type" });

            using var stream = file.OpenReadStream();
            var doc = await storage.UploadAsync(user.UserId, file.FileName, file.ContentType, stream);

            // Process in background
            _ = Task.Run(() => storage.ProcessDocumentAsync(doc));

            return Results.Accepted($"/api/documents/{doc.Id}",
                new DocumentDto(doc.Id.ToString(), doc.FileName, doc.MimeType, doc.SizeBytes,
                    doc.PageCount, doc.ChunkCount, doc.Status, doc.IngestedAt));
        }).DisableAntiforgery();

        group.MapDelete("/{id:guid}", async (Guid id, UserContext user, DocumentStorageService storage) =>
        {
            return await storage.DeleteAsync(user.UserId, id)
                ? Results.NoContent()
                : Results.NotFound();
        });
    }
}
