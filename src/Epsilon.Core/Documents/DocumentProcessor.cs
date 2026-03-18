using Epsilon.Core.Database;
using Epsilon.Core.Models;

namespace Epsilon.Core.Documents;

public class DocumentProcessor
{
    private readonly DatabaseService _db;

    public event Action<string, string>? StatusChanged; // docId, status

    public DocumentProcessor(DatabaseService db)
    {
        _db = db;
    }

    public async Task ProcessDocumentAsync(DocumentInfo doc, CancellationToken ct = default)
    {
        try
        {
            _db.UpdateDocumentStatus(doc.Id, "processing", 0);
            StatusChanged?.Invoke(doc.Id, "processing");

            var (chunks, pageCount) = await Task.Run(() =>
            {
                var extractor = TextExtractorFactory.GetExtractor(doc.MimeType);
                var text = extractor.Extract(doc.FilePath);
                var docChunks = TextChunker.Chunk(doc.Id, text);

                // Estimate page count from text length for non-PDFs
                int? pages = doc.MimeType == "application/pdf" ? EstimatePdfPages(doc.FilePath) : null;

                return (docChunks, pages);
            }, ct);

            _db.InsertChunks(chunks);
            _db.UpdateDocumentStatus(doc.Id, "ready", chunks.Count, pageCount);
            StatusChanged?.Invoke(doc.Id, "ready");
        }
        catch (Exception)
        {
            _db.UpdateDocumentStatus(doc.Id, "error", 0);
            StatusChanged?.Invoke(doc.Id, "error");
        }
    }

    private static int? EstimatePdfPages(string filePath)
    {
        try
        {
            using var doc = UglyToad.PdfPig.PdfDocument.Open(filePath);
            return doc.NumberOfPages;
        }
        catch { return null; }
    }
}
