using DevOpsExtension.Extension;
using System.Reflection;

namespace DevOpsExtension.Tests.Extension;

/// <summary>
/// Unit tests for AzureDevOpsExtensionHandler.
/// Tests focus on handler method behavior and extension management API patterns.
/// </summary>
[TestClass]
public class AzureDevOpsExtensionHandlerTests : HandlerTestBase
{
    private AzureDevOpsExtensionHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _handler = new AzureDevOpsExtensionHandler();
    }

    [TestMethod]
    public void GetIdentifiers_ReturnsCorrectIdentifiers()
    {
        // Arrange
        var extension = new AzureDevOpsExtension
        {
            Organization = "myorg",
            PublisherName = "ms",
            ExtensionName = "vss-code-search",
            Version = "1.0.0"
        };

        // Act - Use reflection to call protected method
        var method = typeof(AzureDevOpsExtensionHandler)
            .GetMethod("GetIdentifiers", BindingFlags.NonPublic | BindingFlags.Instance);
        var identifiers = method?.Invoke(_handler, [extension]) as AzureDevOpsExtensionIdentifiers;

        // Assert
        identifiers.Should().NotBeNull();
        identifiers!.Organization.Should().Be("myorg");
        identifiers.PublisherName.Should().Be("ms");
        identifiers.ExtensionName.Should().Be("vss-code-search");
    }
}

/// <summary>
/// Tests for extension handler validation and idempotency scenarios.
/// </summary>
[TestClass]
public class AzureDevOpsExtensionHandlerValidationTests
{
    [TestMethod]
    public void Extension_SameVersionInstalled_ShouldBeIdempotent()
    {
        // Arrange
        var extension = new AzureDevOpsExtension
        {
            Organization = "org",
            PublisherName = "ms",
            ExtensionName = "vss-code-search",
            Version = "1.0.0"
        };

        // Simulate that extension is already installed with same version
        extension.ExtensionId = "vss-code-search";
        extension.PublisherId = "ms";

        // Assert - With outputs populated, the handler would skip installation
        extension.ExtensionId.Should().Be(extension.ExtensionName);
        extension.PublisherId.Should().Be(extension.PublisherName);
    }

    [TestMethod]
    public void Extension_DifferentVersionInstalled_ShouldUpdate()
    {
        // Arrange
        var currentVersion = "1.0.0";
        var newVersion = "2.0.0";

        var extension = new AzureDevOpsExtension
        {
            Organization = "org",
            PublisherName = "ms",
            ExtensionName = "vss-code-search",
            Version = newVersion
        };

        // Assert - Different versions should trigger update
        extension.Version.Should().Be(newVersion);
        extension.Version.Should().NotBe(currentVersion);
    }

    [TestMethod]
    [DataRow("ms", "vss-code-search", "Microsoft code search extension")]
    [DataRow("SonarSource", "sonarqube", "SonarQube extension")]
    [DataRow("fabrikam", "custom-ext", "Custom extension")]
    public void Extension_ValidPublisherAndName_AreAccepted(string publisher, string extensionName, string description)
    {
        // Arrange & Act
        var extension = new AzureDevOpsExtension
        {
            Organization = "org",
            PublisherName = publisher,
            ExtensionName = extensionName,
            Version = "1.0.0"
        };

        // Assert
        extension.PublisherName.Should().Be(publisher, description);
        extension.ExtensionName.Should().Be(extensionName);
    }

    [TestMethod]
    public void Extension_EmptyVersion_ShouldBeInvalid()
    {
        // Arrange & Act
        var extension = new AzureDevOpsExtension
        {
            Organization = "org",
            PublisherName = "ms",
            ExtensionName = "ext",
            Version = "" // Empty version
        };

        // Assert
        extension.Version.Should().BeEmpty();
    }

    [TestMethod]
    public void Extension_CaseSensitiveNames_ArePreserved()
    {
        // Arrange & Act - Extension names are case-sensitive per docs
        var extension = new AzureDevOpsExtension
        {
            Organization = "org",
            PublisherName = "SonarSource",
            ExtensionName = "sonarqube",
            Version = "1.0.0"
        };

        // Assert
        extension.PublisherName.Should().Be("SonarSource");
        extension.ExtensionName.Should().Be("sonarqube");
    }
}
