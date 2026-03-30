using Epsilon.Core.Models;
using Epsilon.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Epsilon.Web.Services;

public class WebDocumentRetriever
{
    private readonly AppDbContext _db;

    public WebDocumentRetriever(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<RetrievedChunk>> SearchAsync(Guid userId, string query, int topK = 5)
    {
        if (string.IsNullOrWhiteSpace(query)) return new();

        var tsQuery = SanitizeForTsQuery(query);
        if (string.IsNullOrWhiteSpace(tsQuery)) return new();

        var chunks = await _db.DocumentChunks
            .Where(c => c.UserId == userId &&
                        c.SearchVector!.Matches(EF.Functions.ToTsQuery("english", tsQuery)))
            .OrderByDescending(c => c.SearchVector!.Rank(EF.Functions.ToTsQuery("english", tsQuery)))
            .Take(topK)
            .Join(_db.Documents, c => c.DocumentId, d => d.Id, (c, d) => new RetrievedChunk
            {
                Text = c.Content,
                DocumentId = c.DocumentId.ToString(),
                FileName = d.FileName,
            })
            .ToListAsync();

        return chunks;
    }

    public string BuildContext(List<RetrievedChunk> chunks)
    {
        if (chunks.Count == 0) return "";

        var sections = chunks.Select(c =>
            $"--- Source: {c.FileName} ---\n{c.Text}");

        return "\n\n[DOCUMENT CONTEXT]\n" +
               "The following excerpts are from the user's uploaded documents. " +
               "Use them to ground your answer and cite the source document.\n\n" +
               string.Join("\n\n", sections) +
               "\n[END DOCUMENT CONTEXT]";
    }

    private static string SanitizeForTsQuery(string query)
    {
        var cleaned = query
            .Replace("\"", " ").Replace("*", " ").Replace("(", " ").Replace(")", " ")
            .Replace("-", " ").Replace("^", " ").Replace(":", " ").Replace(".", " ")
            .Replace("'", " ").Replace("&", " ").Replace("|", " ").Replace("!", " ");

        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length >= 2)
            .Take(10)
            .ToList();

        if (words.Count == 0) return "";
        return string.Join(" | ", words);
    }
}
