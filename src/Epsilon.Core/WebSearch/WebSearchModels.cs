namespace Epsilon.Core.WebSearch;

public class ExaSearchRequest
{
    public string Query { get; set; } = "";
    public int NumResults { get; set; } = 5;
    public string Type { get; set; } = "auto";
    public ExaContentsOptions? Contents { get; set; }
}

public class ExaContentsOptions
{
    public ExaTextOptions? Text { get; set; }
}

public class ExaTextOptions
{
    public int MaxCharacters { get; set; } = 3000;
}

public class ExaSearchResponse
{
    public List<ExaResult> Results { get; set; } = new();
}

public class ExaResult
{
    public string Id { get; set; } = "";
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Text { get; set; }
    public string? Author { get; set; }
    public DateTime? PublishedDate { get; set; }
    public float? Score { get; set; }
}

public class WebSearchResult
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string Content { get; set; } = "";
    public string? Author { get; set; }
    public DateTime? PublishedDate { get; set; }
}
