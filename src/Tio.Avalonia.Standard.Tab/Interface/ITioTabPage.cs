using Tio.Avalonia.Standard.Tab.Entries;

namespace Tio.Avalonia.Standard.Tab.Interface;

public interface ITioTabPage
{
    public PageInfo PageInfo { get; init; }
    public TabEntry HostTab { get; set; }

    public void OnClose()
    {
    }
}