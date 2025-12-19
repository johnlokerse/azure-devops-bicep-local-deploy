using DevOpsExtension.Project;

namespace DevOpsExtension.Tests.Project;

/// <summary>
/// Unit tests for AzureDevOpsProject model and identifiers.
/// </summary>
[TestClass]
public class AzureDevOpsProjectTests
{
    [TestMethod]
    public void AzureDevOpsProject_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var project = new AzureDevOpsProject
        {
            Name = "test-project",
            Organization = "testorg"
        };

        // Assert
        project.Visibility.Should().Be(ProjectVisibility.Private);
        project.ProcessName.Should().Be("Agile");
        project.SourceControlType.Should().Be("Git");
        project.Description.Should().BeNull();
        project.ProjectId.Should().BeNull();
        project.State.Should().BeNull();
        project.Url.Should().BeNull();
    }

    [TestMethod]
    public void AzureDevOpsProject_SetAllProperties_ReturnsCorrectValues()
    {
        // Arrange & Act
        var project = new AzureDevOpsProject
        {
            Name = "my-project",
            Organization = "myorg",
            Description = "Test description",
            Visibility = ProjectVisibility.Public,
            ProcessName = "Scrum",
            SourceControlType = "Tfvc"
        };

        // Assert
        project.Name.Should().Be("my-project");
        project.Organization.Should().Be("myorg");
        project.Description.Should().Be("Test description");
        project.Visibility.Should().Be(ProjectVisibility.Public);
        project.ProcessName.Should().Be("Scrum");
        project.SourceControlType.Should().Be("Tfvc");
    }

    [TestMethod]
    [DataRow("Private", ProjectVisibility.Private)]
    [DataRow("Public", ProjectVisibility.Public)]
    public void ProjectVisibility_EnumValues_AreCorrect(string expectedName, ProjectVisibility visibility)
    {
        // Assert
        visibility.ToString().Should().Be(expectedName);
    }
}

/// <summary>
/// Unit tests for AzureDevOpsProjectIdentifiers.
/// </summary>
[TestClass]
public class AzureDevOpsProjectIdentifiersTests
{
    [TestMethod]
    public void AzureDevOpsProjectIdentifiers_RequiredProperties_CanBeSet()
    {
        // Arrange & Act
        var identifiers = new AzureDevOpsProjectIdentifiers
        {
            Name = "test-project",
            Organization = "testorg"
        };

        // Assert
        identifiers.Name.Should().Be("test-project");
        identifiers.Organization.Should().Be("testorg");
    }

    [TestMethod]
    [DataRow("myorg", "Standard organization name")]
    [DataRow("https://dev.azure.com/myorg", "Full URL format")]
    [DataRow("https://dev.azure.com/myorg/", "Full URL with trailing slash")]
    public void Organization_SupportsMultipleFormats(string organization, string description)
    {
        // Arrange & Act
        var identifiers = new AzureDevOpsProjectIdentifiers
        {
            Name = "test",
            Organization = organization
        };

        // Assert
        identifiers.Organization.Should().Be(organization, description);
    }
}
