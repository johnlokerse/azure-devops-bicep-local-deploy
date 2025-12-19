using DevOpsExtension.Extension;

namespace DevOpsExtension.Tests.Extension;

/// <summary>
/// Unit tests for AzureDevOpsExtension model and identifiers.
/// </summary>
[TestClass]
public class AzureDevOpsExtensionTests
{
    [TestMethod]
    public void AzureDevOpsExtension_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var extension = new AzureDevOpsExtension
        {
            Organization = "testorg",
            PublisherName = "ms",
            ExtensionName = "vss-code-search",
            Version = "1.0.0"
        };

        // Assert
        extension.ExtensionId.Should().BeNull();
        extension.PublisherId.Should().BeNull();
    }

    [TestMethod]
    public void AzureDevOpsExtension_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var extension = new AzureDevOpsExtension
        {
            Organization = "myorg",
            PublisherName = "SonarSource",
            ExtensionName = "sonarqube",
            Version = "7.4.1"
        };

        // Assert
        extension.Organization.Should().Be("myorg");
        extension.PublisherName.Should().Be("SonarSource");
        extension.ExtensionName.Should().Be("sonarqube");
        extension.Version.Should().Be("7.4.1");
    }

    [TestMethod]
    public void AzureDevOpsExtension_OutputProperties_CanBePopulated()
    {
        // Arrange
        var extension = new AzureDevOpsExtension
        {
            Organization = "org",
            PublisherName = "ms",
            ExtensionName = "vss-code-search",
            Version = "1.0.0"
        };

        // Act
        extension.ExtensionId = "vss-code-search";
        extension.PublisherId = "ms";

        // Assert
        extension.ExtensionId.Should().Be("vss-code-search");
        extension.PublisherId.Should().Be("ms");
    }

    [TestMethod]
    [DataRow("1.0.0", "Simple version")]
    [DataRow("20.263.0.848933653", "Complex version with build number")]
    [DataRow("7.4.1", "Standard semantic version")]
    [DataRow("2.0.0-preview", "Preview version")]
    public void Version_CommonFormats_AreAccepted(string version, string description)
    {
        // Arrange & Act
        var extension = new AzureDevOpsExtension
        {
            Organization = "org",
            PublisherName = "publisher",
            ExtensionName = "extension",
            Version = version
        };

        // Assert
        extension.Version.Should().Be(version, description);
    }
}

/// <summary>
/// Unit tests for AzureDevOpsExtensionIdentifiers.
/// </summary>
[TestClass]
public class AzureDevOpsExtensionIdentifiersTests
{
    [TestMethod]
    public void AzureDevOpsExtensionIdentifiers_RequiredProperties_CanBeSet()
    {
        // Arrange & Act
        var identifiers = new AzureDevOpsExtensionIdentifiers
        {
            Organization = "testorg",
            PublisherName = "ms",
            ExtensionName = "vss-code-search"
        };

        // Assert
        identifiers.Organization.Should().Be("testorg");
        identifiers.PublisherName.Should().Be("ms");
        identifiers.ExtensionName.Should().Be("vss-code-search");
    }

    [TestMethod]
    [DataRow("ms", "Microsoft publisher")]
    [DataRow("SonarSource", "Third-party publisher")]
    [DataRow("fabrikam", "Custom publisher")]
    public void PublisherName_CommonValues_AreAccepted(string publisher, string description)
    {
        // Arrange & Act
        var identifiers = new AzureDevOpsExtensionIdentifiers
        {
            Organization = "org",
            PublisherName = publisher,
            ExtensionName = "ext"
        };

        // Assert
        identifiers.PublisherName.Should().Be(publisher, description);
    }
}
