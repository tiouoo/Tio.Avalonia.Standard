using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Tio.Avalonia.Standard.Tab.Entries;
using Tio.Avalonia.Standard.Tab.Interface;

namespace Tio.Avalonia.Standard.Tab.Behavior;

public enum TabDragState
{
    NoOperation,
    ReorderInCurrentWindow,
    TransferToAnotherWindow,
    DetachToNewWindow
}

/// <summary>
/// 为标签页提供拖拽重新排序、转移到新窗口、转移到其他窗口功能的行为类
/// </summary>
public class TabDragDropBehavior
{
    private Control? _draggedElement;
    private TabEntry? _draggedTab;
    private Point _dragStartPoint;
    private bool _isDragging;
    private Control? _itemsContainer;
    private TioTabWindowBase? _window;
    private TioTabWindowBase? _targetWindow;
    private const double DragThreshold = 5.0;
    private TabDragState _dragState = TabDragState.NoOperation;

    public TabDragState DragState
    {
        get => _dragState;
        private set
        {
            if (_dragState != value)
            {
                _dragState = value;
            }
        }
    }

    /// <summary>
    /// 附加到标签页容器以启用拖拽功能
    /// </summary>
    /// <param name="container">标签页容器控件</param>
    /// <param name="window">标签页窗口</param>
    public void Attach(Control container, TioTabWindowBase window)
    {
        _itemsContainer = container;
        _window = window;

        container.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, handledEventsToo: true);
        container.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, handledEventsToo: true);
        container.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, handledEventsToo: true);
        container.AddHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost, handledEventsToo: true);
    }

    /// <summary>
    /// 从标签页容器分离拖拽功能
    /// </summary>
    /// <param name="container">标签页容器控件</param>
    public void Detach(Control container)
    {
        container.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        container.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
        container.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
        container.RemoveHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost);

        _itemsContainer = null;
        _window = null;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(sender as Control);
        
        if (!point.Properties.IsLeftButtonPressed)
            return;

        var element = e.Source as Control;
        var tabBorder = FindTabBorder(element);
        
        if (tabBorder == null)
            return;

        var tabEntry = tabBorder.Tag as TabEntry;
        if (tabEntry == null)
            return;

        if (IsClickOnCloseButton(element))
            return;

        _draggedElement = tabBorder;
        _draggedTab = tabEntry;
        _dragStartPoint = point.Position;
        _isDragging = false;
        _targetWindow = null;
        DragState = TabDragState.NoOperation;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedElement == null || _draggedTab == null || _window == null)
            return;

        var currentPoint = e.GetPosition(sender as Control);
        var dragVector = currentPoint - _dragStartPoint;
        var dragDistance = Math.Sqrt(dragVector.X * dragVector.X + dragVector.Y * dragVector.Y);

        if (!_isDragging && dragDistance > DragThreshold)
        {
            _isDragging = true;
            _draggedTab.IsDragging = true;
            
            if (sender is Control control)
            {
                e.Pointer.Capture(control);
            }
        }

        if (_isDragging && _itemsContainer != null)
        {
            var screenPoint = GetScreenPosition(e);
            UpdateDragState(screenPoint, currentPoint);

            if (DragState == TabDragState.ReorderInCurrentWindow)
            {
                CheckAndReorderTabs(currentPoint);
            }
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging && _draggedTab != null)
        {
            var screenPoint = GetScreenPosition(e);
            HandleDragDrop(screenPoint);
        }
        
        EndDrag();
        
        if (sender is Control control)
        {
            e.Pointer.Capture(null);
        }
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        EndDrag();
    }

    private void EndDrag()
    {
        if (_draggedTab != null)
        {
            _draggedTab.IsDragging = false;
        }

        _draggedElement = null;
        _draggedTab = null;
        _isDragging = false;
        _targetWindow = null;
        DragState = TabDragState.NoOperation;
    }

    private PixelPoint GetScreenPosition(PointerEventArgs e)
    {
        if (_window == null)
            return new PixelPoint(0, 0);
        
        var visualRoot = _window as Visual;
        if (visualRoot == null)
            return new PixelPoint(0, 0);
        
        var clientPoint = e.GetPosition(visualRoot);
        return visualRoot.PointToScreen(clientPoint);
    }

    private void UpdateDragState(PixelPoint screenPoint, Point containerPoint)
    {
        if (_window == null || _itemsContainer == null)
            return;

        var containerBounds = new Rect(new Point(0, 0), _itemsContainer.Bounds.Size);
        
        if (containerBounds.Contains(containerPoint))
        {
            DragState = TabDragState.ReorderInCurrentWindow;
            _targetWindow = null;
            return;
        }

        var targetWindow = FindTargetWindow(screenPoint);
        if (targetWindow != null)
        {
            if (targetWindow == _window)
            {
                DragState = TabDragState.ReorderInCurrentWindow;
            }
            else
            {
                DragState = TabDragState.TransferToAnotherWindow;
            }
            _targetWindow = targetWindow;
            return;
        }

        DragState = TabDragState.DetachToNewWindow;
        _targetWindow = null;
    }

    private TioTabWindowBase? FindTargetWindow(PixelPoint screenPoint)
    {
        foreach (var window in TioTabWindowBase.AllWindows)
        {
            if (!window.IsVisible)
                continue;

            var windowPosition = new Point(window.Position.X, window.Position.Y);
            var windowBounds = new Rect(windowPosition, window.ClientSize);
            var screenPointAsPoint = new Point(screenPoint.X, screenPoint.Y);
            if (windowBounds.Contains(screenPointAsPoint))
            {
                return window;
            }
        }
        
        return null;
    }

    private void HandleDragDrop(PixelPoint screenPoint)
    {
        if (_draggedTab == null)
            return;

        switch (DragState)
        {
            case TabDragState.TransferToAnotherWindow:
                if (_targetWindow != null)
                {
                    _draggedTab.MoveTabToWindow(_targetWindow);
                }
                break;
            
            case TabDragState.DetachToNewWindow:
                _draggedTab.MoveTabToNewWindow(screenPoint);
                break;
            
            case TabDragState.ReorderInCurrentWindow:
                break;
        }
    }

    private Border? FindTabBorder(Control? element)
    {
        while (element != null)
        {
            if (element is Border border && border.Classes.Contains("tab-root"))
            {
                return border;
            }
            element = element.Parent as Control;
        }
        return null;
    }

    private bool IsClickOnCloseButton(Control? element)
    {
        while (element != null)
        {
            if (element is Button button && button.Name == "CloseButton")
            {
                return true;
            }
            if (element is Border border && border.Classes.Contains("tab-root"))
            {
                return false;
            }
            element = element.Parent as Control;
        }
        return false;
    }

    private void CheckAndReorderTabs(Point currentPoint)
    {
        if (_draggedTab == null || _window == null || _itemsContainer == null)
            return;

        var tabs = _window.Tabs;
        var currentIndex = tabs.IndexOf(_draggedTab);
        
        if (currentIndex < 0)
            return;

        var targetTab = FindTabAtPositionOrNearest(currentPoint);
        
        if (targetTab != null && targetTab != _draggedTab)
        {
            var targetIndex = tabs.IndexOf(targetTab);
            
            if (targetIndex >= 0 && targetIndex != currentIndex)
            {
                _window.ReorderTab(currentIndex, targetIndex);
            }
        }
    }
    
    private TabEntry? FindTabAtPositionOrNearest(Point position)
    {
        if (_itemsContainer == null)
            return null;

        var containerBounds = new Rect(new Point(0, 0), _itemsContainer.Bounds.Size);
        if (!containerBounds.Contains(position))
        {
            return null;
        }

        var tabBorders = _itemsContainer.GetVisualDescendants()
            .OfType<Border>()
            .Where(b => b.Classes.Contains("tab-root") && b.Tag is TabEntry)
            .Select(b => new
            {
                Border = b,
                Tab = b.Tag as TabEntry,
                Position = b.TranslatePoint(new Point(0, 0), _itemsContainer),
                Bounds = b.Bounds
            })
            .Where(x => x.Tab != null && x.Position.HasValue)
            .ToList();

        if (tabBorders.Count == 0)
            return null;

        foreach (var item in tabBorders)
        {
            var relativeBounds = new Rect(item.Position!.Value, item.Bounds.Size);
            if (relativeBounds.Contains(position))
            {
                return item.Tab;
            }
        }

        return null;
    }
}
