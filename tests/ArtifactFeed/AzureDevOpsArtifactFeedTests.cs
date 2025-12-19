using DevOpsExtension.ArtifactFeed;

namespace DevOpsExtension.Tests.ArtifactFeed;

/// <summary>
/// Unit tests for AzureDevOpsArtifactFeed model and related classes.
/// </summary>
[TestClass]
public class AzureDevOpsArtifactFeedTests
{
    [TestMethod]
    public void AzureDevOpsArtifactFeed_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var feed = new AzureDevOpsArtifactFeed
        {
            Name = "test-feed",
            Organization = "testorg"
        };

        // Assert
        feed.HideDeletedPackageVersions.Should().BeTrue();
        feed.UpstreamEnabled.Should().BeTrue();
        feed.Description.Should().BeNull();
        feed.Project.Should().BeNull();
        feed.Permissions.Should().BeNull();
        feed.UpstreamSources.Should().BeNull();
        feed.FeedId.Should().BeNull();
        feed.Url.Should().BeNull();
        feed.ProjectReference.Should().BeNull();
    }

    [TestMethod]
    public void AzureDevOpsArtifactFeed_OrganizationScoped_HasNoProject()
    {
        // Arrange & Act
        var feed = new AzureDevOpsArtifactFeed
        {
            Name = "org-feed",
            Organization = "myorg",
            Description = "Organization-level feed"
        };

        // Assert
        feed.Project.Should().BeNull();
        feed.Name.Should().Be("org-feed");
        feed.Organization.Should().Be("myorg");
    }

    [TestMethod]
    public void AzureDevOpsArtifactFeed_ProjectScoped_HasProject()
    {
        // Arrange & Act
        var feed = new AzureDevOpsArtifactFeed
        {
            Name = "project-feed",
            Organization = "myorg",
            Project = "my-project",
            Description = "Project-scoped feed"
        };

        // Assert
        feed.Project.Should().Be("my-project");
        feed.Name.Should().Be("project-feed");
    }

    [TestMethod]
    public void AzureDevOpsArtifactFeed_WithPermissions_StoresCorrectly()
    {
        // Arrange & Act
        var feed = new AzureDevOpsArtifactFeed
        {
            Name = "feed-with-permissions",
            Organization = "myorg",
            Permissions =
            [
                new AzureDevOpsArtifactFeedPermission
                {
                    IdentityDescriptor = "descriptor-1",
                    Role = 4 // Owner
                },
                new AzureDevOpsArtifactFeedPermission
                {
                    IdentityId = "id-2",
                    Role = 2 // Contributor
                }
            ]
        };

        // Assert
        feed.Permissions.Should().HaveCount(2);
        feed.Permissions![0].IdentityDescriptor.Should().Be("descriptor-1");
        feed.Permissions[0].Role.Should().Be(4);
        feed.Permissions[1].IdentityId.Should().Be("id-2");
        feed.Permissions[1].Role.Should().Be(2);
    }

    [TestMethod]
    public void AzureDevOpsArtifactFeed_WithUpstreamSources_StoresCorrectly()
    {
        // Arrange & Act
        var feed = new AzureDevOpsArtifactFeed
        {
            Name = "feed-with-upstream",
            Organization = "myorg",
            UpstreamSources =
            [
                new AzureDevOpsArtifactFeedUpstreamSource
                {
                    Name = "npmjs",
                    Location = "https://registry.npmjs.org/",
                    Protocol = "npm"
                },
                new AzureDevOpsArtifactFeedUpstreamSource
                {
                    Name = "nuget-org",
                    Location = "https://api.nuget.org/v3/index.json",
                    Protocol = "nuget"
                }
            ]
        };

        // Assert
        feed.UpstreamSources.Should().HaveCount(2);
        feed.UpstreamSources![0].Name.Should().Be("npmjs");
        feed.UpstreamSources[0].Protocol.Should().Be("npm");
        feed.UpstreamSources[1].Name.Should().Be("nuget-org");
        feed.UpstreamSources[1].Protocol.Should().Be("nuget");
    }
}

/// <summary>
/// Unit tests for AzureDevOpsArtifactFeedIdentifiers.
/// </summary>
[TestClass]
public class AzureDevOpsArtifactFeedIdentifiersTests
{
    [TestMethod]
    public void AzureDevOpsArtifactFeedIdentifiers_RequiredProperties_CanBeSet()
    {
        // Arrange & Act
        var identifiers = new AzureDevOpsArtifactFeedIdentifiers
        {
            Name = "test-feed",
            Organization = "testorg"
        };

        // Assert
        identifiers.Name.Should().Be("test-feed");
        identifiers.Organization.Should().Be("testorg");
        identifiers.Project.Should().BeNull();
    }

    [TestMethod]
    public void AzureDevOpsArtifactFeedIdentifiers_WithProject_SetsProjectScope()
    {
        // Arrange & Act
        var identifiers = new AzureDevOpsArtifactFeedIdentifiers
        {
            Name = "test-feed",
            Organization = "testorg",
            Project = "my-project"
        };

        // Assert
        identifiers.Project.Should().Be("my-project");
    }
}

/// <summary>
/// Unit tests for AzureDevOpsArtifactFeedPermission.
/// </summary>
[TestClass]
public class AzureDevOpsArtifactFeedPermissionTests
{
    [TestMethod]
    [DataRow(1, "Reader")]
    [DataRow(2, "Contributor")]
    [DataRow(3, "Collaborator")]
    [DataRow(4, "Owner")]
    [DataRow(5, "Administrator")]
    public void Permission_RoleLevels_AreCorrect(int roleLevel, string description)
    {
        // Arrange & Act
        var permission = new AzureDevOpsArtifactFeedPermission
        {
            IdentityDescriptor = "test-descriptor",
            Role = roleLevel
        };

        // Assert
        permission.Role.Should().Be(roleLevel, $"Role {description} should have level {roleLevel}");
    }
}

/// <summary>
/// Unit tests for AzureDevOpsArtifactFeedUpstreamSource.
/// </summary>
[TestClass]
public class AzureDevOpsArtifactFeedUpstreamSourceTests
{
    [TestMethod]
    [DataRow("npm", "https://registry.npmjs.org/")]
    [DataRow("nuget", "https://api.nuget.org/v3/index.json")]
    [DataRow("pypi", "https://pypi.org/")]
    [DataRow("maven", "https://repo.maven.apache.org/maven2/")]
    public void UpstreamSource_CommonProtocols_AreSupported(string protocol, string location)
    {
        // Arrange & Act
        var source = new AzureDevOpsArtifactFeedUpstreamSource
        {
            Name = $"{protocol}-upstream",
            Protocol = protocol,
            Location = location
        };

        // Assert
        source.Protocol.Should().Be(protocol);
        source.Location.Should().Be(location);
    }
}

/// <summary>
/// Unit tests for AzureDevOpsArtifactFeedProjectReference.
/// </summary>
[TestClass]
public class AzureDevOpsArtifactFeedProjectReferenceTests
{
    [TestMethod]
    public void ProjectReference_Properties_CanBeSet()
    {
        // Arrange & Act
        var reference = new AzureDevOpsArtifactFeedProjectReference
        {
            Id = "00000000-0000-0000-0000-000000000001",
            Name = "my-project"
        };

        // Assert
        reference.Id.Should().Be("00000000-0000-0000-0000-000000000001");
        reference.Name.Should().Be("my-project");
    }
}
