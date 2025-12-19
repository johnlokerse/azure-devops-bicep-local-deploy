using DevOpsExtension.ServiceConnection;

namespace DevOpsExtension.Tests.ServiceConnection;

/// <summary>
/// Unit tests for AzureDevOpsServiceConnection model and related classes.
/// </summary>
[TestClass]
public class AzureDevOpsServiceConnectionTests
{
    [TestMethod]
    public void AzureDevOpsServiceConnection_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var connection = new AzureDevOpsServiceConnection
        {
            Name = "test-connection",
            Organization = "testorg",
            Project = "test-project",
            TenantId = "tenant-guid",
            ClientId = "client-guid"
        };

        // Assert
        connection.ScopeLevel.Should().Be(AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.Subscription);
        connection.GrantAllPipelines.Should().BeFalse();
        connection.Description.Should().BeNull();
        connection.SubscriptionId.Should().BeNull();
        connection.SubscriptionName.Should().BeNull();
        connection.ManagementGroupId.Should().BeNull();
        connection.ManagementGroupName.Should().BeNull();
        connection.ServiceConnectionId.Should().BeNull();
        connection.Url.Should().BeNull();
        connection.Issuer.Should().BeNull();
        connection.SubjectIdentifier.Should().BeNull();
    }

    [TestMethod]
    public void AzureDevOpsServiceConnection_SubscriptionScoped_HasCorrectProperties()
    {
        // Arrange & Act
        var connection = new AzureDevOpsServiceConnection
        {
            Name = "azure-subscription",
            Organization = "myorg",
            Project = "my-project",
            TenantId = "00000000-0000-0000-0000-000000000001",
            ClientId = "00000000-0000-0000-0000-000000000002",
            ScopeLevel = AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.Subscription,
            SubscriptionId = "00000000-0000-0000-0000-000000000003",
            SubscriptionName = "My Azure Subscription"
        };

        // Assert
        connection.ScopeLevel.Should().Be(AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.Subscription);
        connection.SubscriptionId.Should().Be("00000000-0000-0000-0000-000000000003");
        connection.SubscriptionName.Should().Be("My Azure Subscription");
        connection.ManagementGroupId.Should().BeNull();
        connection.ManagementGroupName.Should().BeNull();
    }

    [TestMethod]
    public void AzureDevOpsServiceConnection_ManagementGroupScoped_HasCorrectProperties()
    {
        // Arrange & Act
        var connection = new AzureDevOpsServiceConnection
        {
            Name = "azure-mg",
            Organization = "myorg",
            Project = "my-project",
            TenantId = "00000000-0000-0000-0000-000000000001",
            ClientId = "00000000-0000-0000-0000-000000000002",
            ScopeLevel = AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.ManagementGroup,
            ManagementGroupId = "mg-id-001",
            ManagementGroupName = "Enterprise"
        };

        // Assert
        connection.ScopeLevel.Should().Be(AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.ManagementGroup);
        connection.ManagementGroupId.Should().Be("mg-id-001");
        connection.ManagementGroupName.Should().Be("Enterprise");
        connection.SubscriptionId.Should().BeNull();
        connection.SubscriptionName.Should().BeNull();
    }

    [TestMethod]
    public void AzureDevOpsServiceConnection_WithGrantAllPipelines_IsTrue()
    {
        // Arrange & Act
        var connection = new AzureDevOpsServiceConnection
        {
            Name = "test",
            Organization = "org",
            Project = "project",
            TenantId = "tenant",
            ClientId = "client",
            GrantAllPipelines = true
        };

        // Assert
        connection.GrantAllPipelines.Should().BeTrue();
    }

    [TestMethod]
    public void AzureDevOpsServiceConnection_OutputProperties_CanBePopulated()
    {
        // Arrange
        var connection = new AzureDevOpsServiceConnection
        {
            Name = "test",
            Organization = "org",
            Project = "project",
            TenantId = "tenant",
            ClientId = "client"
        };

        // Act
        connection.ServiceConnectionId = "connection-guid";
        connection.Url = "https://management.azure.com/";
        connection.AuthorizationScheme = "WorkloadIdentityFederation";
        connection.Issuer = "https://vstoken.dev.azure.com/org";
        connection.SubjectIdentifier = "sc://org/project/test";

        // Assert
        connection.ServiceConnectionId.Should().Be("connection-guid");
        connection.Url.Should().Be("https://management.azure.com/");
        connection.AuthorizationScheme.Should().Be("WorkloadIdentityFederation");
        connection.Issuer.Should().Contain("vstoken.dev.azure.com");
        connection.SubjectIdentifier.Should().StartWith("sc://");
    }
}

/// <summary>
/// Unit tests for AzureDevOpsServiceConnectionIdentifiers.
/// </summary>
[TestClass]
public class AzureDevOpsServiceConnectionIdentifiersTests
{
    [TestMethod]
    public void AzureDevOpsServiceConnectionIdentifiers_RequiredProperties_CanBeSet()
    {
        // Arrange & Act
        var identifiers = new AzureDevOpsServiceConnectionIdentifiers
        {
            Name = "test-connection",
            Organization = "testorg",
            Project = "test-project"
        };

        // Assert
        identifiers.Name.Should().Be("test-connection");
        identifiers.Organization.Should().Be("testorg");
        identifiers.Project.Should().Be("test-project");
    }
}

/// <summary>
/// Tests for ServiceConnectionScopeLevel enum.
/// </summary>
[TestClass]
public class ServiceConnectionScopeLevelTests
{
    [TestMethod]
    [DataRow(AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.Subscription, "Subscription")]
    [DataRow(AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.ManagementGroup, "ManagementGroup")]
    public void ScopeLevel_EnumValues_AreCorrect(AzureDevOpsServiceConnection.ServiceConnectionScopeLevel level, string expectedName)
    {
        // Assert
        level.ToString().Should().Be(expectedName);
    }
}
