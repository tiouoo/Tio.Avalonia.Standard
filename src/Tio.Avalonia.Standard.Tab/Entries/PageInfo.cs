using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Tio.Avalonia.Standard.Tab.Entries;

public partial class PageInfo : ObservableObject
{
    [ObservableProperty] private string _title;
    [ObservableProperty] private StreamGeometry _icon;
    [ObservableProperty] private bool _isCloseable = true;
    [ObservableProperty] private bool _isIconVisible = true;
    [ObservableProperty] private object? _header;
}