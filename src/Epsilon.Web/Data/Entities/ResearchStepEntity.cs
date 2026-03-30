using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epsilon.Web.Data.Entities;

[Table("research_steps")]
public class ResearchStepEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("project_id")]
    public Guid ProjectId { get; set; }

    [Column("step_index")]
    public int StepIndex { get; set; }

    [Column("user_input")]
    public string UserInput { get; set; } = "";

    [Column("generated_content")]
    public string GeneratedContent { get; set; } = "";

    [Column("status")]
    public string Status { get; set; } = "empty";

    [Column("generated_at")]
    public DateTime? GeneratedAt { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public ResearchProjectEntity Project { get; set; } = null!;
}
