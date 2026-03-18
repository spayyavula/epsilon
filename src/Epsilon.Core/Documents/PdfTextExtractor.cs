using UglyToad.PdfPig;

namespace Epsilon.Core.Documents;

public class PdfTextExtractor : ITextExtractor
{
    public string Extract(string filePath)
    {
        using var document = PdfDocument.Open(filePath);
        var pages = new List<string>();

        foreach (var page in document.GetPages())
        {
            var words = page.GetWords();
            var text = string.Join(" ", words.Select(w => w.Text));
            if (!string.IsNullOrWhiteSpace(text))
                pages.Add(text);
        }

        return string.Join("\n\n", pages);
    }
}
