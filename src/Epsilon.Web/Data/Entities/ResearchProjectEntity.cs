using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epsilon.Web.Data.Entities;

[Table("research_projects")]
public class ResearchProjectEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("tool_type")]
    public string ToolType { get; set; } = "";

    [Required]
    [Column("title")]
    public string Title { get; set; } = "";

    [Column("current_step")]
    public int CurrentStep { get; set; }

    [Column("status")]
    public string Status { get; set; } = "active";

    [Column("provider_id")]
    public string? ProviderId { get; set; }

    [Column("model_id")]
    public string? ModelId { get; set; }

    [Column("web_search_enabled")]
    public bool WebSearchEnabled { get; set; }

    [Column("metadata")]
    public string? Metadata { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<ResearchStepEntity> Steps { get; set; } = new List<ResearchStepEntity>();
}
