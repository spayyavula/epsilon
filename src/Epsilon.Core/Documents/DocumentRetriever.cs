using Epsilon.Core.Database;
using Epsilon.Core.Models;

namespace Epsilon.Core.Documents;

public class DocumentRetriever
{
    private readonly DatabaseService _db;

    public DocumentRetriever(DatabaseService db)
    {
        _db = db;
    }

    public List<RetrievedChunk> Search(string query, int topK = 5)
    {
        if (string.IsNullOrWhiteSpace(query)) return new();

        // Sanitize query for FTS5
        var sanitized = SanitizeFtsQuery(query);
        if (string.IsNullOrWhiteSpace(sanitized)) return new();

        return _db.SearchChunks(sanitized, topK);
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

    private static string SanitizeFtsQuery(string query)
    {
        // Remove FTS5 special characters
        var cleaned = query
            .Replace("\"", " ")
            .Replace("*", " ")
            .Replace("(", " ")
            .Replace(")", " ")
            .Replace("-", " ")
            .Replace("^", " ")
            .Replace(":", " ")
            .Replace(".", " ");

        // Split into words, filter short ones, join with OR for broader matching
        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length >= 2)
            .Take(10)
            .ToList();

        if (words.Count == 0) return "";

        return string.Join(" OR ", words);
    }
}
