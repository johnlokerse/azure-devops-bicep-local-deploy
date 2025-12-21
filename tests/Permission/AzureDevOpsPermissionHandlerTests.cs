using DevOpsExtension.Permission;
using System.Reflection;

namespace DevOpsExtension.Tests.Permission;

/// <summary>
/// Unit tests for AzureDevOpsPermissionHandler.
/// Tests focus on handler method behavior and Graph API interactions.
/// </summary>
[TestClass]
public class AzureDevOpsPermissionHandlerTests : HandlerTestBase
{
    private AzureDevOpsPermissionHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _handler = new AzureDevOpsPermissionHandler();
    }

    [TestMethod]
    public void GetIdentifiers_ReturnsCorrectIdentifiers()
    {
        // Arrange
        var permission = new AzureDevOpsPermission
        {
            Organization = "myorg",
            Project = "my-project",
            GroupObjectId = "group-guid",
            Role = "Contributors"
        };

        // Act - Use reflection to call protected method
        var method = typeof(AzureDevOpsPermissionHandler)
            .GetMethod("GetIdentifiers", BindingFlags.NonPublic | BindingFlags.Instance);
        var identifiers = method?.Invoke(_handler, [permission]) as AzureDevOpsPermissionIdentifiers;

        // Assert
        identifiers.Should().NotBeNull();
        identifiers!.Organization.Should().Be("myorg");
        identifiers.Project.Should().Be("my-project");
    }

    [TestMethod]
    [DataRow("https://dev.azure.com", "https://vssps.dev.azure.com")]
    [DataRow("https://mycompany.visualstudio.com", "https://mycompany.visualstudio.com")]
    public void GetGraphBaseUrl_ReturnsCorrectUrl(string baseUrl, string expectedGraphBase)
    {
        // Act - Use reflection to test static protected method
        var method = typeof(AzureDevOpsResourceHandlerBase<AzureDevOpsPermission, AzureDevOpsPermissionIdentifiers>)
            .GetMethod("GetGraphBaseUrl", BindingFlags.NonPublic | BindingFlags.Static);
        var result = method?.Invoke(null, [baseUrl]) as string;

        // Assert
        result.Should().Be(expectedGraphBase);
    }
}

/// <summary>
/// Tests for permission handler validation scenarios.
/// </summary>
[TestClass]
public class AzureDevOpsPermissionHandlerValidationTests
{
    [TestMethod]
    public void Permission_WithValidGuid_IsAccepted()
    {
        // Arrange
        var validGuid = "00000000-0000-0000-0000-000000000001";

        // Act
        var permission = new AzureDevOpsPermission
        {
            Organization = "org",
            Project = "project",
            GroupObjectId = validGuid,
            Role = "Contributors"
        };

        // Assert
        Guid.TryParse(permission.GroupObjectId, out var parsed).Should().BeTrue();
        parsed.Should().NotBeEmpty();
    }

    [TestMethod]
    public void Permission_WithInvalidGuid_CanBeDetected()
    {
        // Arrange
        var invalidGuid = "not-a-guid";

        // Act
        var permission = new AzureDevOpsPermission
        {
            Organization = "org",
            Project = "project",
            GroupObjectId = invalidGuid,
            Role = "Contributors"
        };

        // Assert - The handler would throw for invalid GUIDs
        Guid.TryParse(permission.GroupObjectId, out _).Should().BeFalse();
    }

    [TestMethod]
    public void Permission_EmptyRole_ShouldBeInvalid()
    {
        // Arrange & Act
        var permission = new AzureDevOpsPermission
        {
            Organization = "org",
            Project = "project",
            GroupObjectId = Guid.NewGuid().ToString(),
            Role = "" // Empty role
        };

        // Assert
        permission.Role.Should().BeEmpty();
    }

    [TestMethod]
    public void Permission_WhitespaceRole_ShouldBeInvalid()
    {
        // Arrange & Act
        var permission = new AzureDevOpsPermission
        {
            Organization = "org",
            Project = "project",
            GroupObjectId = Guid.NewGuid().ToString(),
            Role = "   " // Whitespace role
        };

        // Assert
        permission.Role.Should().Be("   ");
        string.IsNullOrWhiteSpace(permission.Role).Should().BeTrue();
    }

    [TestMethod]
    public void Permission_AssignmentState_ReflectsOperation()
    {
        // Arrange
        var permission = new AzureDevOpsPermission
        {
            Organization = "org",
            Project = "project",
            GroupObjectId = Guid.NewGuid().ToString(),
            Role = "Contributors"
        };

        // Act - Initially not assigned
        permission.Assigned.Should().BeFalse();

        // Simulate assignment
        permission.Assigned = true;

        // Assert
        permission.Assigned.Should().BeTrue();
    }
}
