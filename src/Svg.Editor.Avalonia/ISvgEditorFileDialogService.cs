using System.Threading.Tasks;
using Avalonia.Controls;

namespace Svg.Editor.Avalonia;

public interface ISvgEditorFileDialogService
{
    Task<string?> OpenSvgDocumentAsync(TopLevel? owner);
    Task<string?> SaveSvgDocumentAsync(TopLevel? owner, string? currentFile);
    Task<string?> SaveElementPngAsync(TopLevel? owner, string? currentFile);
    Task<string?> SavePdfAsync(TopLevel? owner, string? currentFile);
    Task<string?> SaveXpsAsync(TopLevel? owner, string? currentFile);
    Task<string?> OpenImageAsync(TopLevel? owner);
}
