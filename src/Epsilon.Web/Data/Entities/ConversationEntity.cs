using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epsilon.Web.Data.Entities;

[Table("conversations")]
public class ConversationEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("title")]
    public string Title { get; set; } = "New Chat";

    [Column("provider_id")]
    public string? ProviderId { get; set; }

    [Column("model_id")]
    public string? ModelId { get; set; }

    [Column("system_prompt_id")]
    public Guid? SystemPromptId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<MessageEntity> Messages { get; set; } = new List<MessageEntity>();
}
