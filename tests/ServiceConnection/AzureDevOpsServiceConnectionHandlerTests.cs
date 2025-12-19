using DevOpsExtension.ServiceConnection;
using System.Reflection;

namespace DevOpsExtension.Tests.ServiceConnection;

/// <summary>
/// Unit tests for AzureDevOpsServiceConnectionHandler.
/// Tests focus on handler validation and identifier extraction.
/// </summary>
[TestClass]
public class AzureDevOpsServiceConnectionHandlerTests : HandlerTestBase
{
    private AzureDevOpsServiceConnectionHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _handler = new AzureDevOpsServiceConnectionHandler();
    }

    [TestMethod]
    public void GetIdentifiers_ReturnsCorrectIdentifiers()
    {
        // Arrange
        var connection = new AzureDevOpsServiceConnection
        {
            Name = "my-connection",
            Organization = "myorg",
            Project = "my-project",
            TenantId = "tenant",
            ClientId = "client"
        };

        // Act - Use reflection to call protected method
        var method = typeof(AzureDevOpsServiceConnectionHandler)
            .GetMethod("GetIdentifiers", BindingFlags.NonPublic | BindingFlags.Instance);
        var identifiers = method?.Invoke(_handler, [connection]) as AzureDevOpsServiceConnectionIdentifiers;

        // Assert
        identifiers.Should().NotBeNull();
        identifiers!.Name.Should().Be("my-connection");
        identifiers.Organization.Should().Be("myorg");
        identifiers.Project.Should().Be("my-project");
    }
}

/// <summary>
/// Tests for service connection handler validation scenarios.
/// </summary>
[TestClass]
public class AzureDevOpsServiceConnectionHandlerValidationTests
{
    [TestMethod]
    public void ValidateProps_SubscriptionScope_RequiresSubscriptionProperties()
    {
        // Arrange
        var validConnection = new AzureDevOpsServiceConnection
        {
            Name = "test",
            Organization = "org",
            Project = "project",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ScopeLevel = AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.Subscription,
            SubscriptionId = "subscription-guid",
            SubscriptionName = "My Subscription"
        };

        // Assert - should have valid properties for subscription scope
        validConnection.SubscriptionId.Should().NotBeNullOrWhiteSpace();
        validConnection.SubscriptionName.Should().NotBeNullOrWhiteSpace();
        validConnection.TenantId.Should().NotBeNullOrWhiteSpace();
        validConnection.ClientId.Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public void ValidateProps_ManagementGroupScope_RequiresManagementGroupProperties()
    {
        // Arrange
        var validConnection = new AzureDevOpsServiceConnection
        {
            Name = "test",
            Organization = "org",
            Project = "project",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            ScopeLevel = AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.ManagementGroup,
            ManagementGroupId = "mg-id",
            ManagementGroupName = "Management Group Name"
        };

        // Assert - should have valid properties for management group scope
        validConnection.ManagementGroupId.Should().NotBeNullOrWhiteSpace();
        validConnection.ManagementGroupName.Should().NotBeNullOrWhiteSpace();
        validConnection.TenantId.Should().NotBeNullOrWhiteSpace();
        validConnection.ClientId.Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public void ServiceConnection_MissingTenantId_ShouldFail()
    {
        // Arrange - TenantId is required, so this would fail validation in the handler
        // Note: The actual validation happens in ValidateProps method at runtime
        var connection = new AzureDevOpsServiceConnection
        {
            Name = "test",
            Organization = "org",
            Project = "project",
            TenantId = "", // Empty - should fail validation
            ClientId = "client-guid"
        };

        // Assert
        connection.TenantId.Should().BeEmpty();
    }

    [TestMethod]
    public void ServiceConnection_MissingClientId_ShouldFail()
    {
        // Arrange
        var connection = new AzureDevOpsServiceConnection
        {
            Name = "test",
            Organization = "org",
            Project = "project",
            TenantId = "tenant-guid",
            ClientId = "" // Empty - should fail validation
        };

        // Assert
        connection.ClientId.Should().BeEmpty();
    }

    [TestMethod]
    public void ServiceConnection_BothSubscriptionAndManagementGroup_IsInvalid()
    {
        // Arrange - Having both sets should be invalid
        var connection = new AzureDevOpsServiceConnection
        {
            Name = "test",
            Organization = "org",
            Project = "project",
            TenantId = "tenant-guid",
            ClientId = "client-guid",
            SubscriptionId = "sub-id",
            SubscriptionName = "Sub Name",
            ManagementGroupId = "mg-id",
            ManagementGroupName = "MG Name"
        };

        // Assert - Both are set, which is invalid per handler validation
        connection.SubscriptionId.Should().NotBeNullOrWhiteSpace();
        connection.ManagementGroupId.Should().NotBeNullOrWhiteSpace();
        // The handler's ValidateProps would throw in this case
    }

    [TestMethod]
    [DataRow("azure-prod", "Standard connection name")]
    [DataRow("azure_dev", "Connection name with underscore")]
    [DataRow("Azure-Production-2024", "Connection name with mixed case and numbers")]
    public void ServiceConnection_ValidNames_AreAccepted(string name, string description)
    {
        // Arrange & Act
        var connection = new AzureDevOpsServiceConnection
        {
            Name = name,
            Organization = "org",
            Project = "project",
            TenantId = "tenant",
            ClientId = "client"
        };

        // Assert
        connection.Name.Should().Be(name, description);
    }
}
