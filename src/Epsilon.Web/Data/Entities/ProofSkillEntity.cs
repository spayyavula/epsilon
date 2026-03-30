using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epsilon.Web.Data.Entities;

[Table("proof_skills")]
public class ProofSkillEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("category")]
    public string Category { get; set; } = "";

    [Column("level")]
    public int Level { get; set; }

    [Column("problems_attempted")]
    public int ProblemsAttempted { get; set; }

    [Column("problems_solved")]
    public int ProblemsSolved { get; set; }

    [Column("last_practiced")]
    public DateTime? LastPracticed { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
