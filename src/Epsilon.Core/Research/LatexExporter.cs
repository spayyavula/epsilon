using Epsilon.Core.Models;

namespace Epsilon.Core.Research;

public static class LatexExporter
{
    public static string Export(ResearchProject project, List<ResearchStep> steps,
        ToolDefinition tool, string outputDir)
    {
        var safeTitle = string.Join("_",
            project.Title.Split(Path.GetInvalidFileNameChars(),
            StringSplitOptions.RemoveEmptyEntries)).Trim();

        if (safeTitle.Length > 60) safeTitle = safeTitle[..60];
        if (string.IsNullOrWhiteSpace(safeTitle)) safeTitle = "research_project";

        var fileName = $"{safeTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.tex";
        var filePath = Path.Combine(outputDir, fileName);

        var sb = new System.Text.StringBuilder();

        sb.AppendLine(@"\documentclass{article}");
        sb.AppendLine(@"\usepackage{amsmath,amssymb,amsthm}");
        sb.AppendLine(@"\usepackage[utf8]{inputenc}");
        sb.AppendLine(@"\usepackage[T1]{fontenc}");
        sb.AppendLine(@"\usepackage{geometry}");
        sb.AppendLine(@"\geometry{a4paper, margin=2.5cm}");
        sb.AppendLine(@"\usepackage{hyperref}");
        sb.AppendLine();
        sb.AppendLine($@"\title{{{EscapeLatex(project.Title)}}}");
        sb.AppendLine($@"\author{{Epsilon Mathematics Assistant}}");
        sb.AppendLine($@"\date{{{DateTime.Now:MMMM dd, yyyy}}}");
        sb.AppendLine();
        sb.AppendLine(@"\begin{document}");
        sb.AppendLine(@"\maketitle");
        sb.AppendLine();

        bool isProofTool = tool.ToolType == "ProofBuilder";

        foreach (var stepDef in tool.Steps)
        {
            var step = steps.FirstOrDefault(s => s.StepIndex == stepDef.Index);
            var content = step?.GeneratedContent ?? "";

            sb.AppendLine($@"\section{{{EscapeLatex(stepDef.Label)}}}");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(content))
            {
                // Wrap the final step of proof builder in a proof environment
                bool wrapInProof = isProofTool &&
                    (stepDef.Label.Contains("Proof", StringComparison.OrdinalIgnoreCase) ||
                     stepDef.Label.Contains("Construction", StringComparison.OrdinalIgnoreCase) ||
                     stepDef.Label.Contains("Formal", StringComparison.OrdinalIgnoreCase));

                if (wrapInProof)
                {
                    sb.AppendLine(@"\begin{proof}");
                    sb.AppendLine(ConvertMarkdownToLatex(content));
                    sb.AppendLine(@"\end{proof}");
                }
                else
                {
                    sb.AppendLine(ConvertMarkdownToLatex(content));
                }
            }
            else
            {
                sb.AppendLine(@"\textit{(Section not completed)}");
            }

            sb.AppendLine();
        }

        sb.AppendLine(@"\end{document}");

        Directory.CreateDirectory(outputDir);
        File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);

        return filePath;
    }

    private static string EscapeLatex(string text)
    {
        return text
            .Replace("\\", @"\textbackslash{}")
            .Replace("&", @"\&")
            .Replace("%", @"\%")
            .Replace("$", @"\$")
            .Replace("#", @"\#")
            .Replace("_", @"\_")
            .Replace("{", @"\{")
            .Replace("}", @"\}")
            .Replace("~", @"\textasciitilde{}")
            .Replace("^", @"\textasciicircum{}");
    }

    private static string ConvertMarkdownToLatex(string markdown)
    {
        // The content already contains LaTeX math ($...$ and $$...$$) which is valid in .tex
        // Convert the most common markdown structural elements
        var lines = markdown.Split('\n');
        var result = new System.Text.StringBuilder();
        bool inCodeBlock = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            // Code blocks
            if (line.StartsWith("```"))
            {
                if (!inCodeBlock)
                {
                    result.AppendLine(@"\begin{verbatim}");
                    inCodeBlock = true;
                }
                else
                {
                    result.AppendLine(@"\end{verbatim}");
                    inCodeBlock = false;
                }
                continue;
            }

            if (inCodeBlock)
            {
                result.AppendLine(line);
                continue;
            }

            // Headings
            if (line.StartsWith("### "))
                result.AppendLine($@"\subsubsection{{{line[4..]}}}");
            else if (line.StartsWith("## "))
                result.AppendLine($@"\subsection{{{line[3..]}}}");
            else if (line.StartsWith("# "))
                result.AppendLine($@"\section{{{line[2..]}}}");
            // Bullet lists
            else if (line.StartsWith("- ") || line.StartsWith("* "))
                result.AppendLine($@"\item {line[2..]}");
            // Numbered lists (simple: "1. ")
            else if (line.Length > 2 && char.IsDigit(line[0]) && line[1] == '.')
                result.AppendLine($@"\item {line[2..].TrimStart()}");
            // Bold **text**
            else
            {
                var converted = System.Text.RegularExpressions.Regex.Replace(
                    line, @"\*\*(.+?)\*\*", @"\textbf{$1}");
                converted = System.Text.RegularExpressions.Regex.Replace(
                    converted, @"\*(.+?)\*", @"\textit{$1}");
                result.AppendLine(converted);
            }
        }

        return result.ToString();
    }
}
