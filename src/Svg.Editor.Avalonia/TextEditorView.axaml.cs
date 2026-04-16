using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Svg;
using Svg.Editor.Avalonia.Models;

namespace Svg.Editor.Avalonia;

public partial class TextEditorView : SvgEditorDialogViewBase<TextEditorResult>
{
    private readonly TextBox _editor;
    private readonly ComboBox _fontFamilyBox;
    private readonly ComboBox _fontWeightBox;
    private readonly TextBox _letterBox;
    private readonly TextBox _wordBox;

    public TextEditorView()
    {
        InitializeComponent();
        _editor = this.FindControl<TextBox>("Editor")!;
        _fontFamilyBox = this.FindControl<ComboBox>("FontFamilyBox")!;
        _fontWeightBox = this.FindControl<ComboBox>("FontWeightBox")!;
        _letterBox = this.FindControl<TextBox>("LetterBox")!;
        _wordBox = this.FindControl<TextBox>("WordBox")!;
        _fontWeightBox.ItemsSource = Enum.GetValues(typeof(FontWeight)).Cast<FontWeight>();
    }

    public void Initialize(string text, string fontFamily, SvgFontWeight weight, float letterSpacing, float wordSpacing)
    {
        _editor.Text = text;
        var fonts = FontManager.Current?.SystemFonts?
            .Select(font => font.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .OrderBy(name => name)
            .ToList() ?? new List<string>();

        if (!string.IsNullOrWhiteSpace(fontFamily) && !fonts.Contains(fontFamily))
            fonts.Insert(0, fontFamily);

        _fontFamilyBox.ItemsSource = fonts;
        _fontFamilyBox.SelectedItem = string.IsNullOrWhiteSpace(fontFamily) ? fonts.FirstOrDefault() : fontFamily;
        _fontWeightBox.SelectedItem = ToFontWeight(weight);
        _letterBox.Text = letterSpacing.ToString();
        _wordBox.Text = wordSpacing.ToString();
    }

    private static FontWeight ToFontWeight(SvgFontWeight weight) => weight switch
    {
        SvgFontWeight.W100 => FontWeight.Thin,
        SvgFontWeight.W200 => FontWeight.ExtraLight,
        SvgFontWeight.W300 => FontWeight.Light,
        SvgFontWeight.W400 => FontWeight.Normal,
        SvgFontWeight.W500 => FontWeight.Medium,
        SvgFontWeight.W600 => FontWeight.SemiBold,
        SvgFontWeight.W700 => FontWeight.Bold,
        SvgFontWeight.W800 => FontWeight.ExtraBold,
        SvgFontWeight.W900 => FontWeight.Black,
        SvgFontWeight.Bold => FontWeight.Bold,
        _ => FontWeight.Normal
    };

    private static SvgFontWeight FromFontWeight(FontWeight weight) => weight switch
    {
        FontWeight.Thin => SvgFontWeight.W100,
        FontWeight.ExtraLight => SvgFontWeight.W200,
        FontWeight.Light => SvgFontWeight.W300,
        FontWeight.Normal => SvgFontWeight.W400,
        FontWeight.Medium => SvgFontWeight.W500,
        FontWeight.SemiBold => SvgFontWeight.W600,
        FontWeight.Bold => SvgFontWeight.W700,
        FontWeight.ExtraBold => SvgFontWeight.W800,
        FontWeight.Black => SvgFontWeight.W900,
        _ => SvgFontWeight.Normal
    };

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        float.TryParse(_letterBox.Text, out var letterSpacing);
        float.TryParse(_wordBox.Text, out var wordSpacing);
        Accept(new TextEditorResult(
            _editor.Text ?? string.Empty,
            _fontFamilyBox.SelectedItem as string ?? string.Empty,
            FromFontWeight((FontWeight)_fontWeightBox.SelectedItem!),
            letterSpacing,
            wordSpacing));
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Cancel();
    }
}
