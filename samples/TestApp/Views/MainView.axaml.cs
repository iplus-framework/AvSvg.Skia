using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ShimSkiaSharp;
using SkiaSharp;
using Svg.Skia;
using TestApp.ViewModels;

namespace TestApp.Views;

public partial class MainView : UserControl
{
    private readonly ObservableCollection<string> _hitResults = new();
    private SKSvg? _currentSkSvg;
    private bool _showHitBounds;
    private SkiaSharp.SKColor _hitBoundsColor = SKColors.Cyan;
    private readonly IList<ShimSkiaSharp.SKPoint> _hitTestPoints = new List<ShimSkiaSharp.SKPoint>();
    private readonly IList<ShimSkiaSharp.SKRect> _hitTestRects = new List<ShimSkiaSharp.SKRect>();
    private readonly DispatcherTimer _animationUiTimer;
    private double _resumeAnimationPlaybackRate = 1.0;

    public MainView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, Drop);
        AddHandler(DragDrop.DragOverEvent, DragOver);
        HitResults.ItemsSource = _hitResults;
        _animationUiTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background, OnAnimationUiTick);
        _animationUiTimer.Start();
        SubscribeOnDraw();
        UpdateAnimationUi();
    }

    private void SubscribeOnDraw()
    {
        if (_currentSkSvg is { })
        {
            _currentSkSvg.OnDraw -= SkSvg_OnDraw;
        }

        _currentSkSvg = Svg.SkSvg;

        if (_currentSkSvg is { })
        {
            _currentSkSvg.OnDraw += SkSvg_OnDraw;
        }

        AutoStartAnimationIfNeeded();
        UpdateAnimationUi();
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DragEffects & (DragDropEffects.Copy | DragDropEffects.Link);

        if (!e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var paths = e.Data.GetFileNames();
            if (paths is { })
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    try
                    {
                        vm.Drop(paths);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }
        }
    }

    private void FileItem_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is FileItemViewModel fileItemViewModel)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("explorer", fileItemViewModel.Path);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", fileItemViewModel.Path);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", fileItemViewModel.Path);
            }
        }
    }

    private void ShowHitBoundsToggle_OnToggled(object? sender, RoutedEventArgs e)
    {
        _showHitBounds = ShowHitBoundsToggle.IsChecked == true;
        SubscribeOnDraw();
        Svg.InvalidateVisual();
    }

    private void Svg_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pt = e.GetPosition(Svg);

        _hitResults.Clear();

        if (Svg.SkSvg is { })
        {
            _hitTestPoints.Clear();

            if (Svg.TryGetPicturePoint(pt, out var skPoint))
            {
                _hitTestPoints.Add(skPoint);

                // foreach (var element in Svg.HitTestElements(pt))
                // {
                //     _hitResults.Add(element.ID);
                // }
                var element = Svg.HitTestElements(pt).FirstOrDefault();
                if (element is { })
                {
                    _hitResults.Add(element.ID ?? element.GetType().Name);
                }
            }
        }

        SubscribeOnDraw();
        Svg.InvalidateVisual();
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _hitResults.Clear();

        if (Svg.SkSvg is { })
        {
            _hitTestPoints.Clear();
            _showHitBounds = ShowHitBoundsToggle.IsChecked == true;
            SubscribeOnDraw();
        }

        Svg.InvalidateVisual();
        UpdateAnimationUi();
    }

    private void PlayAnimationButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Svg.SkSvg?.HasAnimations != true)
        {
            UpdateAnimationUi();
            return;
        }

        if (Svg.AnimationPlaybackRate <= 0)
        {
            var playbackRate = _resumeAnimationPlaybackRate > 0 ? _resumeAnimationPlaybackRate : 1.0;
            Svg.AnimationPlaybackRate = playbackRate;
        }

        UpdateAnimationUi();
    }

    private void PauseAnimationButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Svg.AnimationPlaybackRate > 0)
        {
            _resumeAnimationPlaybackRate = Svg.AnimationPlaybackRate;
        }

        Svg.AnimationPlaybackRate = 0;
        UpdateAnimationUi();
    }

    private void RestartAnimationButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Svg.SkSvg?.ResetAnimation();
        AutoStartAnimationIfNeeded();
        Svg.InvalidateVisual();
        UpdateAnimationUi();
    }

    private void OnAnimationUiTick(object? sender, EventArgs e)
    {
        if (!ReferenceEquals(_currentSkSvg, Svg.SkSvg))
        {
            SubscribeOnDraw();
        }

        if (Svg.AnimationPlaybackRate > 0)
        {
            _resumeAnimationPlaybackRate = Svg.AnimationPlaybackRate;
        }

        UpdateAnimationUi();
    }

    private void AutoStartAnimationIfNeeded()
    {
        if (Svg.SkSvg?.HasAnimations != true || Svg.AnimationPlaybackRate > 0)
        {
            return;
        }

        Svg.AnimationPlaybackRate = _resumeAnimationPlaybackRate > 0
            ? _resumeAnimationPlaybackRate
            : 1.0;
    }

    private void UpdateAnimationUi()
    {
        var skSvg = Svg.SkSvg;
        var hasAnimations = skSvg?.HasAnimations == true;
        var animationTime = skSvg?.AnimationTime ?? TimeSpan.Zero;
        var isPaused = Svg.AnimationPlaybackRate <= 0;

        PlayAnimationButton.IsEnabled = hasAnimations && isPaused;
        PauseAnimationButton.IsEnabled = hasAnimations && !isPaused;
        RestartAnimationButton.IsEnabled = hasAnimations;

        AnimationStatusText.Text = !hasAnimations
            ? "No animation"
            : isPaused
                ? "Paused"
                : "Playing";

        AnimationClockText.Text = animationTime.ToString(@"mm\:ss\.fff");
        AnimationBackendInfoText.Text = Svg.ActualAnimationBackend.ToString();
        ToolTip.SetTip(AnimationBackendInfoText, Svg.AnimationBackendFallbackReason);
    }

    private void SkSvg_OnDraw(object? sender, SKSvgDrawEventArgs e)
    {
        if (sender is not SKSvg skSvg)
        {
            return;
        }

        if (!_showHitBounds)
        {
            return;
        }

        var hits = new HashSet<SvgSceneNode>();

        foreach (var pt in _hitTestPoints)
        {
            foreach (var node in skSvg.HitTestSceneNodes(pt))
            {
                hits.Add(node);
            }
        }

        foreach (var r in _hitTestRects)
        {
            foreach (var node in skSvg.HitTestSceneNodes(r))
            {
                hits.Add(node);
            }
        }

        using var paint = new SkiaSharp.SKPaint();
        paint.IsAntialias = true;
        paint.Style = SkiaSharp.SKPaintStyle.Stroke;
        paint.Color = _hitBoundsColor;

        foreach (var hit in hits.Take(1))
        {
            var rect = skSvg.SkiaModel.ToSKRect(hit.TransformedBounds);
            e.Canvas.DrawRect(rect, paint);
        }
    }
}
