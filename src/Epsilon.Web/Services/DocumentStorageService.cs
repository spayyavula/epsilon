using Epsilon.Core.Documents;
using Epsilon.Web.Data;
using Epsilon.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Epsilon.Web.Services;

public class DocumentStorageService
{
    private readonly AppDbContext _db;
    private readonly string _storagePath;

    public DocumentStorageService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _storagePath = config["Storage:DocumentsPath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "storage");
    }

    public async Task<DocumentEntity> UploadAsync(Guid userId, string fileName, string mimeType, Stream fileStream)
    {
        var userDir = Path.Combine(_storagePath, userId.ToString(), "documents");
        Directory.CreateDirectory(userDir);

        var doc = new DocumentEntity
        {
            UserId = userId,
            FileName = fileName,
            MimeType = mimeType,
        };

        var ext = Path.GetExtension(fileName);
        doc.StoragePath = Path.Combine(userId.ToString(), "documents", $"{doc.Id}{ext}");

        var fullPath = Path.Combine(_storagePath, doc.StoragePath);
        await using var fs = File.Create(fullPath);
        await fileStream.CopyToAsync(fs);
        await fs.FlushAsync();

        doc.SizeBytes = new FileInfo(fullPath).Length;

        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();

        return doc;
    }

    public async Task<List<DocumentEntity>> ListAsync(Guid userId)
    {
        return await _db.Documents
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.IngestedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid docId)
    {
        var doc = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == docId && d.UserId == userId);
        if (doc == null) return false;

        var fullPath = Path.Combine(_storagePath, doc.StoragePath);
        if (File.Exists(fullPath)) File.Delete(fullPath);

        _db.Documents.Remove(doc);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task ProcessDocumentAsync(DocumentEntity doc, CancellationToken ct = default)
    {
        try
        {
            doc.Status = "processing";
            await _db.SaveChangesAsync(ct);

            var fullPath = Path.Combine(_storagePath, doc.StoragePath);
            var extractor = TextExtractorFactory.GetExtractor(doc.MimeType);
            var text = extractor.Extract(fullPath);
            var chunks = TextChunker.Chunk(doc.Id.ToString(), text);

            foreach (var chunk in chunks)
            {
                _db.DocumentChunks.Add(new DocumentChunkEntity
                {
                    DocumentId = doc.Id,
                    UserId = doc.UserId,
                    ChunkIndex = chunk.ChunkIndex,
                    Content = chunk.Text,
                    CharStart = chunk.CharStart,
                    CharEnd = chunk.CharEnd,
                });
            }

            doc.ChunkCount = chunks.Count;
            doc.Status = "ready";

            if (doc.MimeType == "application/pdf")
            {
                try
                {
                    using var pdf = UglyToad.PdfPig.PdfDocument.Open(fullPath);
                    doc.PageCount = pdf.NumberOfPages;
                }
                catch { /* ignore PDF page count errors */ }
            }

            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            doc.Status = "error";
            await _db.SaveChangesAsync(CancellationToken.None);
        }
    }
}
