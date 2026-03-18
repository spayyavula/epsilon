namespace Epsilon.Core.Documents;

public static class TextExtractorFactory
{
    public static ITextExtractor GetExtractor(string mimeType)
    {
        return mimeType switch
        {
            "application/pdf" => new PdfTextExtractor(),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => new DocxTextExtractor(),
            "text/plain" or "text/markdown" => new PlainTextExtractor(),
            _ => new PlainTextExtractor(),
        };
    }
}
