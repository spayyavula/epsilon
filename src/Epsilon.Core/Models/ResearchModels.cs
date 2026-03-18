namespace Epsilon.Core.Models;

public class ProofSkill
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Category { get; set; } = "";
    public int Level { get; set; } // 0=Beginner, 1=Intermediate, 2=Advanced, 3=Expert
    public int ProblemsAttempted { get; set; }
    public int ProblemsSolved { get; set; }
    public DateTime? LastPracticed { get; set; }
}

public class Flashcard
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Front { get; set; } = "";
    public string Back { get; set; } = "";
    public string Category { get; set; } = "General";
    public double EaseFactor { get; set; } = 2.5;
    public int IntervalDays { get; set; } = 1;
    public int Repetitions { get; set; }
    public DateTime NextReview { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

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
