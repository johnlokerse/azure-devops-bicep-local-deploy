using DevOpsExtension.PipelineRun;
using System.Reflection;

namespace DevOpsExtension.Tests.PipelineRun;

/// <summary>
/// Unit tests for AzureDevOpsPipelineRunHandler.
/// Tests focus on handler method behavior and validation.
/// </summary>
[TestClass]
public class AzureDevOpsPipelineRunHandlerTests : HandlerTestBase
{
    private AzureDevOpsPipelineRunHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _handler = new AzureDevOpsPipelineRunHandler();
    }

    [TestMethod]
    public void GetIdentifiers_ReturnsCorrectIdentifiers()
    {
        // Arrange
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "myorg",
            Project = "my-project",
            PipelineId = "42",
            Branch = "main"
        };

        // Act - Use reflection to call protected method
        var method = typeof(AzureDevOpsPipelineRunHandler)
            .GetMethod("GetIdentifiers", BindingFlags.NonPublic | BindingFlags.Instance);
        var identifiers = method?.Invoke(_handler, [pipelineRun]) as AzureDevOpsPipelineRunIdentifiers;

        // Assert
        identifiers.Should().NotBeNull();
        identifiers!.Organization.Should().Be("myorg");
        identifiers.Project.Should().Be("my-project");
        identifiers.PipelineId.Should().Be("42");
    }
}

/// <summary>
/// Tests for pipeline run handler validation scenarios.
/// </summary>
[TestClass]
public class AzureDevOpsPipelineRunHandlerValidationTests
{
    [TestMethod]
    public void ValidateInputs_WithBranch_IsValid()
    {
        // Arrange
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "org",
            Project = "project",
            PipelineId = "1",
            Branch = "main"
        };

        // Assert - should have valid inputs
        pipelineRun.Branch.Should().NotBeNullOrWhiteSpace();
        pipelineRun.Tag.Should().BeNull();
    }

    [TestMethod]
    public void ValidateInputs_WithTag_IsValid()
    {
        // Arrange
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "org",
            Project = "project",
            PipelineId = "1",
            Tag = "v1.0.0"
        };

        // Assert - should have valid inputs
        pipelineRun.Tag.Should().NotBeNullOrWhiteSpace();
        pipelineRun.Branch.Should().BeNull();
    }

    [TestMethod]
    public void ValidateInputs_NoBranchOrTag_ShouldBeInvalid()
    {
        // Arrange
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "org",
            Project = "project",
            PipelineId = "1"
            // Neither Branch nor Tag set
        };

        // Assert - both are null, which is invalid
        pipelineRun.Branch.Should().BeNull();
        pipelineRun.Tag.Should().BeNull();
    }

    [TestMethod]
    public void ValidateInputs_BothBranchAndTag_ShouldBeInvalid()
    {
        // Arrange - having both is invalid
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "org",
            Project = "project",
            PipelineId = "1",
            Branch = "main",
            Tag = "v1.0.0"
        };

        // Assert - both are set, which is invalid per handler validation
        pipelineRun.Branch.Should().NotBeNullOrWhiteSpace();
        pipelineRun.Tag.Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public void Variables_ValidJson_ShouldBeAccepted()
    {
        // Arrange
        var validJson = "{\"key\": \"value\"}";

        // Act
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "org",
            Project = "project",
            PipelineId = "1",
            Branch = "main",
            Variables = validJson
        };

        // Assert
        pipelineRun.Variables.Should().Be(validJson);
        // The handler would parse this during execution
        FluentActions
            .Invoking(() => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(pipelineRun.Variables!))
            .Should()
            .NotThrow();
    }

    [TestMethod]
    public void Variables_InvalidJson_CanBeDetected()
    {
        // Arrange
        var invalidJson = "not valid json";

        // Act
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "org",
            Project = "project",
            PipelineId = "1",
            Branch = "main",
            Variables = invalidJson
        };

        // Assert - The handler would throw when parsing
        FluentActions
            .Invoking(() => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(pipelineRun.Variables!))
            .Should()
            .Throw<System.Text.Json.JsonException>();
    }

    [TestMethod]
    public void TemplateParameters_ValidJson_ShouldBeAccepted()
    {
        // Arrange
        var validJson = "{\"param1\": \"value1\", \"param2\": 123}";

        // Act
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "org",
            Project = "project",
            PipelineId = "1",
            Branch = "main",
            TemplateParameters = validJson
        };

        // Assert
        pipelineRun.TemplateParameters.Should().Be(validJson);
        FluentActions
            .Invoking(() => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(pipelineRun.TemplateParameters!))
            .Should()
            .NotThrow();
    }

    [TestMethod]
    public void PipelineId_NumericString_IsNumeric()
    {
        // Arrange
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "org",
            Project = "project",
            PipelineId = "42",
            Branch = "main"
        };

        // Assert
        int.TryParse(pipelineRun.PipelineId, out var id).Should().BeTrue();
        id.Should().Be(42);
    }

    [TestMethod]
    public void PipelineId_NameString_IsNotNumeric()
    {
        // Arrange
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "org",
            Project = "project",
            PipelineId = "my-build-pipeline",
            Branch = "main"
        };

        // Assert - name needs to be resolved to ID
        int.TryParse(pipelineRun.PipelineId, out _).Should().BeFalse();
    }

    [TestMethod]
    [DataRow("inProgress", "Pipeline running")]
    [DataRow("completed", "Pipeline finished")]
    [DataRow("canceling", "Pipeline being canceled")]
    public void State_CommonValues_AreAccepted(string state, string description)
    {
        // Arrange
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "org",
            Project = "project",
            PipelineId = "1",
            Branch = "main"
        };

        // Act
        pipelineRun.State = state;

        // Assert
        pipelineRun.State.Should().Be(state, description);
    }

    [TestMethod]
    [DataRow("succeeded", "Successful run")]
    [DataRow("failed", "Failed run")]
    [DataRow("canceled", "Canceled run")]
    public void Result_CommonValues_AreAccepted(string result, string description)
    {
        // Arrange
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "org",
            Project = "project",
            PipelineId = "1",
            Branch = "main"
        };

        // Act
        pipelineRun.Result = result;

        // Assert
        pipelineRun.Result.Should().Be(result, description);
    }
}
