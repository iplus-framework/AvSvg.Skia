using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Svg.Editor.Avalonia;
using Svg.Editor.Skia.Avalonia;
using Xunit;

namespace Svg.Editor.Skia.Avalonia.UnitTests;

public class SvgEditorWorkspaceTests
{
    [AvaloniaFact]
    public void SvgEditorSurface_CanBeConstructed()
    {
        var surface = new SvgEditorSurface();

        Assert.NotNull(surface);
    }

    [AvaloniaFact]
    public void SvgEditorWorkspace_LoadDocument_PopulatesSession()
    {
        const string svg = "<svg width=\"24\" height=\"24\"><rect id=\"rect1\" x=\"1\" y=\"1\" width=\"10\" height=\"10\" fill=\"red\" /></svg>";
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.svg");
        File.WriteAllText(path, svg);

        try
        {
            var workspace = new SvgEditorWorkspace();
            var host = new Window
            {
                Width = 1024,
                Height = 768,
                Content = workspace
            };

            host.Show();
            workspace.LoadDocument(path);

            Assert.NotNull(workspace.Document);
            Assert.NotNull(workspace.Session.Document);
            Assert.Equal(path, workspace.CurrentFile);
            Assert.NotEmpty(workspace.Session.Nodes);
            Assert.Contains(Path.GetFileName(path), workspace.WorkspaceTitle, StringComparison.Ordinal);

            host.Close();
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [AvaloniaFact]
    public async Task SvgEditorWorkspace_OpenDocumentAsync_UsesInjectedFileDialogService()
    {
        const string svg = "<svg width=\"16\" height=\"16\"><circle id=\"circle1\" cx=\"8\" cy=\"8\" r=\"4\" fill=\"red\" /></svg>";
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.svg");
        File.WriteAllText(path, svg);

        try
        {
            var workspace = new SvgEditorWorkspace
            {
                FileDialogService = new FakeFileDialogService { OpenSvgDocumentPath = path }
            };

            var host = new Window
            {
                Width = 1024,
                Height = 768,
                Content = workspace
            };

            host.Show();
            await workspace.OpenDocumentAsync();

            Assert.NotNull(workspace.Document);
            Assert.Equal(path, workspace.CurrentFile);
            Assert.NotEmpty(workspace.Session.Nodes);

            host.Close();
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private sealed class FakeFileDialogService : ISvgEditorFileDialogService
    {
        public string? OpenSvgDocumentPath { get; init; }

        public Task<string?> OpenSvgDocumentAsync(TopLevel? owner)
            => Task.FromResult(OpenSvgDocumentPath);

        public Task<string?> SaveSvgDocumentAsync(TopLevel? owner, string? currentFile)
            => Task.FromResult<string?>(null);

        public Task<string?> SaveElementPngAsync(TopLevel? owner, string? currentFile)
            => Task.FromResult<string?>(null);

        public Task<string?> SavePdfAsync(TopLevel? owner, string? currentFile)
            => Task.FromResult<string?>(null);

        public Task<string?> SaveXpsAsync(TopLevel? owner, string? currentFile)
            => Task.FromResult<string?>(null);

        public Task<string?> OpenImageAsync(TopLevel? owner)
            => Task.FromResult<string?>(null);
    }
}
