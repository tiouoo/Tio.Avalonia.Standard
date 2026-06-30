using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using Tio.Avalonia.Standard.Modules.Extensions;
using Tio.Avalonia.Standard.Tab.Entries;
using TioUi.Controls;

namespace Tio.Avalonia.Standard.Tab.Interface;

public class TioTabWindowBase : TioWindow, ITioTabWindow, INotifyPropertyChanged
{
    private static readonly List<TioTabWindowBase> _allWindows = [];

    public static IReadOnlyList<TioTabWindowBase> AllWindows => _allWindows.AsReadOnly();

    public string WindowId { get; }

    public TioTabWindowBase()
    {
        WindowId = GenerateWindowId();
        _allWindows.Add(this);
        
        KeyBindings.Add(new KeyBinding
        {
            Gesture = KeyGesture.Parse("Ctrl+T"),
            Command = new RelayCommand(CreateNewTab)
        });
        KeyBindings.Add(new KeyBinding
        {
            Gesture = KeyGesture.Parse("Ctrl+W"),
            Command = new RelayCommand(CloseCurrentTab)
        });
        KeyBindings.Add(new KeyBinding
        {
            Gesture = KeyGesture.Parse("Ctrl+Shift+W"),
            Command = new RelayCommand(CloseAllTab)
        });

        Closed += OnClosed;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _allWindows.Remove(this);
    }

    private static string GenerateWindowId()
    {
        var hash = Guid.NewGuid().GetHashCode();
        return (hash & 0xFFFFFF).ToString("x6");
    }

    public void CloseAllTab()
    {
        Tabs.ToList().ForEach(x => x.Close());
    }

    public void CreateNewTab()
    {
        CreateNewTabFunc?.Invoke();
    }

    public void CloseCurrentTab()
    {
        SelectedTab?.Close();
    }

    public Action? CreateNewTabFunc { get; set; }

    public TioNotificationManager Notification { get; set; }
    public TioToastManager Toast { get; set; }
    public TioWindow Window { get; set; }
    public bool IsMainWindow { get; init; } 
    public ObservableCollection<TabEntry> Tabs { get; } = [];

    public Action? CreateLastTabFunc
    {
        get => field ?? CreateNewTabFunc;
        set;
    }

    public TabEntry? SelectedTab
    {
        get;
        set => SetField(ref field, value);
    }

    public void SelectTab(TabEntry tab)
    {
        if (!Tabs.Contains(tab)) return;
        SelectedTab = tab;
    }

    public void SelectTabByIndex(int index)
    {
        if (index < 0 || index >= Tabs.Count) return;
        SelectedTab = Tabs[index];
    }

    public void RemoveTab(TabEntry tab)
    {
        Tabs.Remove(tab);
    }

    public void CreateTab(TabEntry tab)
    {
        Tabs.Add(tab);
    }

    public void AddTab(TabEntry tab)
    {
        Tabs.Add(tab);
    }

    public void ReorderTab(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= Tabs.Count || newIndex < 0 || newIndex >= Tabs.Count)
            return;

        if (oldIndex == newIndex)
            return;

        var tab = Tabs[oldIndex];
        Tabs.RemoveAt(oldIndex);
        Tabs.Insert(newIndex, tab);
        SelectTab(tab);
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public void MoveWindow(PixelPoint delta)
    {
        Position = new PixelPoint(Position.X + delta.X, Position.Y + delta.Y);
    }
}