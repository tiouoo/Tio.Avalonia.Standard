using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Tio.Avalonia.Standard.Tab.Entries;

namespace Tio.Avalonia.Standard.Tab.Behavior;

public partial class TabDragPreviewWindow : Window
{
    public TabDragPreviewWindow()
    {
        InitializeComponent();
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

    public void MoveTo(PixelPoint screenPosition)
    {
        Position = new PixelPoint(
            screenPosition.X + 10,
            screenPosition.Y + 10
        );
    }
}