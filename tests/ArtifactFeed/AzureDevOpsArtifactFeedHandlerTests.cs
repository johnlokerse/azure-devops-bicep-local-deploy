using DevOpsExtension.ArtifactFeed;

namespace DevOpsExtension.Tests.ArtifactFeed;

/// <summary>
/// Unit tests for AzureDevOpsArtifactFeedHandler.
/// Tests focus on handler method behavior and URL construction.
/// </summary>
[TestClass]
public class AzureDevOpsArtifactFeedHandlerTests : HandlerTestBase
{
    private AzureDevOpsArtifactFeedHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _handler = new AzureDevOpsArtifactFeedHandler();
    }

    [TestMethod]
    public void GetIdentifiers_OrganizationScoped_ReturnsCorrectIdentifiers()
    {
        // Arrange
        var feed = new AzureDevOpsArtifactFeed
        {
            Name = "my-feed",
            Organization = "myorg"
        };

        // Act - Use reflection to call protected method
        var method = typeof(AzureDevOpsArtifactFeedHandler)
            .GetMethod("GetIdentifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var identifiers = method?.Invoke(_handler, [feed]) as AzureDevOpsArtifactFeedIdentifiers;

        // Assert
        identifiers.Should().NotBeNull();
        identifiers!.Name.Should().Be("my-feed");
        identifiers.Organization.Should().Be("myorg");
        identifiers.Project.Should().BeNull();
    }

    [TestMethod]
    public void GetIdentifiers_ProjectScoped_ReturnsCorrectIdentifiers()
    {
        // Arrange
        var feed = new AzureDevOpsArtifactFeed
        {
            Name = "my-feed",
            Organization = "myorg",
            Project = "my-project"
        };

        // Act
        var method = typeof(AzureDevOpsArtifactFeedHandler)
            .GetMethod("GetIdentifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var identifiers = method?.Invoke(_handler, [feed]) as AzureDevOpsArtifactFeedIdentifiers;

        // Assert
        identifiers.Should().NotBeNull();
        identifiers!.Name.Should().Be("my-feed");
        identifiers.Organization.Should().Be("myorg");
        identifiers.Project.Should().Be("my-project");
    }
}

/// <summary>
/// Tests for artifact feed handler validation scenarios.
/// </summary>
[TestClass]
public class AzureDevOpsArtifactFeedHandlerValidationTests
{
    [TestMethod]
    public void Feed_WithHideDeletedPackageVersionsTrue_IsCorrect()
    {
        // Arrange & Act
        var feed = new AzureDevOpsArtifactFeed
        {
            Name = "test",
            Organization = "org",
            HideDeletedPackageVersions = true
        };

        // Assert
        feed.HideDeletedPackageVersions.Should().BeTrue();
    }

    [TestMethod]
    public void Feed_WithHideDeletedPackageVersionsFalse_IsCorrect()
    {
        // Arrange & Act
        var feed = new AzureDevOpsArtifactFeed
        {
            Name = "test",
            Organization = "org",
            HideDeletedPackageVersions = false
        };

        // Assert
        feed.HideDeletedPackageVersions.Should().BeFalse();
    }

    [TestMethod]
    public void Feed_WithUpstreamDisabled_IsCorrect()
    {
        // Arrange & Act
        var feed = new AzureDevOpsArtifactFeed
        {
            Name = "test",
            Organization = "org",
            UpstreamEnabled = false
        };

        // Assert
        feed.UpstreamEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void Feed_OutputProperties_CanBePopulated()
    {
        // Arrange
        var feed = new AzureDevOpsArtifactFeed
        {
            Name = "test",
            Organization = "org"
        };

        // Act
        feed.FeedId = "feed-guid-123";
        feed.Url = "https://feeds.dev.azure.com/org/_apis/packaging/feeds/feed-guid-123";
        feed.ProjectReference = new AzureDevOpsArtifactFeedProjectReference
        {
            Id = "project-guid",
            Name = "my-project"
        };

        // Assert
        feed.FeedId.Should().Be("feed-guid-123");
        feed.Url.Should().Contain("feeds.dev.azure.com");
        feed.ProjectReference.Should().NotBeNull();
        feed.ProjectReference!.Id.Should().Be("project-guid");
    }
}
