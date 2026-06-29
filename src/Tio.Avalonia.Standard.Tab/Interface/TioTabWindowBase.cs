using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using Tio.Avalonia.Standard.Modules.Extensions;
using Tio.Avalonia.Standard.Tab.Entries;
using TioUi.Controls;

namespace Tio.Avalonia.Standard.Tab.Interface;

public class TioTabWindowBase : TioWindow, ITioTabWindow, INotifyPropertyChanged
{
    public TioTabWindowBase()
    {
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
}