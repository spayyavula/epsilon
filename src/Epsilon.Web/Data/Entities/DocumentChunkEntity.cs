using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace Epsilon.Web.Data.Entities;

[Table("document_chunks")]
public class DocumentChunkEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("document_id")]
    public Guid DocumentId { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("chunk_index")]
    public int ChunkIndex { get; set; }

    [Required]
    [Column("content")]
    public string Content { get; set; } = "";

    [Column("char_start")]
    public int CharStart { get; set; }

    [Column("char_end")]
    public int CharEnd { get; set; }

    [Column("search_vector")]
    public NpgsqlTsVector? SearchVector { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public DocumentEntity Document { get; set; } = null!;
}
