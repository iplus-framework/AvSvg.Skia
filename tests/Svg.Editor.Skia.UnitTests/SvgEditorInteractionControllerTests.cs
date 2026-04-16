using Svg;
using Svg.Editor.Skia;
using Xunit;

namespace Svg.Editor.Skia.UnitTests;

public class SvgEditorInteractionControllerTests
{
    [Fact]
    public void Controller_DelegatesSelectionSettingsAndTransforms()
    {
        var selectionService = new SelectionService();
        var pathService = new PathService();
        var controller = new SvgEditorInteractionController(selectionService, pathService);
        var rectangle = new SvgRectangle();

        controller.SnapToGrid = true;
        controller.GridSize = 10;
        controller.CurrentSegmentTool = PathService.SegmentTool.Cubic;
        controller.SetTranslation(rectangle, 3, 17);

        Assert.True(selectionService.SnapToGrid);
        Assert.Equal(10, selectionService.GridSize);
        Assert.Equal(PathService.SegmentTool.Cubic, pathService.CurrentSegmentTool);
        Assert.Equal((0f, 20f), controller.GetTranslation(rectangle));
    }
}
