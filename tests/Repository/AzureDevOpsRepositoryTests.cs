using DevOpsExtension.Repository;

namespace DevOpsExtension.Tests.Repository;

/// <summary>
/// Unit tests for AzureDevOpsRepository model and identifiers.
/// </summary>
[TestClass]
public class AzureDevOpsRepositoryTests
{
    [TestMethod]
    public void AzureDevOpsRepository_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var repository = new AzureDevOpsRepository
        {
            Name = "test-repo",
            Organization = "testorg",
            Project = "test-project"
        };

        // Assert
        repository.DefaultBranch.Should().BeNull();
        repository.RepositoryId.Should().BeNull();
        repository.WebUrl.Should().BeNull();
        repository.RemoteUrl.Should().BeNull();
        repository.SshUrl.Should().BeNull();
    }

    [TestMethod]
    public void AzureDevOpsRepository_SetAllProperties_ReturnsCorrectValues()
    {
        // Arrange & Act
        var repository = new AzureDevOpsRepository
        {
            Name = "my-repo",
            Organization = "myorg",
            Project = "my-project",
            DefaultBranch = "refs/heads/main"
        };

        // Assert
        repository.Name.Should().Be("my-repo");
        repository.Organization.Should().Be("myorg");
        repository.Project.Should().Be("my-project");
        repository.DefaultBranch.Should().Be("refs/heads/main");
    }

    [TestMethod]
    [DataRow("refs/heads/main")]
    [DataRow("refs/heads/develop")]
    [DataRow("refs/heads/feature/my-feature")]
    public void DefaultBranch_ValidFormats_AreAccepted(string branch)
    {
        // Arrange & Act
        var repository = new AzureDevOpsRepository
        {
            Name = "test",
            Organization = "org",
            Project = "project",
            DefaultBranch = branch
        };

        // Assert
        repository.DefaultBranch.Should().Be(branch);
    }
}

/// <summary>
/// Unit tests for AzureDevOpsRepositoryIdentifiers.
/// </summary>
[TestClass]
public class AzureDevOpsRepositoryIdentifiersTests
{
    [TestMethod]
    public void AzureDevOpsRepositoryIdentifiers_RequiredProperties_CanBeSet()
    {
        // Arrange & Act
        var identifiers = new AzureDevOpsRepositoryIdentifiers
        {
            Name = "test-repo",
            Organization = "testorg",
            Project = "test-project"
        };

        // Assert
        identifiers.Name.Should().Be("test-repo");
        identifiers.Organization.Should().Be("testorg");
        identifiers.Project.Should().Be("test-project");
    }
}
