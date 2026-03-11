using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Svg.Editor.Avalonia;

public class SvgEditorFileDialogService : ISvgEditorFileDialogService
{
    private static readonly FilePickerFileType AllFiles = new("All files")
    {
        Patterns = new[] { "*.*" },
        MimeTypes = new[] { "*/*" }
    };

    private static readonly FilePickerFileType SvgFiles = new("SVG files")
    {
        Patterns = new[] { "*.svg", "*.svgz" },
        AppleUniformTypeIdentifiers = new[] { "public.svg-image" },
        MimeTypes = new[] { "image/svg+xml" }
    };

    private static readonly FilePickerFileType ImageFiles = new("Images")
    {
        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" },
        AppleUniformTypeIdentifiers = new[] { "public.image" },
        MimeTypes = new[] { "image/*" }
    };

    private static readonly FilePickerFileType PngFiles = new("PNG image")
    {
        Patterns = new[] { "*.png" },
        AppleUniformTypeIdentifiers = new[] { "public.png" },
        MimeTypes = new[] { "image/png" }
    };

    private static readonly FilePickerFileType PdfFiles = new("PDF document")
    {
        Patterns = new[] { "*.pdf" },
        AppleUniformTypeIdentifiers = new[] { "com.adobe.pdf" },
        MimeTypes = new[] { "application/pdf" }
    };

    private static readonly FilePickerFileType XpsFiles = new("XPS document")
    {
        Patterns = new[] { "*.xps" },
        AppleUniformTypeIdentifiers = new[] { "com.microsoft.xps" },
        MimeTypes = new[] { "application/oxps", "application/vnd.ms-xpsdocument" }
    };

    public Task<string?> OpenSvgDocumentAsync(TopLevel? owner)
        => OpenSingleFileAsync(owner, "Open SVG", new[] { SvgFiles, AllFiles });

    public Task<string?> SaveSvgDocumentAsync(TopLevel? owner, string? currentFile)
        => SaveFileAsync(owner, "Save SVG", new[] { SvgFiles, AllFiles }, "svg", currentFile, "drawing");

    public Task<string?> SaveElementPngAsync(TopLevel? owner, string? currentFile)
        => SaveFileAsync(owner, "Export Element", new[] { PngFiles, AllFiles }, "png", currentFile, "element");

    public Task<string?> SavePdfAsync(TopLevel? owner, string? currentFile)
        => SaveFileAsync(owner, "Export PDF", new[] { PdfFiles, AllFiles }, "pdf", currentFile, "drawing");

    public Task<string?> SaveXpsAsync(TopLevel? owner, string? currentFile)
        => SaveFileAsync(owner, "Export XPS", new[] { XpsFiles, AllFiles }, "xps", currentFile, "drawing");

    public Task<string?> OpenImageAsync(TopLevel? owner)
        => OpenSingleFileAsync(owner, "Place Image", new[] { ImageFiles, AllFiles });

    private static async Task<string?> OpenSingleFileAsync(TopLevel? owner, string title, IReadOnlyList<FilePickerFileType> fileTypes)
    {
        var storageProvider = owner?.StorageProvider;
        if (storageProvider is null)
            return null;

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            FileTypeFilter = fileTypes,
            AllowMultiple = false
        });

        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    private static async Task<string?> SaveFileAsync(
        TopLevel? owner,
        string title,
        IReadOnlyList<FilePickerFileType> fileTypes,
        string defaultExtension,
        string? currentFile,
        string fallbackName)
    {
        var storageProvider = owner?.StorageProvider;
        if (storageProvider is null)
            return null;

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            FileTypeChoices = fileTypes,
            DefaultExtension = defaultExtension,
            SuggestedFileName = GetSuggestedFileName(currentFile, fallbackName),
            ShowOverwritePrompt = true
        });

        return file?.TryGetLocalPath();
    }

    private static string GetSuggestedFileName(string? currentFile, string fallbackName)
    {
        if (string.IsNullOrWhiteSpace(currentFile))
            return fallbackName;

        var name = Path.GetFileNameWithoutExtension(currentFile);
        return string.IsNullOrWhiteSpace(name) ? fallbackName : name;
    }
}
