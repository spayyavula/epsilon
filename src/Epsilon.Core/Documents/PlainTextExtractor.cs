using System.Text;

namespace Epsilon.Core.Documents;

public class PlainTextExtractor : ITextExtractor
{
    public string Extract(string filePath)
    {
        return File.ReadAllText(filePath, Encoding.UTF8);
    }
}
