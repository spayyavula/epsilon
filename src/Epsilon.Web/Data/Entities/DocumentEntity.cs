using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epsilon.Web.Data.Entities;

[Table("documents")]
public class DocumentEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("filename")]
    public string FileName { get; set; } = "";

    [Required]
    [Column("storage_path")]
    public string StoragePath { get; set; } = "";

    [Required]
    [Column("mime_type")]
    public string MimeType { get; set; } = "";

    [Column("size_bytes")]
    public long SizeBytes { get; set; }

    [Column("page_count")]
    public int? PageCount { get; set; }

    [Column("chunk_count")]
    public int ChunkCount { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("ingested_at")]
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<DocumentChunkEntity> Chunks { get; set; } = new List<DocumentChunkEntity>();
}
