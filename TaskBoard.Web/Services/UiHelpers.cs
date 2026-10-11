using MudBlazor;
using TaskBoard.Core.Enums;

namespace TaskBoard.Web.Services;

/// <summary>Small presentation helpers (icons, colors, labels) shared by pages and dialogs.</summary>
public static class UiHelpers
{
    private static readonly Color[] AvatarColors =
    [
        Color.Primary, Color.Secondary, Color.Tertiary, Color.Info, Color.Success, Color.Warning, Color.Error
    ];

    /// <summary>Stable color for a string (project key, user email) so the same item always looks the same.</summary>
    public static Color ColorFor(string? text)
    {
        if (string.IsNullOrEmpty(text)) return Color.Default;
        var hash = 0;
        foreach (var c in text) hash = unchecked(hash * 31 + c);
        return AvatarColors[(hash & int.MaxValue) % AvatarColors.Length];
    }

    private static readonly string[] BadgeColors =
    [
        "#5E6AD2", "#8B5CF6", "#3E8EF7", "#1F9D6B", "#D99100", "#E5484D", "#E5589D", "#0E9AA7"
    ];

    /// <summary>Stable hex color for small project badges in the sidebar.</summary>
    public static string ColorHex(string? text)
    {
        if (string.IsNullOrEmpty(text)) return BadgeColors[0];
        var hash = 0;
        foreach (var c in text) hash = unchecked(hash * 31 + c);
        return BadgeColors[(hash & int.MaxValue) % BadgeColors.Length];
    }

    public static string BadgeStyle(string? text) => $"background:{ColorHex(text)};color:#fff;font-weight:600";

    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var local = name.Split('@')[0];
        var parts = local.Split([' ', '.', '_', '-'], StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => (parts[0][..1] + parts[1][..1]).ToUpperInvariant()
        };
    }

    public static string TypeIcon(IssueType type) => type switch
    {
        IssueType.Bug => Icons.Material.Filled.BugReport,
        IssueType.Story => Icons.Material.Filled.Bookmark,
        _ => Icons.Material.Filled.CheckBox
    };

    public static Color TypeColor(IssueType type) => type switch
    {
        IssueType.Bug => Color.Error,
        IssueType.Story => Color.Success,
        _ => Color.Info
    };

    public static string PriorityIcon(IssuePriority priority) => priority switch
    {
        IssuePriority.High => Icons.Material.Filled.ArrowUpward,
        IssuePriority.Low => Icons.Material.Filled.ArrowDownward,
        _ => Icons.Material.Filled.DragHandle
    };

    public static Color PriorityColor(IssuePriority priority) => priority switch
    {
        IssuePriority.High => Color.Error,
        IssuePriority.Low => Color.Info,
        _ => Color.Warning
    };

    public static string StatusLabel(IssueStatus status) => status switch
    {
        IssueStatus.ToDo => "To do",
        IssueStatus.InProgress => "In progress",
        IssueStatus.Done => "Done",
        _ => status.ToString()
    };

    public static Color StatusColor(IssueStatus status) => status switch
    {
        IssueStatus.InProgress => Color.Info,
        IssueStatus.Done => Color.Success,
        _ => Color.Default
    };
}
