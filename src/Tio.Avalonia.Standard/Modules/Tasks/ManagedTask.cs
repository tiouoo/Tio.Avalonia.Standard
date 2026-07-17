using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Tio.Avalonia.Standard.Modules.Tasks;

/// <summary>
/// 表示应用生命周期内的一个可观察、可取消任务节点。
/// </summary>
public sealed class ManagedTask : ObservableObject
{
    private readonly ObservableCollection<ManagedTask> _children = [];
    private readonly ObservableCollection<TaskAction> _actions = [];
    private readonly ObservableCollection<TaskLogEntry> _logs = [];
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Func<TaskExecutionContext, Task>? _operation;
    private readonly TaskCompletionSource _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private string? _description;
    private ManagedTaskStatus _status = ManagedTaskStatus.Pending;
    private DateTimeOffset? _startedAt;
    private DateTimeOffset? _completedAt;
    private Exception? _exception;

    internal ManagedTask(ManagedTask? parent, TaskOptions options, Func<TaskExecutionContext, Task>? operation)
    {
        ArgumentNullException.ThrowIfNull(options);

        Id = Guid.NewGuid();
        Parent = parent;
        Name = options.Name ?? throw new ArgumentException("任务名称不能为空。", nameof(options));
        Description = options.Description;
        _operation = operation;
        _cancellationTokenSource = parent is null
            ? new CancellationTokenSource()
            : CancellationTokenSource.CreateLinkedTokenSource(parent.CancellationToken);

        Children = new ReadOnlyObservableCollection<ManagedTask>(_children);
        Actions = new ReadOnlyObservableCollection<TaskAction>(_actions);
        Logs = new ReadOnlyObservableCollection<TaskLogEntry>(_logs);

        if (options.AddCancellationAction)
        {
            _actions.Add(new TaskAction(this, new TaskActionDefinition
            {
                Name = "取消任务",
                Description = "请求取消此任务及其子任务。",
                IconKey = "Cancel",
                ExecuteAsync = (task, _) =>
                {
                    task.RequestCancellation();
                    return Task.CompletedTask;
                },
                CanExecute = task => task.CanBeCancelled,
                IsVisible = task => !task.IsTerminal
            }));
        }

        foreach (var action in options.Actions)
        {
            _actions.Add(new TaskAction(this, action));
        }
    }

    public Guid Id { get; }

    public string Name { get; }

    public string? Description
    {
        get => _description;
        private set => SetProperty(ref _description, value);
    }

    public ManagedTask? Parent { get; }

    public ReadOnlyObservableCollection<ManagedTask> Children { get; }

    public ReadOnlyObservableCollection<TaskAction> Actions { get; }

    public ReadOnlyObservableCollection<TaskLogEntry> Logs { get; }

    public ManagedTaskStatus Status
    {
        get => _status;
        private set
        {
            if (!SetProperty(ref _status, value)) return;

            OnPropertyChanged(nameof(IsTerminal));
            OnPropertyChanged(nameof(CanBeCancelled));
            foreach (var action in _actions) action.Refresh();
        }
    }

    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;

    public DateTimeOffset? StartedAt
    {
        get => _startedAt;
        private set => SetProperty(ref _startedAt, value);
    }

    public DateTimeOffset? CompletedAt
    {
        get => _completedAt;
        private set => SetProperty(ref _completedAt, value);
    }

    public Exception? Exception
    {
        get => _exception;
        private set => SetProperty(ref _exception, value);
    }

    public string? ErrorMessage => Exception?.Message;

    public bool IsTerminal => Status is ManagedTaskStatus.Cancelled or ManagedTaskStatus.Completed or ManagedTaskStatus.Faulted;

    public bool CanBeCancelled => Status is ManagedTaskStatus.Pending or ManagedTaskStatus.Waiting or ManagedTaskStatus.Running;

    public bool IsCancellationRequested => _cancellationTokenSource.IsCancellationRequested;

    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public Task Completion => _completionSource.Task;

    /// <summary>
    /// 启动任务。没有执行器的任务进入 <see cref="ManagedTaskStatus.Running"/>，
    /// 并由调用方通过 <see cref="Complete"/> 或 <see cref="Fault"/> 终结。
    /// </summary>
    public void Start()
    {
        if (Status != ManagedTaskStatus.Pending)
            throw new InvalidOperationException("只有等待执行的任务可以启动。");

        StartedAt = DateTimeOffset.Now;
        Status = ManagedTaskStatus.Running;

        if (_operation is not null) _ = RunAsync();
    }

    /// <summary>
    /// 为无执行器的任务指定执行器并启动。任务只能启动一次。
    /// </summary>
    public void Start(Func<TaskExecutionContext, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (_operation is not null)
            throw new InvalidOperationException("此任务已配置执行器。");

        _operation = operation;
        Start();
    }

