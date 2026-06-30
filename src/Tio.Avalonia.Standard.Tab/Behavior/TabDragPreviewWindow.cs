using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Tio.Avalonia.Standard.Tab.Entries;

namespace Tio.Avalonia.Standard.Tab.Behavior;

public partial class TabDragPreviewWindow : Window
{
    public TabDragPreviewWindow()
    {
        InitializeComponent();
        DragStateIcon.Data = Geometry.Parse(TabDragIcons.NoOperation);
    }

    public void UpdateContent(TabEntry tab)
    {
        Header.Content = tab.Header;
        
        if (tab.Icon != null && tab.IsIconVisible)
        {
            IconPath.Data = tab.Icon;
            IconPath.IsVisible = true;
        }
        else
        {
            IconPath.IsVisible = false;
        }

        Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Arrange(new Rect(DesiredSize));
    }

    public void UpdateDragState(TabDragState state)
    {
        DragStateIcon.Data = state switch
        {
            TabDragState.ReorderInCurrentWindow => Geometry.Parse(TabDragIcons.ReorderInCurrentWindow),
            TabDragState.TransferToAnotherWindow => Geometry.Parse(TabDragIcons.TransferToAnotherWindow),
            TabDragState.DetachToNewWindow => Geometry.Parse(TabDragIcons.DetachToNewWindow),
            _ => Geometry.Parse(TabDragIcons.NoOperation)
        };
    }

    public void MoveTo(PixelPoint screenPosition)
    {
        Position = new PixelPoint(
            screenPosition.X + 10,
            screenPosition.Y + 10
        );
    }
}