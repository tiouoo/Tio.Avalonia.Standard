namespace Tio.Avalonia.Standard.Modules.Tasks;

/// <summary>
/// 创建受管理任务的配置。
/// </summary>
public sealed class TaskOptions
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>
    /// 是否自动提供“取消任务”操作。默认值为 <see langword="true"/>。
    /// </summary>
    public bool AddCancellationAction { get; init; } = true;

    public IReadOnlyCollection<TaskActionDefinition> Actions { get; init; } = [];
}