    public void RequestCancellation()
    {
        if (!CanBeCancelled) return;

        Status = ManagedTaskStatus.Cancelling;
        LogInformation("已请求取消任务，正在等待清理完成。");
        _cancellationTokenSource.Cancel();

        foreach (var child in _children)
        {
            child.RequestCancellation();
        }

        // 尚未启动的任务没有执行器或资源需要收尾，可以立即结束取消。
        if (StartedAt is null) CompleteCore(ManagedTaskStatus.Cancelled);
    }

    public string GetFormattedLog()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"任务: {Name}");
        builder.AppendLine($"状态: {Status}");
        builder.AppendLine($"创建时间: {CreatedAt:O}");

        foreach (var entry in _logs)
        {
            builder.Append($"[{entry.Timestamp:O}] [{entry.Level}] {entry.Message}");
            if (entry.Exception is not null) builder.Append($"{Environment.NewLine}{entry.Exception}");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    public ManagedTask CreateChild(TaskOptions options) => CreateChildCore(options, null);

    public ManagedTask CreateChild(TaskOptions options, Func<TaskExecutionContext, Task> operation) =>
        CreateChildCore(options, operation);

    private ManagedTask CreateChildCore(TaskOptions options, Func<TaskExecutionContext, Task>? operation)
    {
        if (IsCancellationRequested || IsTerminal)
            throw new InvalidOperationException("已取消或已完成的任务不能创建子任务。");

        var child = new ManagedTask(this, options, operation);
        _children.Add(child);
        return child;
    }

    /// <summary>
    /// 在任务存活期间动态添加一个可绑定的 UI 操作。
    /// </summary>
    public TaskAction AddAction(TaskActionDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (IsTerminal) throw new InvalidOperationException("已结束的任务不能添加操作。");

        var action = new TaskAction(this, definition);
        _actions.Add(action);
        return action;
    }

    /// <summary>
    /// 将一个无执行器的运行中或等待中任务标记为成功完成。
    /// </summary>
    public void Complete()
    {
        EnsureCanComplete();
        CompleteCore(IsCancellationRequested ? ManagedTaskStatus.Cancelled : ManagedTaskStatus.Completed);
    }

    /// <summary>
    /// 将一个无执行器的运行中或等待中任务标记为失败。
    /// </summary>
    public void Fault(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        EnsureCanComplete();
        Exception = exception;
        OnPropertyChanged(nameof(ErrorMessage));
        LogError("任务执行失败。", exception);
        CompleteCore(ManagedTaskStatus.Faulted);
    }

    internal void SetWaiting(string? description)
    {
        EnsureActive();
        if (description is not null) Description = description;
        Status = ManagedTaskStatus.Waiting;
    }

    internal void SetRunning(string? description)
    {
        EnsureActive();
        if (description is not null) Description = description;
        Status = ManagedTaskStatus.Running;
    }

    internal void LogDebug(string message) => AddLog(TaskLogLevel.Debug, message);

    internal void LogInformation(string message) => AddLog(TaskLogLevel.Information, message);

    internal void LogWarning(string message) => AddLog(TaskLogLevel.Warning, message);

    internal void LogError(string message, Exception? exception = null) => AddLog(TaskLogLevel.Error, message, exception);

    private async Task RunAsync()
    {
        StartedAt = DateTimeOffset.Now;
        Status = ManagedTaskStatus.Running;

        try
        {
            await _operation!(new TaskExecutionContext(this));
            if (IsCancellationRequested)
            {
                CompleteCore(ManagedTaskStatus.Cancelled);
            }
            else
            {
                CompleteCore(ManagedTaskStatus.Completed);
            }
        }
        catch (OperationCanceledException) when (IsCancellationRequested)
        {
            CompleteCore(ManagedTaskStatus.Cancelled);
        }
        catch (Exception exception)
        {
            Exception = exception;
            OnPropertyChanged(nameof(ErrorMessage));
            LogError("任务执行失败。", exception);
            CompleteCore(ManagedTaskStatus.Faulted);
        }
    }

    private void CompleteCore(ManagedTaskStatus status)
    {
        Status = status;
        CompletedAt = DateTimeOffset.Now;
        _completionSource.TrySetResult();
    }

    private void AddLog(TaskLogLevel level, string message, Exception? exception = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _logs.Add(new TaskLogEntry(DateTimeOffset.Now, level, message, exception));
        foreach (var action in _actions) action.Refresh();
    }

    private void EnsureActive()
    {
        CancellationToken.ThrowIfCancellationRequested();

        if (IsTerminal)
            throw new InvalidOperationException("已取消或已完成的任务不能更新执行状态。");
    }

    private void EnsureCanComplete()
    {
        if (Status is not (ManagedTaskStatus.Running or ManagedTaskStatus.Waiting or ManagedTaskStatus.Cancelling))
            throw new InvalidOperationException("只有运行中、等待中或取消中的任务可以结束。");
    }
}
