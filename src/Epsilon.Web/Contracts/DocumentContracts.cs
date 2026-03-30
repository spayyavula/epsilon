namespace Epsilon.Web.Contracts;

public record DocumentDto(
    string Id, string FileName, string MimeType, long SizeBytes,
    int? PageCount, int ChunkCount, string Status, DateTime IngestedAt);
