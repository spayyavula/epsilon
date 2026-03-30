using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epsilon.Web.Data.Entities;

[Table("flashcards")]
public class FlashcardEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("front")]
    public string Front { get; set; } = "";

    [Required]
    [Column("back")]
    public string Back { get; set; } = "";

    [Column("category")]
    public string Category { get; set; } = "General";

    [Column("ease_factor")]
    public double EaseFactor { get; set; } = 2.5;

    [Column("interval_days")]
    public int IntervalDays { get; set; } = 1;

    [Column("repetitions")]
    public int Repetitions { get; set; }

    [Column("next_review")]
    public DateTime NextReview { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
