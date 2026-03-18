using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Epsilon.Core.Database;
using Epsilon.Core.Documents;
using Epsilon.Core.Models;

namespace Epsilon.App.ViewModels;

public partial class DocumentsViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly DocumentProcessor _processor;
    private readonly FolderScanner _scanner;
    private readonly string _docsDir;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _oneDriveAvailable;

    [ObservableProperty]
    private string _oneDriveLabel = "";

    public ObservableCollection<DocumentInfo> Documents { get; } = new();
    public ObservableCollection<LibraryFolder> Folders { get; } = new();
    public ObservableCollection<OneDriveFolder> OneDriveSubfolders { get; } = new();

    public DocumentsViewModel(DatabaseService db, DocumentProcessor processor, FolderScanner scanner, AppConfig config)
    {
        _db = db;
        _processor = processor;
        _scanner = scanner;
        _docsDir = config.DocsDirectory;

        _processor.StatusChanged += OnDocumentStatusChanged;
        _scanner.ScanProgress += OnScanProgress;
        LoadFolders();
        LoadDocuments();
        DetectOneDrive();
    }

    private OneDriveInfo? _oneDriveInfo;

    private void DetectOneDrive()
    {
        _oneDriveInfo = OneDriveDetector.Detect();
        OneDriveAvailable = _oneDriveInfo != null;
        OneDriveLabel = _oneDriveInfo?.Label ?? "";
    }

    private void OnDocumentStatusChanged(string docId, string status)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var doc = Documents.FirstOrDefault(d => d.Id == docId);
            if (doc != null)
            {
                doc.Status = status;
                var idx = Documents.IndexOf(doc);
                Documents.RemoveAt(idx);
                Documents.Insert(idx, doc);
                StatusMessage = status == "ready"
                    ? $"{doc.FileName} processed successfully."
                    : status == "error"
                        ? $"Failed to process {doc.FileName}."
                        : $"Processing {doc.FileName}...";
            }
            else if (status == "ready")
            {
                // Document added by folder scan — reload
                LoadDocuments();
                LoadFolders();
            }
        });
    }

    private void OnScanProgress(string folderId, int processed, int total)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            StatusMessage = $"Scanning folder: {processed}/{total} files processed...";
        });
    }

    private void LoadDocuments()
    {
        Documents.Clear();
        foreach (var doc in _db.ListDocuments())
            Documents.Add(doc);
    }

    private void LoadFolders()
    {
        Folders.Clear();
        foreach (var folder in _db.ListFolders())
            Folders.Add(folder);
    }

    [RelayCommand]
    private void AddDocument()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Documents|*.pdf;*.txt;*.md;*.docx|PDF Files|*.pdf|Text Files|*.txt;*.md|Word Documents|*.docx|All Files|*.*",
            Multiselect = true,
        };

        if (dialog.ShowDialog() != true) return;

        foreach (var filePath in dialog.FileNames)
        {
            var fileName = Path.GetFileName(filePath);
            var ext = Path.GetExtension(filePath).ToLower();
            var docId = Guid.NewGuid().ToString();
            var destPath = Path.Combine(_docsDir, $"{docId}{ext}");

            File.Copy(filePath, destPath);

            var mimeType = ext switch
            {
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".md" => "text/markdown",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream",
            };

            var fileInfo = new FileInfo(filePath);

            var doc = new DocumentInfo
            {
                Id = docId,
                FileName = fileName,
                FilePath = destPath,
                MimeType = mimeType,
                SizeBytes = fileInfo.Length,
                Status = "processing",
            };

            _db.InsertDocument(doc);
            Documents.Add(doc);
            _ = _processor.ProcessDocumentAsync(doc);
        }

        StatusMessage = $"Added {dialog.FileNames.Length} document(s). Processing...";
    }

    [RelayCommand]
    private void RemoveDocument(DocumentInfo doc)
    {
        // Only delete file if it was manually added (copied to app storage)
        if (doc.FolderId == null)
        {
            try
            {
                if (File.Exists(doc.FilePath))
                    File.Delete(doc.FilePath);
            }
            catch { /* File may be in use */ }
        }

        _db.DeleteChunks(doc.Id);
        _db.DeleteDocument(doc.Id);
        Documents.Remove(doc);
        LoadFolders(); // Refresh folder doc counts
        StatusMessage = $"Removed {doc.FileName}.";
    }

    [RelayCommand]
    private void LinkFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select a folder containing mathematics documents",
        };

        if (dialog.ShowDialog() != true) return;

        var folderPath = dialog.FolderName;

        // Check if already linked
        if (Folders.Any(f => f.Path.Equals(folderPath, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = "This folder is already linked.";
            return;
        }

        var folder = new LibraryFolder
        {
            Path = folderPath,
            Label = Path.GetFileName(folderPath) ?? folderPath,
        };

        _db.InsertFolder(folder);
        Folders.Add(folder);
        StatusMessage = $"Linked folder: {folder.Label}. Scanning...";

        _ = ScanFolderAsync(folder);
    }

    [RelayCommand]
    private async Task RefreshFolder(LibraryFolder folder)
    {
        if (IsScanning) return;
        StatusMessage = $"Rescanning {folder.Label}...";
        await ScanFolderAsync(folder);
    }

    [RelayCommand]
    private void UnlinkFolder(LibraryFolder folder)
    {
        // Remove all documents from this folder
        var folderDocs = Documents.Where(d => d.FolderId == folder.Id).ToList();
        foreach (var doc in folderDocs)
        {
            _db.DeleteChunks(doc.Id);
            _db.DeleteDocument(doc.Id);
            Documents.Remove(doc);
        }

        _db.DeleteFolder(folder.Id);
        Folders.Remove(folder);
        StatusMessage = $"Unlinked folder: {folder.Label}. {folderDocs.Count} document(s) removed.";
    }

    [RelayCommand]
    private void ConnectOneDrive()
    {
        if (_oneDriveInfo == null) return;

        // Load subfolder list for the picker
        OneDriveSubfolders.Clear();
        foreach (var sub in OneDriveDetector.ListSubfolders(_oneDriveInfo.RootPath))
            OneDriveSubfolders.Add(sub);

        if (OneDriveSubfolders.Count == 0)
        {
            StatusMessage = "No folders found in OneDrive.";
            return;
        }

        // Show picker (will be handled by the view)
        ShowOneDrivePicker?.Invoke();
    }

    public event Action? ShowOneDrivePicker;

    [RelayCommand]
    private async Task LinkSelectedOneDriveFolders()
    {
        var selected = OneDriveSubfolders.Where(f => f.IsSelected).ToList();
        if (selected.Count == 0)
        {
            StatusMessage = "No folders selected.";
            return;
        }

        foreach (var sub in selected)
        {
            if (Folders.Any(f => f.Path.Equals(sub.Path, StringComparison.OrdinalIgnoreCase)))
                continue;

            var folder = new LibraryFolder
            {
                Path = sub.Path,
                Label = $"OneDrive/{sub.Name}",
            };

            _db.InsertFolder(folder);
            Folders.Add(folder);
            await ScanFolderAsync(folder);
        }

        StatusMessage = $"Linked {selected.Count} OneDrive folder(s).";
    }

    private async Task ScanFolderAsync(LibraryFolder folder)
    {
        IsScanning = true;
        try
        {
            await _scanner.ScanFolderAsync(folder);
            LoadDocuments();
            LoadFolders();
            StatusMessage = $"Folder scan complete: {folder.Label}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }
}
