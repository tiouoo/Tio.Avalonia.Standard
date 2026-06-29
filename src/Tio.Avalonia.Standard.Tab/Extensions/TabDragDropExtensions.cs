using Avalonia.Controls;
using Tio.Avalonia.Standard.Tab.Behavior;
using Tio.Avalonia.Standard.Tab.Interface;

namespace Tio.Avalonia.Standard.Tab.Extensions;

/// <summary>
/// 为标签页控件提供拖拽功能的扩展方法
/// </summary>
public static class TabDragDropExtensions
{
    private static readonly Dictionary<Control, TabDragDropBehavior> _behaviors = new();

    /// <summary>
    /// 为标签页容器启用拖拽重新排序功能
    /// </summary>
    /// <param name="container">标签页容器控件（通常是 SelectionList）</param>
    /// <param name="window">标签页窗口</param>
    public static void EnableTabDragDrop(this Control container, TioTabWindowBase window)
    {
        if (_behaviors.ContainsKey(container))
        {
            // 如果已经启用，先禁用旧的
            container.DisableTabDragDrop();
        }

        var behavior = new TabDragDropBehavior();
        behavior.Attach(container, window);
        _behaviors[container] = behavior;
    }

    /// <summary>
    /// 禁用标签页容器的拖拽功能
    /// </summary>
    /// <param name="container">标签页容器控件</param>
    public static void DisableTabDragDrop(this Control container)
    {
        if (_behaviors.TryGetValue(container, out var behavior))
        {
            behavior.Detach(container);
            _behaviors.Remove(container);
        }
    }
}
