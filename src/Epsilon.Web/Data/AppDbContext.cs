using Epsilon.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Epsilon.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserApiKey> UserApiKeys => Set<UserApiKey>();
    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    public DbSet<DocumentChunkEntity> DocumentChunks => Set<DocumentChunkEntity>();
    public DbSet<ResearchProjectEntity> ResearchProjects => Set<ResearchProjectEntity>();
    public DbSet<ResearchStepEntity> ResearchSteps => Set<ResearchStepEntity>();
    public DbSet<FlashcardEntity> Flashcards => Set<FlashcardEntity>();
    public DbSet<SystemPromptEntity> SystemPrompts => Set<SystemPromptEntity>();
    public DbSet<ProofSkillEntity> ProofSkills => Set<ProofSkillEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<UserApiKey>(e =>
        {
            e.HasIndex(k => new { k.UserId, k.ProviderId }).IsUnique();
        });

        modelBuilder.Entity<ConversationEntity>(e =>
        {
            e.HasIndex(c => new { c.UserId, c.UpdatedAt }).IsDescending(false, true);
            e.HasMany(c => c.Messages).WithOne(m => m.Conversation).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MessageEntity>(e =>
        {
            e.HasIndex(m => new { m.ConversationId, m.CreatedAt });
        });

        modelBuilder.Entity<DocumentEntity>(e =>
        {
            e.HasIndex(d => d.UserId);
            e.HasMany(d => d.Chunks).WithOne(c => c.Document).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentChunkEntity>(e =>
        {
            e.HasIndex(c => c.DocumentId);
            e.HasIndex(c => c.SearchVector).HasMethod("gin");
            e.Property(c => c.SearchVector)
                .HasComputedColumnSql("to_tsvector('english', content)", stored: true);
        });

        modelBuilder.Entity<ResearchProjectEntity>(e =>
        {
            e.HasIndex(r => new { r.UserId, r.UpdatedAt }).IsDescending(false, true);
            e.HasMany(r => r.Steps).WithOne(s => s.Project).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ResearchStepEntity>(e =>
        {
            e.HasIndex(s => new { s.ProjectId, s.StepIndex }).IsUnique();
        });

        modelBuilder.Entity<FlashcardEntity>(e =>
        {
            e.HasIndex(f => new { f.UserId, f.NextReview });
        });

        modelBuilder.Entity<ProofSkillEntity>(e =>
        {
            e.HasIndex(p => new { p.UserId, p.Category }).IsUnique();
        });

        modelBuilder.Entity<SystemPromptEntity>(e =>
        {
            e.HasIndex(s => new { s.UserId, s.IsDefault });
        });

        modelBuilder.Entity<SystemPromptEntity>().HasData(new SystemPromptEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            UserId = null,
            Name = "Mathematics Assistant",
            Domain = "general",
            Content = "You are Epsilon, an advanced AI mathematics research assistant. You help with solving equations step by step, constructing and verifying mathematical proofs, explaining mathematical concepts with rigor and intuition, and formal verification with Lean 4. Use LaTeX notation: inline $...$ and display $$...$$. Be precise, rigorous, and pedagogically clear.",
            IsDefault = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
    }
}
