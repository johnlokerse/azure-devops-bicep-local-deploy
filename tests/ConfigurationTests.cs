namespace DevOpsExtension.Tests;

/// <summary>
/// Unit tests for Configuration model.
/// </summary>
[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void Configuration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new Configuration();

        // Assert
        config.AccessToken.Should().BeNull();
    }

    [TestMethod]
    public void Configuration_AccessToken_CanBeSet()
    {
        // Arrange & Act
        var config = new Configuration
        {
            AccessToken = "test-pat-token"
        };

        // Assert
        config.AccessToken.Should().Be("test-pat-token");
    }

    [TestMethod]
    public void Configuration_AccessToken_CanBeNull()
    {
        // Arrange - When no PAT is provided, the handler falls back to Azure Entra credentials
        var config = new Configuration
        {
            AccessToken = null
        };

        // Assert
        config.AccessToken.Should().BeNull();
    }
}
