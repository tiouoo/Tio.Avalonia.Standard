using System.Collections.ObjectModel;

namespace Tio.Avalonia.Standard.Modules.Tasks;

/// <summary>
/// 应用生命周期内唯一的任务创建入口和根任务记录器。
/// </summary>
public sealed class TaskManager
{
    private readonly ObservableCollection<ManagedTask> _rootTasks = [];

    private TaskManager()
    {
        RootTasks = new ReadOnlyObservableCollection<ManagedTask>(_rootTasks);
    }

    public static TaskManager Instance { get; } = new();

    public ReadOnlyObservableCollection<ManagedTask> RootTasks { get; }

    /// <summary>
    /// 创建处于 <see cref="ManagedTaskStatus.Pending"/> 状态的根任务。
    /// 调用 <see cref="ManagedTask.Start"/> 前不会执行任何任务代码。
    /// </summary>
    public ManagedTask CreateTask(TaskOptions options)
    {
        var task = new ManagedTask(null, options, null);
        _rootTasks.Add(task);
        return task;
    }

    /// <summary>
    /// 创建带执行器的根任务。仍需由调用方在准备完成后显式调用 <see cref="ManagedTask.Start"/>。
    /// </summary>
    public ManagedTask CreateTask(TaskOptions options, Func<TaskExecutionContext, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var task = new ManagedTask(null, options, operation);
        _rootTasks.Add(task);
        return task;
    }

    public void RequestCancellationForAll()
    {
        foreach (var task in _rootTasks)
        {
            task.RequestCancellation();
        }
    }
}
