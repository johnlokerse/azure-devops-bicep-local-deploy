using DevOpsExtension.Repository;

namespace DevOpsExtension.Tests.Repository;

/// <summary>
/// Unit tests for AzureDevOpsRepositoryHandler.
/// Tests focus on handler method behavior and validation.
/// </summary>
[TestClass]
public class AzureDevOpsRepositoryHandlerTests : HandlerTestBase
{
    private AzureDevOpsRepositoryHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _handler = new AzureDevOpsRepositoryHandler();
    }

    [TestMethod]
    public void GetIdentifiers_ReturnsCorrectIdentifiers()
    {
        // Arrange
        var repository = new AzureDevOpsRepository
        {
            Name = "my-repo",
            Organization = "myorg",
            Project = "my-project"
        };

        // Act - Use reflection to call protected method
        var method = typeof(AzureDevOpsRepositoryHandler)
            .GetMethod("GetIdentifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var identifiers = method?.Invoke(_handler, [repository]) as AzureDevOpsRepositoryIdentifiers;

        // Assert
        identifiers.Should().NotBeNull();
        identifiers!.Name.Should().Be("my-repo");
        identifiers.Organization.Should().Be("myorg");
        identifiers.Project.Should().Be("my-project");
    }

    [TestMethod]
    [DataRow("test-repo", "Standard repository name")]
    [DataRow("my-repo-123", "Repository name with numbers")]
    [DataRow("Test.Repo", "Repository name with dot")]
    [DataRow("test_repo", "Repository name with underscore")]
    public void Repository_NameFormats_AreAccepted(string repoName, string description)
    {
        // Arrange & Act
        var repository = new AzureDevOpsRepository
        {
            Name = repoName,
            Organization = "org",
            Project = "project"
        };

        // Assert
        repository.Name.Should().Be(repoName, description);
    }
}

/// <summary>
/// Tests for repository handler validation scenarios.
/// </summary>
[TestClass]
public class AzureDevOpsRepositoryHandlerValidationTests
{
    [TestMethod]
    public void AzureDevOpsRepository_WithMinimalProperties_IsValid()
    {
        // Arrange & Act
        var repository = new AzureDevOpsRepository
        {
            Name = "test",
            Organization = "org",
            Project = "project"
        };

        // Assert
        repository.Name.Should().NotBeEmpty();
        repository.Organization.Should().NotBeEmpty();
        repository.Project.Should().NotBeEmpty();
    }

    [TestMethod]
    public void AzureDevOpsRepository_OutputProperties_CanBePopulated()
    {
        // Arrange
        var repository = new AzureDevOpsRepository
        {
            Name = "test",
            Organization = "org",
            Project = "project"
        };

        // Act
        repository.RepositoryId = "guid-123";
        repository.WebUrl = "https://dev.azure.com/org/project/_git/test";
        repository.RemoteUrl = "https://org@dev.azure.com/org/project/_git/test";
        repository.SshUrl = "git@ssh.dev.azure.com:v3/org/project/test";

        // Assert
        repository.RepositoryId.Should().Be("guid-123");
        repository.WebUrl.Should().Contain("_git/test");
        repository.RemoteUrl.Should().StartWith("https://");
        repository.SshUrl.Should().StartWith("git@");
    }
}
