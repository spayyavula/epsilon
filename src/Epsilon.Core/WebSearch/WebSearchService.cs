using Epsilon.Core.Database;

namespace Epsilon.Core.WebSearch;

public class WebSearchService
{
    private readonly ExaClient _exa;
    private readonly DatabaseService _db;

    public WebSearchService(ExaClient exa, DatabaseService db)
    {
        _exa = exa;
        _db = db;
    }

    public async Task<List<WebSearchResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        var apiKey = _db.GetSetting("exa_api_key");
        if (string.IsNullOrEmpty(apiKey)) return new();

        try
        {
            return await _exa.SearchAsync(query, apiKey, numResults: 5, ct);
        }
        catch
        {
            return new();
        }
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(_db.GetSetting("exa_api_key"));
    }

    public string BuildContext(List<WebSearchResult> results)
    {
        if (results.Count == 0) return "";

        var sections = results.Select(r =>
        {
            var header = $"--- Web Source: {r.Title} ({r.Url}) ---";
            if (r.Author != null) header += $"\nAuthor: {r.Author}";
            if (r.PublishedDate != null) header += $"\nPublished: {r.PublishedDate:yyyy-MM-dd}";
            return $"{header}\n{r.Content}";
        });

        return "\n\n[WEB SEARCH RESULTS]\n" +
               "The following are relevant web sources found via search. " +
               "Use them to supplement your answer and cite URLs when referencing.\n\n" +
               string.Join("\n\n", sections) +
               "\n[END WEB SEARCH RESULTS]";
    }
}
