using DevOpsExtension.PipelineRun;

namespace DevOpsExtension.Tests.PipelineRun;

/// <summary>
/// Unit tests for AzureDevOpsPipelineRun model and identifiers.
/// </summary>
[TestClass]
public class AzureDevOpsPipelineRunTests
{
    [TestMethod]
    public void AzureDevOpsPipelineRun_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "testorg",
            Project = "test-project",
            PipelineId = "42"
        };

        // Assert
        pipelineRun.Branch.Should().BeNull();
        pipelineRun.Tag.Should().BeNull();
        pipelineRun.Variables.Should().BeNull();
        pipelineRun.TemplateParameters.Should().BeNull();
        pipelineRun.RunId.Should().Be(0);
        pipelineRun.State.Should().BeNull();
        pipelineRun.Result.Should().BeNull();
        pipelineRun.Url.Should().BeNull();
    }

    [TestMethod]
    public void AzureDevOpsPipelineRun_WithBranch_HasCorrectProperties()
    {
        // Arrange & Act
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "myorg",
            Project = "my-project",
            PipelineId = "42",
            Branch = "refs/heads/main"
        };

        // Assert
        pipelineRun.Branch.Should().Be("refs/heads/main");
        pipelineRun.Tag.Should().BeNull();
    }

    [TestMethod]
    public void AzureDevOpsPipelineRun_WithTag_HasCorrectProperties()
    {
        // Arrange & Act
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "myorg",
            Project = "my-project",
            PipelineId = "42",
            Tag = "refs/tags/v1.0.0"
        };

        // Assert
        pipelineRun.Tag.Should().Be("refs/tags/v1.0.0");
        pipelineRun.Branch.Should().BeNull();
    }

    [TestMethod]
    public void AzureDevOpsPipelineRun_WithVariables_StoresJsonString()
    {
        // Arrange & Act
        var variables = "{\"MyVar1\": \"value1\", \"MyVar2\": \"value2\"}";
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "org",
            Project = "project",
            PipelineId = "1",
            Branch = "main",
            Variables = variables
        };

        // Assert
        pipelineRun.Variables.Should().Be(variables);
    }

    [TestMethod]
    public void AzureDevOpsPipelineRun_WithTemplateParameters_StoresJsonString()
    {
        // Arrange & Act
        var templateParameters = "{\"Param1\": \"value1\"}";
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "org",
            Project = "project",
            PipelineId = "1",
            Branch = "main",
            TemplateParameters = templateParameters
        };

        // Assert
        pipelineRun.TemplateParameters.Should().Be(templateParameters);
    }

    [TestMethod]
    public void AzureDevOpsPipelineRun_OutputProperties_CanBePopulated()
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
        pipelineRun.RunId = 123;
        pipelineRun.State = "completed";
        pipelineRun.Result = "succeeded";
        pipelineRun.Url = "https://dev.azure.com/org/project/_build/results?buildId=123";

        // Assert
        pipelineRun.RunId.Should().Be(123);
        pipelineRun.State.Should().Be("completed");
        pipelineRun.Result.Should().Be("succeeded");
        pipelineRun.Url.Should().Contain("buildId=123");
    }

    [TestMethod]
    [DataRow("refs/heads/main", "Full ref format")]
    [DataRow("main", "Short branch name")]
    [DataRow("refs/heads/feature/my-feature", "Feature branch")]
    [DataRow("refs/heads/release/v1.0", "Release branch")]
    public void Branch_CommonFormats_AreAccepted(string branch, string description)
    {
        // Arrange & Act
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "org",
            Project = "project",
            PipelineId = "1",
            Branch = branch
        };

        // Assert
        pipelineRun.Branch.Should().Be(branch, description);
    }

    [TestMethod]
    [DataRow("refs/tags/v1.0.0", "Full ref format")]
    [DataRow("v1.0.0", "Short tag name")]
    [DataRow("refs/tags/release-2024.01", "Release tag")]
    public void Tag_CommonFormats_AreAccepted(string tag, string description)
    {
        // Arrange & Act
        var pipelineRun = new AzureDevOpsPipelineRun
        {
            Organization = "org",
            Project = "project",
            PipelineId = "1",
            Tag = tag
        };

        // Assert
        pipelineRun.Tag.Should().Be(tag, description);
    }
}

/// <summary>
/// Unit tests for AzureDevOpsPipelineRunIdentifiers.
/// </summary>
[TestClass]
public class AzureDevOpsPipelineRunIdentifiersTests
{
    [TestMethod]
    public void AzureDevOpsPipelineRunIdentifiers_RequiredProperties_CanBeSet()
    {
        // Arrange & Act
        var identifiers = new AzureDevOpsPipelineRunIdentifiers
        {
            Organization = "testorg",
            Project = "test-project",
            PipelineId = "42"
        };

        // Assert
        identifiers.Organization.Should().Be("testorg");
        identifiers.Project.Should().Be("test-project");
        identifiers.PipelineId.Should().Be("42");
    }

    [TestMethod]
    [DataRow("42", "Numeric ID")]
    [DataRow("my-pipeline", "Pipeline name")]
    [DataRow("Build-CI", "Pipeline name with hyphen")]
    public void PipelineId_SupportsIdAndName(string pipelineId, string description)
    {
        // Arrange & Act
        var identifiers = new AzureDevOpsPipelineRunIdentifiers
        {
            Organization = "org",
            Project = "project",
            PipelineId = pipelineId
        };

        // Assert
        identifiers.PipelineId.Should().Be(pipelineId, description);
    }
}
