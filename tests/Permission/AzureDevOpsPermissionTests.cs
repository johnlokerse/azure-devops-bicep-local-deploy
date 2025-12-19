using DevOpsExtension.Permission;

namespace DevOpsExtension.Tests.Permission;

/// <summary>
/// Unit tests for AzureDevOpsPermission model and identifiers.
/// </summary>
[TestClass]
public class AzureDevOpsPermissionTests
{
    [TestMethod]
    public void AzureDevOpsPermission_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var permission = new AzureDevOpsPermission
        {
            Organization = "testorg",
            Project = "test-project",
            GroupObjectId = "00000000-0000-0000-0000-000000000001",
            Role = "Contributors"
        };

        // Assert
        permission.Assigned.Should().BeFalse();
        permission.GroupDescriptor.Should().BeNull();
        permission.ProjectGroupDescriptor.Should().BeNull();
    }

    [TestMethod]
    public void AzureDevOpsPermission_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var permission = new AzureDevOpsPermission
        {
            Organization = "myorg",
            Project = "my-project",
            GroupObjectId = "00000000-0000-0000-0000-000000000001",
            Role = "Project Administrators"
        };

        // Assert
        permission.Organization.Should().Be("myorg");
        permission.Project.Should().Be("my-project");
        permission.GroupObjectId.Should().Be("00000000-0000-0000-0000-000000000001");
        permission.Role.Should().Be("Project Administrators");
    }

    [TestMethod]
    public void AzureDevOpsPermission_OutputProperties_CanBePopulated()
    {
        // Arrange
        var permission = new AzureDevOpsPermission
        {
            Organization = "org",
            Project = "project",
            GroupObjectId = "guid",
            Role = "Contributors"
        };

        // Act
        permission.Assigned = true;
        permission.GroupDescriptor = "aadgp.descriptor";
        permission.ProjectGroupDescriptor = "vssgp.descriptor";

        // Assert
        permission.Assigned.Should().BeTrue();
        permission.GroupDescriptor.Should().StartWith("aadgp.");
        permission.ProjectGroupDescriptor.Should().StartWith("vssgp.");
    }

    [TestMethod]
    [DataRow("Readers", "Built-in readers role")]
    [DataRow("Contributors", "Built-in contributors role")]
    [DataRow("Project Administrators", "Built-in project admins role")]
    [DataRow("Build Administrators", "Built-in build admins role")]
    [DataRow("Endpoint Administrators", "Endpoint admins role")]
    [DataRow("Endpoint Creators", "Endpoint creators role")]
    [DataRow("Project Valid Users", "Project valid users role")]
    [DataRow("CustomRole", "Custom role name")]
    public void Role_CommonValues_AreAccepted(string role, string description)
    {
        // Arrange & Act
        var permission = new AzureDevOpsPermission
        {
            Organization = "org",
            Project = "project",
            GroupObjectId = "guid",
            Role = role
        };

        // Assert
        permission.Role.Should().Be(role, description);
    }

    [TestMethod]
    public void GroupObjectId_ValidGuid_IsAccepted()
    {
        // Arrange
        var validGuid = Guid.NewGuid().ToString();

        // Act
        var permission = new AzureDevOpsPermission
        {
            Organization = "org",
            Project = "project",
            GroupObjectId = validGuid,
            Role = "Contributors"
        };

        // Assert
        Guid.TryParse(permission.GroupObjectId, out _).Should().BeTrue();
    }
}

/// <summary>
/// Unit tests for AzureDevOpsPermissionIdentifiers.
/// </summary>
[TestClass]
public class AzureDevOpsPermissionIdentifiersTests
{
    [TestMethod]
    public void AzureDevOpsPermissionIdentifiers_RequiredProperties_CanBeSet()
    {
        // Arrange & Act
        var identifiers = new AzureDevOpsPermissionIdentifiers
        {
            Organization = "testorg",
            Project = "test-project"
        };

        // Assert
        identifiers.Organization.Should().Be("testorg");
        identifiers.Project.Should().Be("test-project");
    }
}
