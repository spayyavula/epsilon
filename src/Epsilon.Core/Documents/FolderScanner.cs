using Epsilon.Core.Database;
using Epsilon.Core.Models;

namespace Epsilon.Core.Documents;

public class FolderScanner
{
    private readonly DatabaseService _db;
    private readonly DocumentProcessor _processor;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".txt", ".md", ".docx"
    };

    public event Action<string, int, int>? ScanProgress; // folderId, processed, total

    public FolderScanner(DatabaseService db, DocumentProcessor processor)
    {
        _db = db;
        _processor = processor;
    }

    public async Task ScanFolderAsync(LibraryFolder folder, CancellationToken ct = default)
    {
        if (!Directory.Exists(folder.Path)) return;

        // Get files already tracked for this folder
        var existingPaths = new HashSet<string>(
            _db.GetDocumentPathsForFolder(folder.Id),
            StringComparer.OrdinalIgnoreCase);

        // Find all supported files recursively
        var files = Directory.EnumerateFiles(folder.Path, "*.*", SearchOption.AllDirectories)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f)))
            .ToList();

        var newFiles = files.Where(f => !existingPaths.Contains(f)).ToList();
        int processed = 0;

        foreach (var filePath in newFiles)
        {
            if (ct.IsCancellationRequested) break;

            var fileName = Path.GetFileName(filePath);
            var ext = Path.GetExtension(filePath).ToLower();
            var fileInfo = new FileInfo(filePath);

            var mimeType = ext switch
            {
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".md" => "text/markdown",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream",
            };

            var doc = new DocumentInfo
            {
                Id = Guid.NewGuid().ToString(),
                FileName = fileName,
                FilePath = filePath, // Reference in-place, no copy
                MimeType = mimeType,
                SizeBytes = fileInfo.Length,
                Status = "processing",
                FolderId = folder.Id,
            };

            _db.InsertDocumentWithFolder(doc);
            await _processor.ProcessDocumentAsync(doc, ct);

            processed++;
            ScanProgress?.Invoke(folder.Id, processed, newFiles.Count);
        }

        // Remove documents whose source files no longer exist on disk
        var currentPaths = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
        var folderDocs = _db.ListDocuments()
            .Where(d => d.FolderId == folder.Id)
            .ToList();

        foreach (var doc in folderDocs)
        {
            if (!currentPaths.Contains(doc.FilePath))
            {
                _db.DeleteChunks(doc.Id);
                _db.DeleteDocument(doc.Id);
            }
        }

        _db.UpdateFolderScannedAt(folder.Id, DateTime.UtcNow);
    }
}
