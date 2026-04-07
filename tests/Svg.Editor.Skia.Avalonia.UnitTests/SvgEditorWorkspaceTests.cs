using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Svg;
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
    public void SvgEditorWorkspace_LoadDocument_ClearsPreviousSelectionState()
    {
        const string firstSvg = "<svg width=\"24\" height=\"24\"><rect id=\"rect1\" x=\"1\" y=\"1\" width=\"10\" height=\"10\" fill=\"red\" /></svg>";
        const string secondSvg = "<svg width=\"24\" height=\"24\"><circle id=\"circle1\" cx=\"8\" cy=\"8\" r=\"4\" fill=\"blue\" /></svg>";
        var firstPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-first.svg");
        var secondPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-second.svg");
        File.WriteAllText(firstPath, firstSvg);
        File.WriteAllText(secondPath, secondSvg);

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
            workspace.LoadDocument(firstPath);

            var previousElement = Assert.IsType<SvgRectangle>(workspace.Document!.Children.OfType<SvgRectangle>().Single());
            SetPrivateField(workspace, "_selectedSvgElement", previousElement);
            SetPrivateField(workspace, "_selectedElement", previousElement);
            var multiSelected = GetPrivateField<IList>(workspace, "_multiSelected");
            multiSelected.Add(previousElement);
            workspace.Session.SetSelectedElementIds(new[] { previousElement.ID });

            workspace.LoadDocument(secondPath);

            Assert.Null(GetPrivateField<SvgElement?>(workspace, "_selectedSvgElement"));
            Assert.Null(GetPrivateField<SvgVisualElement?>(workspace, "_selectedElement"));
            Assert.Empty(GetPrivateField<IList>(workspace, "_multiSelected").Cast<object>());
            Assert.Empty(workspace.Session.SelectedElementIds);
            Assert.NotNull(workspace.Document);
            Assert.Contains(workspace.Document!.Children.OfType<SvgCircle>(), element => element.ID == "circle1");

            host.Close();
        }
        finally
        {
            if (File.Exists(firstPath))
                File.Delete(firstPath);

            if (File.Exists(secondPath))
                File.Delete(secondPath);
        }
    }

    [AvaloniaFact]
    public void SvgEditorWorkspace_NewMenuItem_PreservesUndoHistory()
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

            InvokePrivateMenuHandler(workspace, "NewMenuItem_Click");

            Assert.True(workspace.Session.UndoCount > 0);

            InvokePrivateMenuHandler(workspace, "UndoMenuItem_Click");

            Assert.NotNull(workspace.Document);
            Assert.Contains(workspace.Document!.Children.OfType<SvgRectangle>(), element => element.ID == "rect1");

            host.Close();
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void SvgEditorWorkspace_LoadDocument_ResolvesAvaresResourceFromLoadedAssembly()
    {
        var workspace = new SvgEditorWorkspace();
        var host = new Window
        {
            Width = 1024,
            Height = 768,
            Content = workspace
        };

        host.Show();
        workspace.LoadDocument("Assets/embedded-test.svg");

        Assert.NotNull(workspace.Document);
        Assert.Equal("Assets/embedded-test.svg", workspace.CurrentFile);
        Assert.NotEmpty(workspace.Session.Nodes);

        host.Close();
    }

    [AvaloniaFact]
    public void SvgEditorWorkspace_UsesHostProvidedTitlePrefix()
    {
        const string svg = "<svg width=\"24\" height=\"24\"><rect id=\"rect1\" x=\"1\" y=\"1\" width=\"10\" height=\"10\" fill=\"red\" /></svg>";
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.svg");
        File.WriteAllText(path, svg);

        try
        {
            var workspace = new SvgEditorWorkspace
            {
                WorkspaceTitlePrefix = "HostApp"
            };

            var host = new Window
            {
                Width = 1024,
                Height = 768,
                Content = workspace
            };

            host.Show();
            workspace.LoadDocument(path);

            Assert.Equal($"HostApp - {Path.GetFileName(path)}", workspace.WorkspaceTitle);

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

    private static void InvokePrivateMenuHandler(SvgEditorWorkspace workspace, string methodName)
    {
        var method = typeof(SvgEditorWorkspace).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(workspace, new object?[] { null, new RoutedEventArgs() });
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var value = field!.GetValue(instance);
        return value is null ? default! : (T)value;
    }

    private static void SetPrivateField<T>(object instance, string fieldName, T value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }
}
