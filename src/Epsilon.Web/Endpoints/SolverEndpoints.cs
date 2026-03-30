using System.Text.Json;
using Epsilon.Web.Auth;
using Epsilon.Web.Contracts;
using Epsilon.Web.Services;

namespace Epsilon.Web.Endpoints;

public static class SolverEndpoints
{
    public static void MapSolverEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/solver").RequireAuthorization().WithTags("Solver");

        group.MapPost("/solve", async (SolveRequest req, UserContext user,
            WebSolverService solver, HttpContext ctx, CancellationToken ct) =>
        {
            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";

            var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await foreach (var chunk in solver.SolveAsync(
                user.UserId, req.Equation, req.ProviderId, req.ModelId, ct))
            {
                var data = JsonSerializer.Serialize(chunk, jsonOpts);
                await ctx.Response.WriteAsync($"data: {data}\n\n", ct);
                await ctx.Response.Body.FlushAsync(ct);
            }

            await ctx.Response.WriteAsync("data: [DONE]\n\n", ct);
            await ctx.Response.Body.FlushAsync(ct);
        });
    }
}
