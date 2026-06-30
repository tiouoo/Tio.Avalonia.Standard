using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tio.Avalonia.Standard.Tab.Common;
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
    [ObservableProperty] private bool _isDragging;
    public TioTabWindowBase Window { get; set; }

    public TabEntry(TioTabWindowBase window, ITioTabPage content, object? header = null, string? title = null,
        StreamGeometry? icon = null, bool? isCloseable = true, bool? isIconVisible = true)
    {
        Window = window;
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
        var selected = Window.SelectedTab == this;
        Window.RemoveTab(this);
        Content.OnClose();
        if (!selected) return;
        var tabs = Window.Tabs;
        if (tabs.Count == 0 && Window.CreateLastTabFunc != null)
        {
            Window.CreateLastTabFunc();
        }

        if (tabs.Count > 0)
            Window.SelectTab(tabs.Last());
    }

    public void CloseOther()
    {
        foreach (var tab in Window.Tabs.ToList())
        {
            if (tab == this) continue;
            if (!tab.IsCloseable) continue;
            Window.RemoveTab(tab);
            tab.Content.OnClose();
        }
    }

    public void MoveTabToNewWindow(PixelPoint? screenPosition = null)
    {
        var oldWindow = Window;
        if (oldWindow == null)
            return;

        var oldSelected = oldWindow.SelectedTab == this;

        if (Content is Control contentControl)
        {
            DetachControl(contentControl);
        }

        Window.RemoveTab(this);

        if (oldSelected && oldWindow.Tabs.Count > 0)
        {
            oldWindow.SelectTab(oldWindow.Tabs.Last());
        }

        var window = Functions.CreateNewTabWindowFunc(screenPosition ?? new PixelPoint(100, 100));
        
        if (screenPosition.HasValue)
        {
            var offsetX = -window.Width / 2;
            var offsetY = -window.Height / 2;
            window.Position = new PixelPoint(
                screenPosition.Value.X + (int)offsetX,
                screenPosition.Value.Y + (int)offsetY
            );
        }

        window.AddTab(this);
        Window = window;
        window.SelectTab(this);
        window.Show();

        if (oldWindow.Tabs.Count == 0)
            oldWindow.Close();
    }

    public void MoveTabToWindow(TioTabWindowBase targetWindow)
    {
        if (targetWindow == null || !TioTabWindowBase.AllWindows.Contains(targetWindow))
            return;

        var oldWindow = Window;
        if (oldWindow == null)
            return;

        var oldSelected = oldWindow.SelectedTab == this;

        if (Content is Control contentControl)
        {
            DetachControl(contentControl);
        }

        Window.RemoveTab(this);

        if (oldSelected && oldWindow.Tabs.Count > 0)
        {
            oldWindow.SelectTab(oldWindow.Tabs.Last());
        }

        targetWindow.AddTab(this);
        Window = targetWindow;
        targetWindow.SelectTab(this);
        targetWindow.Activate();

        if (oldWindow.Tabs.Count == 0)
            oldWindow.Close();
    }

    private static void DetachControl(Control control)
    {
        if (control.Parent is Panel panel)
        {
            panel.Children.Remove(control);
        }
        else if (control.Parent is ContentControl contentControl)
        {
            contentControl.Content = null;
        }
    }

    public MenuFlyout BuildContextMenu()
    {
        var flyout = new MenuFlyout();

        if (IsCloseable)
        {
            flyout.Items.Add(new MenuItem
            {
                Header = "关闭标签页",
                InputGesture = KeyGesture.Parse("Ctrl+W"),
                Command = new RelayCommand(Close),
                Icon = new PathIcon()
                {
                    Data = Geometry.Parse(
                        "M13.41 12l4.3-4.29a1 1 0 1 0-1.42-1.42L12 10.59l-4.29-4.3a1 1 0 0 0-1.42 1.42l4.3 4.29-4.3 4.29a1 1 0 0 0 0 1.42 1 1 0 0 0 1.42 0l4.29-4.3 4.29 4.3a1 1 0 0 0 1.42 0 1 1 0 0 0 0-1.42z"),
                    Width = 10, Height = 10,
                }
            });
        }

        if (Window.Tabs.Count > 1)
        {
            flyout.Items.Add(new MenuItem
            {
                Header = "关闭其他标签页",
                Command = new RelayCommand(CloseOther)
            });
        }

        var otherWindows = TioTabWindowBase.AllWindows.Where(w => w != Window).ToList();
        if (otherWindows.Any())
        {
            var moveToWindowMenuItem = new MenuItem
            {
                Header = "转移到窗口", Icon = new PathIcon()
                {
                    Data = Geometry.Parse(
                        "F1 M640,640z M0,0z M566.6,342.6C579.1,330.1,579.1,309.8,566.6,297.3L406.6,137.3C394.1,124.8 373.8,124.8 361.3,137.3 348.8,149.8 348.8,170.1 361.3,182.6L466.7,288 96,288C78.3,288 64,302.3 64,320 64,337.7 78.3,352 96,352L466.7,352 361.3,457.4C348.8,469.9 348.8,490.2 361.3,502.7 373.8,515.2 394.1,515.2 406.6,502.7L566.6,342.7z"),
                    Height = 16,
                }
            };
            foreach (var win in otherWindows)
            {
                moveToWindowMenuItem.Items.Add(new MenuItem
                {
                    Header = $"#{win.WindowId}",
                    Command = new RelayCommand(() => MoveTabToWindow(win)),
                    Classes = { "hide-icon" }
                });
            }

            flyout.Items.Add(moveToWindowMenuItem);
        }

        flyout.Items.Add(new MenuItem
        {
            Header = "在新窗口打开",
            Command = new RelayCommand(() => MoveTabToNewWindow())
        });

        if (Content is IContextMenuTabPage contextMenuTab)
        {
            flyout.Items.Add(new Separator());
            var customItems = new List<MenuItem>();
            contextMenuTab.BuildContextMenu(customItems);
            foreach (var item in customItems)
            {
                flyout.Items.Add(item);
            }
        }

        return flyout;
    }
}