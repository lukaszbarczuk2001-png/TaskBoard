namespace TaskBoard.Web.Services;

/// <summary>
/// Per-circuit cache of the project list, shared by the sidebar and the pages so a new
/// project shows up everywhere without every component fetching on its own.
/// </summary>
public class ProjectsState(TaskBoardApiClient api)
{
    public IReadOnlyList<ProjectDto> Projects { get; private set; } = [];
    public bool Loaded { get; private set; }
    public string? Error { get; private set; }

    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        if (!Loaded)
        {
            await RefreshAsync();
        }
    }

    public async Task RefreshAsync()
    {
        var result = await api.GetProjectsAsync();
        if (result.Ok)
        {
            Projects = result.Value!;
            Error = null;
            Loaded = true;
        }
        else
        {
            Error = result.Error;
        }

        Changed?.Invoke();
    }

    public void Clear()
    {
        Projects = [];
        Loaded = false;
        Error = null;
        Changed?.Invoke();
    }
}
