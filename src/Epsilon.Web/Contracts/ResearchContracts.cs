namespace Epsilon.Web.Contracts;

public record CreateProjectRequest(string ToolType, string ProviderId, string ModelId, bool WebSearchEnabled = false);
public record UpdateProjectRequest(string? Title, int? CurrentStep, string? Status, bool? WebSearchEnabled);
public record SaveStepRequest(string UserInput);
public record GenerateStepRequest(string? ProviderId = null, string? ModelId = null);

public record ResearchProjectDto(
    string Id, string ToolType, string Title, int CurrentStep, string Status,
    string? ProviderId, string? ModelId, bool WebSearchEnabled,
    DateTime CreatedAt, DateTime UpdatedAt);

public record ResearchStepDto(
    string Id, int StepIndex, string UserInput, string GeneratedContent,
    string Status, DateTime? GeneratedAt);

public record ToolDefinitionDto(
    string ToolType, string DisplayName, string Icon, string Description,
    string AccentColor, List<StepDefinitionDto> Steps);

public record StepDefinitionDto(
    int Index, string Label, string InputLabel, string? InputPlaceholder, bool IsAutoGenerate);
