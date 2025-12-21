using DevOpsExtension.Project;

namespace DevOpsExtension.Tests;

/// <summary>
/// Unit tests for AzureDevOpsResourceHandlerBase utility methods.
/// Tests the common functionality shared across all resource handlers.
/// </summary>
[TestClass]
public class AzureDevOpsResourceHandlerBaseTests
{
    [TestMethod]
    [DataRow("testorg", "testorg", "https://dev.azure.com")]
    [DataRow("myorg", "myorg", "https://dev.azure.com")]
    [DataRow("https://dev.azure.com/testorg", "testorg", "https://dev.azure.com")]
    [DataRow("https://dev.azure.com/testorg/", "testorg", "https://dev.azure.com")]
    [DataRow("https://mycompany.visualstudio.com/myorg", "myorg", "https://mycompany.visualstudio.com")]
    [DataRow("https://mycompany.visualstudio.com/myorg/", "myorg", "https://mycompany.visualstudio.com")]
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

    [TestMethod]
    [DataRow("https://dev.azure.com", "https://vssps.dev.azure.com")]
    [DataRow("https://mycompany.visualstudio.com", "https://mycompany.visualstudio.com")]
    public void GetGraphBaseUrl_ReturnsCorrectGraphUrl(string baseUrl, string expectedGraphBase)
    {
        // Act - Use reflection to test the static protected method
        var method = typeof(AzureDevOpsResourceHandlerBase<AzureDevOpsProject, AzureDevOpsProjectIdentifiers>)
            .GetMethod("GetGraphBaseUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, [baseUrl]) as string;

        // Assert
        result.Should().Be(expectedGraphBase);
    }

    [TestMethod]
    public void GetGraphBaseUrl_DevAzureCom_ReturnsVsspsDomain()
    {
        // Arrange
        var baseUrl = "https://dev.azure.com";

        // Act
        var method = typeof(AzureDevOpsResourceHandlerBase<AzureDevOpsProject, AzureDevOpsProjectIdentifiers>)
            .GetMethod("GetGraphBaseUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, [baseUrl]) as string;

        // Assert
        result.Should().Be("https://vssps.dev.azure.com");
    }

    [TestMethod]
    public void GetGraphBaseUrl_CustomDomain_ReturnsSameDomain()
    {
        // Arrange - On-premises or custom domains should return same URL
        var baseUrl = "https://tfs.company.com";

        // Act
        var method = typeof(AzureDevOpsResourceHandlerBase<AzureDevOpsProject, AzureDevOpsProjectIdentifiers>)
            .GetMethod("GetGraphBaseUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, [baseUrl]) as string;

        // Assert
        result.Should().Be("https://tfs.company.com");
    }
}
