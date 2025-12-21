using DevOpsExtension.WorkItems;

namespace DevOpsExtension.Tests.WorkItems;

/// <summary>
/// Unit tests for AzureDevOpsWorkItem model and identifiers.
/// </summary>
[TestClass]
public class AzureDevOpsWorkItemTests
{
    [TestMethod]
    public void AzureDevOpsWorkItem_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var workItem = new AzureDevOpsWorkItem
        {
            Organization = "myorg",
            Project = "my-project",
            Id = 1,
            Title = "My Work Item",
            Type = "Task",
            Description = "Work item description"
        };

        // Assert
        workItem.Organization.Should().Be("myorg");
        workItem.Project.Should().Be("my-project");
        workItem.Id.Should().Be(1);
        workItem.Title.Should().Be("My Work Item");
        workItem.Type.Should().Be("Task");
        workItem.Description.Should().Be("Work item description");
    }

    [TestMethod]
    public void AzureDevOpsWorkItem_OptionalDescription_CanBeNull()
    {
        // Arrange & Act
        var workItem = new AzureDevOpsWorkItem
        {
            Organization = "org",
            Project = "project",
            Id = 1,
            Title = "Title",
            Type = "Task"
        };

        // Assert
        workItem.Description.Should().BeNull();
    }

    [TestMethod]
    [DataRow("Task", "Standard task")]
    [DataRow("Bug", "Bug work item")]
    [DataRow("User Story", "User story")]
    [DataRow("Feature", "Feature work item")]
    [DataRow("Epic", "Epic work item")]
    [DataRow("Issue", "Issue work item")]
    public void Type_CommonWorkItemTypes_AreAccepted(string type, string description)
    {
        // Arrange & Act
        var workItem = new AzureDevOpsWorkItem
        {
            Organization = "org",
            Project = "project",
            Id = 1,
            Title = "Test",
            Type = type
        };

        // Assert
        workItem.Type.Should().Be(type, description);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(100)]
    [DataRow(99999)]
    public void Id_PositiveValues_AreAccepted(int id)
    {
        // Arrange & Act
        var workItem = new AzureDevOpsWorkItem
        {
            Organization = "org",
            Project = "project",
            Id = id,
            Title = "Test",
            Type = "Task"
        };

        // Assert
        workItem.Id.Should().Be(id);
    }
}

/// <summary>
/// Unit tests for AzureDevOpsWorkItemIdentifiers.
/// </summary>
[TestClass]
public class AzureDevOpsWorkItemIdentifiersTests
{
    [TestMethod]
    public void AzureDevOpsWorkItemIdentifiers_RequiredProperties_CanBeSet()
    {
        // Arrange & Act
        var identifiers = new AzureDevOpsWorkItemIdentifiers
        {
            Organization = "testorg",
            Project = "test-project"
        };

        // Assert
        identifiers.Organization.Should().Be("testorg");
        identifiers.Project.Should().Be("test-project");
    }
}
