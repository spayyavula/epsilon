using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Epsilon.Core.WebSearch;

public static class PdfExporter
{
    static PdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static string Export(WebSearchResult result, string outputDir)
    {
        var safeTitle = string.Join("_",
            result.Title.Split(Path.GetInvalidFileNameChars(),
            StringSplitOptions.RemoveEmptyEntries)).Trim();

        if (safeTitle.Length > 80) safeTitle = safeTitle[..80];
        if (string.IsNullOrWhiteSpace(safeTitle)) safeTitle = "web_article";

        var fileName = $"{safeTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filePath = Path.Combine(outputDir, fileName);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                page.Header().Column(col =>
                {
                    col.Item().Text(result.Title)
                        .FontSize(18).Bold().FontColor(Colors.Blue.Darken2);

                    col.Item().PaddingTop(6).Text(text =>
                    {
                        text.Span("Source: ").SemiBold();
                        text.Span(result.Url).FontColor(Colors.Blue.Medium);
                    });

                    if (result.Author != null)
                    {
                        col.Item().PaddingTop(2).Text(text =>
                        {
                            text.Span("Author: ").SemiBold();
                            text.Span(result.Author);
                        });
                    }

                    if (result.PublishedDate != null)
                    {
                        col.Item().PaddingTop(2).Text(text =>
                        {
                            text.Span("Published: ").SemiBold();
                            text.Span(result.PublishedDate.Value.ToString("yyyy-MM-dd"));
                        });
                    }

                    col.Item().PaddingTop(4).Text(text =>
                    {
                        text.Span("Saved: ").SemiBold();
                        text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    });

                    col.Item().PaddingTop(10)
                        .LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(16).Text(result.Content)
                    .FontSize(11).LineHeight(1.5f);

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Saved by Epsilon — ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf(filePath);

        return filePath;
    }
}
