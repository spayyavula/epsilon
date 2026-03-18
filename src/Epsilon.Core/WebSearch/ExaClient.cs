using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Epsilon.Core.WebSearch;

public class ExaClient
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://api.exa.ai";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public ExaClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<WebSearchResult>> SearchAsync(string query, string apiKey, int numResults = 5, CancellationToken ct = default)
    {
        var request = new ExaSearchRequest
        {
            Query = query,
            NumResults = numResults,
            Type = "auto",
            Contents = new ExaContentsOptions
            {
                Text = new ExaTextOptions { MaxCharacters = 3000 },
            },
        };

        using var httpReq = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/search");
        httpReq.Headers.Add("x-api-key", apiKey);
        httpReq.Content = JsonContent.Create(request, options: JsonOptions);

        var resp = await _http.SendAsync(httpReq, ct);
        resp.EnsureSuccessStatusCode();

        var exaResponse = await resp.Content.ReadFromJsonAsync<ExaSearchResponse>(JsonOptions, ct);
        if (exaResponse?.Results == null) return new();

        return exaResponse.Results
            .Where(r => !string.IsNullOrWhiteSpace(r.Text))
            .Select(r => new WebSearchResult
            {
                Title = r.Title,
                Url = r.Url,
                Content = r.Text ?? "",
                Author = r.Author,
                PublishedDate = r.PublishedDate,
            })
            .ToList();
    }

    public async Task<bool> HealthCheckAsync(string apiKey)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/search");
            req.Headers.Add("x-api-key", apiKey);
            req.Content = JsonContent.Create(new ExaSearchRequest
            {
                Query = "test",
                NumResults = 1,
            }, options: JsonOptions);

            var resp = await _http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
