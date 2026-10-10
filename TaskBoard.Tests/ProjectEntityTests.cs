using TaskBoard.Core.Entities;
using Xunit;

namespace TaskBoard.Tests;

public class ProjectEntityTests
{
    [Fact]
    public void NewProject_HasGeneratedId()
    {
        var project = new Project { Key = "TB", Name = "TaskBoard" };

        Assert.NotEqual(Guid.Empty, project.Id);
    }

    [Fact]
    public void NewProject_StartsWithNoIssues()
    {
        var project = new Project { Key = "TB", Name = "TaskBoard" };

        Assert.Empty(project.Issues);
    }
}
