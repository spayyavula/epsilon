using Microsoft.Win32;

namespace Epsilon.Core.Documents;

public static class OneDriveDetector
{
    public static OneDriveInfo? Detect()
    {
        // Try environment variables first (most reliable)
        var paths = new List<(string path, string label)>();

        var personal = Environment.GetEnvironmentVariable("OneDrive");
        var consumer = Environment.GetEnvironmentVariable("OneDriveConsumer");
        var business = Environment.GetEnvironmentVariable("OneDriveCommercial");

        if (!string.IsNullOrEmpty(business) && Directory.Exists(business))
            paths.Add((business, "OneDrive - Work"));
        if (!string.IsNullOrEmpty(consumer) && Directory.Exists(consumer))
            paths.Add((consumer, "OneDrive - Personal"));
        if (!string.IsNullOrEmpty(personal) && Directory.Exists(personal) && paths.Count == 0)
            paths.Add((personal, "OneDrive"));

        // Fallback: check common paths
        if (paths.Count == 0)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var commonPaths = new[]
            {
                Path.Combine(userProfile, "OneDrive"),
                Path.Combine(userProfile, "OneDrive - Personal"),
            };

            foreach (var p in commonPaths)
            {
                if (Directory.Exists(p))
                {
                    paths.Add((p, Path.GetFileName(p)));
                    break;
                }
            }
        }

        if (paths.Count == 0) return null;

        // Use the first found path as primary
        var primary = paths[0];
        return new OneDriveInfo
        {
            RootPath = primary.path,
            Label = primary.label,
            AllAccounts = paths.Select(p => new OneDriveAccount
            {
                Path = p.path,
                Label = p.label,
            }).ToList(),
        };
    }

    public static List<OneDriveFolder> ListSubfolders(string rootPath)
    {
        var folders = new List<OneDriveFolder>();
        if (!Directory.Exists(rootPath)) return folders;

        try
        {
            foreach (var dir in Directory.GetDirectories(rootPath))
            {
                var name = Path.GetFileName(dir);
                // Skip hidden/system folders
                if (name.StartsWith('.')) continue;

                var docCount = CountSupportedFiles(dir);
                folders.Add(new OneDriveFolder
                {
                    Path = dir,
                    Name = name,
                    DocumentCount = docCount,
                });
            }
        }
        catch { /* Access denied on some folders */ }

        return folders.OrderBy(f => f.Name).ToList();
    }

    private static int CountSupportedFiles(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .Count(f =>
                {
                    var ext = Path.GetExtension(f).ToLower();
                    return ext is ".pdf" or ".txt" or ".md" or ".docx";
                });
        }
        catch { return 0; }
    }
}

public class OneDriveInfo
{
    public string RootPath { get; set; } = "";
    public string Label { get; set; } = "";
    public List<OneDriveAccount> AllAccounts { get; set; } = new();
}

public class OneDriveAccount
{
    public string Path { get; set; } = "";
    public string Label { get; set; } = "";
}

public class OneDriveFolder
{
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
    public int DocumentCount { get; set; }
    public bool IsSelected { get; set; }
}
