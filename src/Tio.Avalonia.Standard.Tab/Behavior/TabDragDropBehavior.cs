using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Tio.Avalonia.Standard.Tab.Entries;
using Tio.Avalonia.Standard.Tab.Interface;

namespace Tio.Avalonia.Standard.Tab.Behavior;

/// <summary>
/// 为标签页提供拖拽重新排序功能的行为类
/// </summary>
public class TabDragDropBehavior
{
    private Control? _draggedElement;
    private TabEntry? _draggedTab;
    private Point _dragStartPoint;
    private bool _isDragging;
    private Control? _itemsContainer;
    private TioTabWindowBase? _window;
    private const double DragThreshold = 5.0;

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
        
        // 只处理左键按下
        if (!point.Properties.IsLeftButtonPressed)
            return;

        // 查找被按下的标签页元素
        var element = e.Source as Control;
        var tabBorder = FindTabBorder(element);
        
        if (tabBorder == null)
            return;

        var tabEntry = tabBorder.Tag as TabEntry;
        if (tabEntry == null)
            return;

        // 检查是否点击在关闭按钮或右键菜单上
        if (IsClickOnCloseButton(element))
            return;

        _draggedElement = tabBorder;
        _draggedTab = tabEntry;
        _dragStartPoint = point.Position;
        _isDragging = false;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedElement == null || _draggedTab == null || _window == null)
            return;

        var currentPoint = e.GetPosition(sender as Control);
        var dragVector = currentPoint - _dragStartPoint;
        var dragDistance = Math.Sqrt(dragVector.X * dragVector.X + dragVector.Y * dragVector.Y);

        // 检查是否超过拖拽阈值
        if (!_isDragging && dragDistance > DragThreshold)
        {
            _isDragging = true;
            _draggedTab.IsDragging = true;
            
            // 捕获指针以确保即使鼠标移出控件也能接收事件
            if (sender is Control control)
            {
                e.Pointer.Capture(control);
            }
        }

        if (_isDragging && _itemsContainer != null)
        {
            // 检查是否需要重新排序
            CheckAndReorderTabs(currentPoint);
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        EndDrag();
        
        // 释放指针捕获
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

        // 查找光标下或最接近的标签页
        var targetTab = FindTabAtPositionOrNearest(currentPoint);
        
        if (targetTab != null && targetTab != _draggedTab)
        {
            var targetIndex = tabs.IndexOf(targetTab);
            
            if (targetIndex >= 0 && targetIndex != currentIndex)
            {
                // 执行重新排序
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

    // 允许鼠标移出容器继续操作
    // private TabEntry? FindTabAtPositionOrNearest(Point position)
    // {
    //     if (_itemsContainer == null)
    //         return null;
    //
    //     // 获取所有标签页边框元素及其位置信息
    //     var tabBorders = _itemsContainer.GetVisualDescendants()
    //         .OfType<Border>()
    //         .Where(b => b.Classes.Contains("tab-root") && b.Tag is TabEntry)
    //         .Select(b => new
    //         {
    //             Border = b,
    //             Tab = b.Tag as TabEntry,
    //             Position = b.TranslatePoint(new Point(0, 0), _itemsContainer),
    //             Bounds = b.Bounds
    //         })
    //         .Where(x => x.Tab != null && x.Position.HasValue)
    //         .ToList();
    //
    //     if (tabBorders.Count == 0)
    //         return null;
    //
    //     // 首先尝试精确匹配：查找鼠标位置在其范围内的标签页
    //     foreach (var item in tabBorders)
    //     {
    //         var relativeBounds = new Rect(item.Position!.Value, item.Bounds.Size);
    //         if (relativeBounds.Contains(position))
    //         {
    //             return item.Tab;
    //         }
    //     }
    //
    //     // 如果没有精确匹配，根据 X 轴位置找到最接近的标签页
    //     // 这样即使鼠标超出容器范围，也能根据水平位置进行排序
    //     var mouseX = position.X;
    //     
    //     // 查找最接近鼠标 X 位置的标签页
    //     TabEntry? nearestTab = null;
    //     double minDistance = double.MaxValue;
    //
    //     foreach (var item in tabBorders)
    //     {
    //         var tabCenterX = item.Position!.Value.X + item.Bounds.Width / 2;
    //         var distance = Math.Abs(tabCenterX - mouseX);
    //
    //         if (distance < minDistance)
    //         {
    //             minDistance = distance;
    //             nearestTab = item.Tab;
    //         }
    //     }
    //
    //     return nearestTab;
    // }
}
