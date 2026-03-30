using System.Text.Json;
using Epsilon.Core.Models;
using Epsilon.Core.Research;
using Epsilon.Web.Auth;
using Epsilon.Web.Contracts;
using Epsilon.Web.Data.Entities;
using Epsilon.Web.Services;

namespace Epsilon.Web.Endpoints;

public static class ResearchEndpoints
{
    public static void MapResearchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/research").RequireAuthorization().WithTags("Research");

        group.MapGet("/tools", () =>
        {
            return ToolRegistry.GetAll().Select(t => new ToolDefinitionDto(
                t.ToolType, t.DisplayName, t.Icon, t.Description, t.AccentColor,
                t.Steps.Select(s => new StepDefinitionDto(
                    s.Index, s.Label, s.InputLabel, s.InputPlaceholder, s.IsAutoGenerate)).ToList()));
        });

        group.MapGet("/projects", async (UserContext user, WebResearchService research) =>
        {
            var projects = await research.ListProjectsAsync(user.UserId);
            return projects.Select(MapProject);
        });

        group.MapPost("/projects", async (CreateProjectRequest req, UserContext user, WebResearchService research) =>
        {
            var project = await research.CreateProjectAsync(
                user.UserId, req.ToolType, req.ProviderId, req.ModelId, req.WebSearchEnabled);
            return Results.Created($"/api/research/projects/{project.Id}", MapProject(project));
        });

        group.MapGet("/projects/{id:guid}", async (Guid id, UserContext user, WebResearchService research) =>
        {
            var project = await research.GetProjectAsync(user.UserId, id);
            return project != null ? Results.Ok(MapProject(project)) : Results.NotFound();
        });

        group.MapPatch("/projects/{id:guid}", async (Guid id, UpdateProjectRequest req,
            UserContext user, WebResearchService research) =>
        {
            return await research.UpdateProjectAsync(user.UserId, id,
                req.Title, req.CurrentStep, req.Status, req.WebSearchEnabled)
                ? Results.NoContent()
                : Results.NotFound();
        });

        group.MapDelete("/projects/{id:guid}", async (Guid id, UserContext user, WebResearchService research) =>
        {
            return await research.DeleteProjectAsync(user.UserId, id)
                ? Results.NoContent()
                : Results.NotFound();
        });

        group.MapGet("/projects/{id:guid}/steps", async (Guid id, UserContext user, WebResearchService research) =>
        {
            var steps = await research.GetStepsAsync(user.UserId, id);
            return steps.Select(s => new ResearchStepDto(
                s.Id.ToString(), s.StepIndex, s.UserInput, s.GeneratedContent, s.Status, s.GeneratedAt));
        });

        group.MapPut("/projects/{projectId:guid}/steps/{stepIndex:int}", async (
            Guid projectId, int stepIndex, SaveStepRequest req,
            UserContext user, WebResearchService research) =>
        {
            return await research.SaveStepAsync(user.UserId, projectId, stepIndex, req.UserInput)
                ? Results.NoContent()
                : Results.NotFound();
        });

        group.MapPost("/projects/{projectId:guid}/steps/{stepIndex:int}/generate", async (
            Guid projectId, int stepIndex, GenerateStepRequest? req,
            UserContext user, WebResearchService research, HttpContext ctx, CancellationToken ct) =>
        {
            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";

            var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await foreach (var chunk in research.StreamStepAsync(
                user.UserId, projectId, stepIndex, req?.ProviderId, req?.ModelId, ct))
            {
                var data = JsonSerializer.Serialize(chunk, jsonOpts);
                await ctx.Response.WriteAsync($"data: {data}\n\n", ct);
                await ctx.Response.Body.FlushAsync(ct);
            }

            await ctx.Response.WriteAsync("data: [DONE]\n\n", ct);
            await ctx.Response.Body.FlushAsync(ct);
        });

        group.MapGet("/projects/{projectId:guid}/pdf", async (Guid projectId,
            UserContext user, WebResearchService research) =>
        {
            var project = await research.GetProjectAsync(user.UserId, projectId);
            if (project == null) return Results.NotFound();

            var steps = await research.GetStepsAsync(user.UserId, projectId);
            var tool = ToolRegistry.Get(project.ToolType);
            if (tool == null) return Results.NotFound();

            // Map entities to Core models
            var coreProject = new ResearchProject
            {
                Id = project.Id.ToString(),
                ToolType = project.ToolType,
                Title = project.Title,
                CurrentStep = project.CurrentStep,
                Status = project.Status,
                ProviderId = project.ProviderId,
                ModelId = project.ModelId,
                CreatedAt = project.CreatedAt,
            };

            var coreSteps = steps.Select(s => new ResearchStep
            {
                Id = s.Id.ToString(),
                ProjectId = s.ProjectId.ToString(),
                StepIndex = s.StepIndex,
                UserInput = s.UserInput,
                GeneratedContent = s.GeneratedContent,
                Status = s.Status,
                GeneratedAt = s.GeneratedAt,
            }).ToList();

            // Generate PDF to a temp file
            var tempDir = Path.Combine(Path.GetTempPath(), "epsilon-pdfs");
            Directory.CreateDirectory(tempDir);
            var pdfPath = ResearchPdfExporter.Export(coreProject, coreSteps, tool, tempDir);

            var bytes = await File.ReadAllBytesAsync(pdfPath);
            File.Delete(pdfPath); // Clean up temp file

            return Results.File(bytes, "application/pdf",
                $"{coreProject.Title.Replace(" ", "_")}.pdf");
        });
    }

    private static ResearchProjectDto MapProject(ResearchProjectEntity p) => new(
        p.Id.ToString(), p.ToolType, p.Title, p.CurrentStep, p.Status,
        p.ProviderId, p.ModelId, p.WebSearchEnabled, p.CreatedAt, p.UpdatedAt);
}
