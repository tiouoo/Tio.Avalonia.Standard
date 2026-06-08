using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Tio.Avalonia.Standard.Tab.Interface;

namespace Tio.Avalonia.Standard.Tab.Entries;

public partial class TabEntry : ObservableObject
{
    [ObservableProperty] private string _title;
    [ObservableProperty] private StreamGeometry _icon;
    [ObservableProperty] private object _header;
    [ObservableProperty] private ITioTabPage _content;
    [ObservableProperty] private int _minWidth;
    [ObservableProperty] private bool _isCloseable;
    [ObservableProperty] private bool _isIconVisible;

    public TabEntry(ITioTabPage content, object? header = null, string? title = null, StreamGeometry? icon = null,
        bool? isCloseable = true, bool? isIconVisible = true)
    {
        Title = title ?? content.PageInfo.Title;
        Icon = icon ?? content.PageInfo.Icon;
        Header = header ?? content.PageInfo.Header ?? Title;
        Content = content;
        IsCloseable = isCloseable ?? content.PageInfo.IsCloseable;
        IsIconVisible = isIconVisible ?? content.PageInfo.IsIconVisible;
        content.HostTab = this;
    }

    public void Close()
    {
        if (!IsCloseable) return;
        var toplevel = TopLevel.GetTopLevel(Content as UserControl);
        if (toplevel is not TioTabWindowBase window) return;
        var selected = window.SelectedTab == this;
        window.RemoveTab(this);
        Content.OnClose();
        if (!selected) return;
        var tabs = window.Tabs;
        if (tabs.Count == 0 && window.CreateLastTab != null)
        {
            window.CreateTab(window.CreateLastTab());
        }

        if (tabs.Count > 0)
            window.SelectTab(tabs.Last());
    }
}