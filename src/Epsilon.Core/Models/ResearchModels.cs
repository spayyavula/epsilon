namespace Epsilon.Core.Models;

public class ResearchProject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ToolType { get; set; } = "";
    public string Title { get; set; } = "";
    public int CurrentStep { get; set; }
    public string Status { get; set; } = "active";
    public string? ProviderId { get; set; }
    public string? ModelId { get; set; }
    public bool WebSearchEnabled { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ResearchStep
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectId { get; set; } = "";
    public int StepIndex { get; set; }
    public string UserInput { get; set; } = "";
    public string GeneratedContent { get; set; } = "";
    public string Status { get; set; } = "empty";
    public DateTime? GeneratedAt { get; set; }
}
