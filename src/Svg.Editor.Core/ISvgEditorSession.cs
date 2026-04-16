using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Svg;

namespace Svg.Editor.Core;

public interface ISvgEditorSession : INotifyPropertyChanged
{
    SvgDocument? Document { get; }
    string? CurrentFile { get; }
    string WorkspaceTitle { get; }
    string FilterText { get; }
    string PropertyFilterText { get; }
    string? SelectedElementId { get; }
    ObservableCollection<string> SelectedElementIds { get; }
    ObservableCollection<SvgNode> Nodes { get; }
    ObservableCollection<ArtboardInfo> Artboards { get; }
    ISet<string> ExpandedNodeIds { get; }
    SvgEditorSettings Settings { get; }
    ClipboardSnapshot Clipboard { get; }
    SvgEditorToolKind CurrentTool { get; }
    int UndoCount { get; }
    int RedoCount { get; }
}
