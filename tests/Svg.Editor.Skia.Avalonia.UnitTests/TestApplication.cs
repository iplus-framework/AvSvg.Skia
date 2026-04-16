using Avalonia;
using Avalonia.Headless;

[assembly: Avalonia.Headless.AvaloniaTestApplication(typeof(Svg.Editor.Skia.Avalonia.UnitTests.SvgEditorSkiaAvaloniaTestsAppBuilder))]

namespace Svg.Editor.Skia.Avalonia.UnitTests;

internal static class SvgEditorSkiaAvaloniaTestsAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApplication>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .LogToTrace();
}

internal sealed class TestApplication : Application
{
}
