namespace Epsilon.Core.Documents;

public static class TextChunker
{
    private const int ChunkSize = 2000;  // ~500 tokens
    private const int Overlap = 200;

    public static List<DocumentChunk> Chunk(string documentId, string text)
    {
        var chunks = new List<DocumentChunk>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        int pos = 0;
        int index = 0;

        while (pos < text.Length)
        {
            int end = Math.Min(pos + ChunkSize, text.Length);
            int splitAt = end;

            // Try to split at a natural boundary if not at the end
            if (end < text.Length)
            {
                // Try paragraph break first
                var paraBreak = text.LastIndexOf("\n\n", end, Math.Min(end - pos, 400));
                if (paraBreak > pos + ChunkSize / 2)
                {
                    splitAt = paraBreak + 2;
                }
                else
                {
                    // Try sentence break
                    var sentBreak = text.LastIndexOf(". ", end, Math.Min(end - pos, 400));
                    if (sentBreak > pos + ChunkSize / 2)
                    {
                        splitAt = sentBreak + 2;
                    }
                    else
                    {
                        // Try word break
                        var wordBreak = text.LastIndexOf(' ', end - 1, Math.Min(end - pos, 200));
                        if (wordBreak > pos)
                            splitAt = wordBreak + 1;
                    }
                }
            }

            var chunkText = text[pos..splitAt].Trim();
            if (chunkText.Length > 0)
            {
                chunks.Add(new DocumentChunk
                {
                    DocumentId = documentId,
                    ChunkIndex = index,
                    Text = chunkText,
                    CharStart = pos,
                    CharEnd = splitAt,
                });
                index++;
            }

            pos = splitAt - Overlap;
            if (pos <= chunks.LastOrDefault()?.CharStart)
                pos = splitAt; // Prevent infinite loop
        }

        return chunks;
    }
}

public class DocumentChunk
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DocumentId { get; set; } = "";
    public int ChunkIndex { get; set; }
    public string Text { get; set; } = "";
    public int CharStart { get; set; }
    public int CharEnd { get; set; }
}
