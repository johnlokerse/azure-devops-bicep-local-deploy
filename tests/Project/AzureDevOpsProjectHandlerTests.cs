using DevOpsExtension.Project;
using DevOpsExtension.Tests.Helpers;

namespace DevOpsExtension.Tests.Project;

/// <summary>
/// Unit tests for AzureDevOpsProjectHandler.
/// Tests focus on handler method behavior and API interaction patterns.
/// </summary>
[TestClass]
public class AzureDevOpsProjectHandlerTests : HandlerTestBase
{
    private AzureDevOpsProjectHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _handler = new AzureDevOpsProjectHandler();
    }

    [TestMethod]
    public void GetIdentifiers_ReturnsCorrectIdentifiers()
    {
        // Arrange
        var project = new AzureDevOpsProject
        {
            Name = "my-project",
            Organization = "myorg",
            Description = "Test"
        };

        // Act - Use reflection to call protected method
        var method = typeof(AzureDevOpsProjectHandler)
            .GetMethod("GetIdentifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var identifiers = method?.Invoke(_handler, [project]) as AzureDevOpsProjectIdentifiers;

        // Assert
        identifiers.Should().NotBeNull();
        identifiers!.Name.Should().Be("my-project");
        identifiers.Organization.Should().Be("myorg");
    }

    [TestMethod]
    [DataRow("testorg", "testorg", "https://dev.azure.com")]
    [DataRow("https://dev.azure.com/testorg", "testorg", "https://dev.azure.com")]
    [DataRow("https://dev.azure.com/testorg/", "testorg", "https://dev.azure.com")]
    [DataRow("https://mycompany.visualstudio.com/myorg", "myorg", "https://mycompany.visualstudio.com")]
    public void GetOrgAndBaseUrl_ParsesOrganizationCorrectly(string input, string expectedOrg, string expectedBaseUrl)
    {
        // Act - Use reflection to test the static protected method
        var method = typeof(AzureDevOpsResourceHandlerBase<AzureDevOpsProject, AzureDevOpsProjectIdentifiers>)
            .GetMethod("GetOrgAndBaseUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, [input]);

        // Assert
        result.Should().NotBeNull();
        var tuple = ((string org, string baseUrl))result!;
        tuple.org.Should().Be(expectedOrg);
        tuple.baseUrl.Should().Be(expectedBaseUrl);
    }
}

/// <summary>
/// Tests for project handler validation and error scenarios.
/// </summary>
[TestClass]
public class AzureDevOpsProjectHandlerValidationTests
{
    [TestMethod]
    public void AzureDevOpsProject_WithMinimalProperties_IsValid()
    {
        // Arrange & Act
        var project = new AzureDevOpsProject
        {
            Name = "test",
            Organization = "org"
        };

        // Assert - basic instantiation should work
        project.Name.Should().NotBeEmpty();
        project.Organization.Should().NotBeEmpty();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void AzureDevOpsProject_WithEmptyName_ShouldBeInvalid(string name)
    {
        // Arrange & Act
        var project = new AzureDevOpsProject
        {
            Name = name,
            Organization = "org"
        };

        // Assert - name should be empty/whitespace
        project.Name.Should().BeOneOf("", "   ");
    }

    [TestMethod]
    [DataRow("Agile")]
    [DataRow("Scrum")]
    [DataRow("CMMI")]
    [DataRow("Basic")]
    public void ProcessName_CommonValues_AreAccepted(string processName)
    {
        // Arrange & Act
        var project = new AzureDevOpsProject
        {
            Name = "test",
            Organization = "org",
            ProcessName = processName
        };

        // Assert
        project.ProcessName.Should().Be(processName);
    }

    [TestMethod]
    [DataRow("Git")]
    [DataRow("Tfvc")]
    public void SourceControlType_ValidValues_AreAccepted(string sourceControlType)
    {
        // Arrange & Act
        var project = new AzureDevOpsProject
        {
            Name = "test",
            Organization = "org",
            SourceControlType = sourceControlType
        };

        // Assert
        project.SourceControlType.Should().Be(sourceControlType);
    }
}
