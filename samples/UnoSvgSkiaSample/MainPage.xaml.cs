using System.Linq;
using Svg;

namespace UnoSvgSkiaSample;

public sealed partial class MainPage : Page
{
    private bool _useWarmTheme;

    public MainPage()
    {
        InitializeComponent();
        StyledSvg.CurrentCss = ColdCss;
    }

    public string InlineSvg =>
        """
        <svg width="260" height="120" viewBox="0 0 260 120" xmlns="http://www.w3.org/2000/svg">
          <rect x="8" y="8" width="244" height="104" rx="24" fill="#0f172a" />
          <circle cx="56" cy="60" r="24" fill="#38bdf8" />
          <path d="M96 82 L130 28 L164 82 Z" fill="#f59e0b" />
          <rect x="182" y="32" width="42" height="56" rx="14" fill="#34d399" />
        </svg>
        """;

    public string StyledSvgMarkup =>
        """
        <svg width="220" height="180" viewBox="0 0 220 180" xmlns="http://www.w3.org/2000/svg">
          <rect x="12" y="12" width="196" height="156" rx="26" fill="#f8fafc" />
          <circle class="accent" cx="72" cy="88" r="34" />
          <path class="outline" d="M120 128 L162 48 L186 128 Z" fill="none" />
          <rect class="accent" x="122" y="62" width="44" height="18" rx="9" />
        </svg>
        """;

    public string FilterSvgMarkup =>
        """
        <svg width="260" height="160" viewBox="0 0 260 160" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <filter id="blurred-glow" x="-20%" y="-20%" width="140%" height="140%">
              <feGaussianBlur stdDeviation="6" />
            </filter>
          </defs>
          <rect x="12" y="12" width="236" height="136" rx="24" fill="#111827" />
          <circle cx="86" cy="82" r="30" fill="#22c55e" filter="url(#blurred-glow)" />
          <circle cx="86" cy="82" r="24" fill="#4ade80" />
          <rect x="138" y="54" width="66" height="56" rx="18" fill="#f97316" filter="url(#blurred-glow)" />
          <rect x="146" y="62" width="50" height="40" rx="14" fill="#fb923c" />
        </svg>
        """;

    private const string ColdCss = ".accent { fill: #2563eb; } .outline { stroke: #0f172a; stroke-width: 4; }";
    private const string WarmCss = ".accent { fill: #ef4444; } .outline { stroke: #7c2d12; stroke-width: 4; }";

    private void OnSwapThemeClick(object sender, RoutedEventArgs e)
    {
        _useWarmTheme = !_useWarmTheme;
        StyledSvg.CurrentCss = _useWarmTheme ? WarmCss : ColdCss;
        CssStatusText.Text = $"Current theme: {(_useWarmTheme ? "ember" : "cobalt")}";
    }

    private void OnZoomInClick(object sender, RoutedEventArgs e)
    {
        InteractiveSvg.ZoomToPoint(InteractiveSvg.Zoom * 1.2, new Windows.Foundation.Point(200, 130));
    }

    private void OnZoomOutClick(object sender, RoutedEventArgs e)
    {
        InteractiveSvg.ZoomToPoint(InteractiveSvg.Zoom / 1.2, new Windows.Foundation.Point(200, 130));
    }

    private void OnPanLeftClick(object sender, RoutedEventArgs e) => InteractiveSvg.PanX -= 20;

    private void OnPanRightClick(object sender, RoutedEventArgs e) => InteractiveSvg.PanX += 20;

    private void OnPanUpClick(object sender, RoutedEventArgs e) => InteractiveSvg.PanY -= 20;

    private void OnPanDownClick(object sender, RoutedEventArgs e) => InteractiveSvg.PanY += 20;

    private void OnResetViewClick(object sender, RoutedEventArgs e)
    {
        InteractiveSvg.Zoom = 1.0;
        InteractiveSvg.PanX = 0.0;
        InteractiveSvg.PanY = 0.0;
        HitTestStatusText.Text = "Tap the camera SVG to inspect hit-tested elements.";
    }

    private void OnInteractiveSvgPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(InteractiveSvg).Position;
        var hits = InteractiveSvg.HitTestElements(point).ToArray();

        if (hits.Length == 0)
        {
            HitTestStatusText.Text = $"No SVG elements hit at ({point.X:F0}, {point.Y:F0}).";
            return;
        }

        var labels = hits
            .Select(static element => !string.IsNullOrWhiteSpace(element.ID) ? $"#{element.ID}" : element.GetType().Name)
            .Distinct(StringComparer.Ordinal)
            .Take(3);

        HitTestStatusText.Text = $"Hit {hits.Length} element(s): {string.Join(", ", labels)}";
    }

    private void OnWireframeChanged(object sender, RoutedEventArgs e)
    {
        FilterSvg.Wireframe = sender is CheckBox { IsChecked: true };
    }

    private void OnDisableFiltersChanged(object sender, RoutedEventArgs e)
    {
        FilterSvg.DisableFilters = sender is CheckBox { IsChecked: true };
    }
}
