using System.Text.Json;
using Epsilon.Web.Auth;
using Epsilon.Web.Contracts;
using Epsilon.Web.Services;

namespace Epsilon.Web.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/conversations").RequireAuthorization().WithTags("Chat");

        group.MapGet("/", async (UserContext user, WebChatService chat) =>
        {
            var conversations = await chat.ListConversationsAsync(user.UserId);
            return conversations.Select(c => new ConversationDto(
                c.Id.ToString(), c.Title, c.ProviderId, c.ModelId, c.CreatedAt, c.UpdatedAt));
        });

        group.MapPost("/", async (CreateConversationRequest req, UserContext user, WebChatService chat) =>
        {
            var conv = await chat.CreateConversationAsync(user.UserId, req.Title, req.ProviderId, req.ModelId);
            return Results.Created($"/api/conversations/{conv.Id}",
                new ConversationDto(conv.Id.ToString(), conv.Title, conv.ProviderId, conv.ModelId, conv.CreatedAt, conv.UpdatedAt));
        });

        group.MapDelete("/{id:guid}", async (Guid id, UserContext user, WebChatService chat) =>
        {
            return await chat.DeleteConversationAsync(user.UserId, id)
                ? Results.NoContent()
                : Results.NotFound();
        });

        group.MapPatch("/{id:guid}", async (Guid id, RenameConversationRequest req, UserContext user, WebChatService chat) =>
        {
            return await chat.RenameConversationAsync(user.UserId, id, req.Title)
                ? Results.NoContent()
                : Results.NotFound();
        });

        group.MapGet("/{id:guid}/messages", async (Guid id, UserContext user, WebChatService chat) =>
        {
            var messages = await chat.GetMessagesAsync(user.UserId, id);
            return messages.Select(m => new MessageDto(
                m.Id.ToString(), m.Role, m.Content, m.Model, m.CreatedAt));
        });

        group.MapPost("/{id:guid}/messages", async (Guid id, SendMessageRequest req,
            UserContext user, WebChatService chat, HttpContext ctx, CancellationToken ct) =>
        {
            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";

            var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await foreach (var chunk in chat.SendMessageStreamingAsync(
                user.UserId, id, req.Content, req.ProviderId, req.ModelId, req.WebSearchEnabled, ct))
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
