using DevOpsExtension.WorkItems;
using System.Reflection;

namespace DevOpsExtension.Tests.WorkItems;

/// <summary>
/// Unit tests for AzureDevOpsWorkItemHandler.
/// Tests focus on handler method behavior.
/// </summary>
[TestClass]
public class AzureDevOpsWorkItemHandlerTests : HandlerTestBase
{
    private AzureDevOpsWorkItemHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _handler = new AzureDevOpsWorkItemHandler();
    }

    [TestMethod]
    public void GetIdentifiers_ReturnsCorrectIdentifiers()
    {
        // Arrange
        var workItem = new AzureDevOpsWorkItem
        {
            Organization = "myorg",
            Project = "my-project",
            Id = 1,
            Title = "Test",
            Type = "Task"
        };

        // Act - Use reflection to call protected method
        var method = typeof(AzureDevOpsWorkItemHandler)
            .GetMethod("GetIdentifiers", BindingFlags.NonPublic | BindingFlags.Instance);
        var identifiers = method?.Invoke(_handler, [workItem]) as AzureDevOpsWorkItemIdentifiers;

        // Assert
        identifiers.Should().NotBeNull();
        identifiers!.Organization.Should().Be("myorg");
        identifiers.Project.Should().Be("my-project");
    }
}

/// <summary>
/// Unit tests for AzureDevOpsWorkItemClient.
/// Tests the work item client functionality with mocked HTTP responses.
/// </summary>
[TestClass]
public class AzureDevOpsWorkItemClientTests : HandlerTestBase
{
    [TestMethod]
    public async Task FindByAsync_WhenWorkItemExists_ReturnsId()
    {
        // Arrange
        var expectedId = 42;
        MockHandler.WithJsonResponse(new
        {
            workItems = new[] { new { id = expectedId } }
        });

        var client = CreateMockedClient();
        client.BaseAddress = new Uri("https://dev.azure.com/testorg/testproject/_apis/wit/");
        var workItemClient = new AzureDevOpsWorkItemClient(client);

        // Act
        var result = await workItemClient.FindByAsync(1, CancellationToken.None);

        // Assert
        result.Should().Be(expectedId);
    }

    [TestMethod]
    public async Task FindByAsync_WhenWorkItemNotFound_ReturnsNull()
    {
        // Arrange
        MockHandler.WithJsonResponse(new { workItems = Array.Empty<object>() });

        var client = CreateMockedClient();
        client.BaseAddress = new Uri("https://dev.azure.com/testorg/testproject/_apis/wit/");
        var workItemClient = new AzureDevOpsWorkItemClient(client);

        // Act
        var result = await workItemClient.FindByAsync(999, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task FindByAsync_WhenRequestFails_ReturnsNull()
    {
        // Arrange
        MockHandler.WithNotFound();

        var client = CreateMockedClient();
        client.BaseAddress = new Uri("https://dev.azure.com/testorg/testproject/_apis/wit/");
        var workItemClient = new AzureDevOpsWorkItemClient(client);

        // Act
        var result = await workItemClient.FindByAsync(1, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task CreateAsync_WithValidData_MakesPostRequest()
    {
        // Arrange
        MockHandler.WithJsonResponse(new { id = 1, rev = 1 });

        var client = CreateMockedClient();
        client.BaseAddress = new Uri("https://dev.azure.com/testorg/testproject/_apis/wit/");
        var workItemClient = new AzureDevOpsWorkItemClient(client);

        // Act
        await workItemClient.CreateAsync(1, "Test Title", "Task", "Description", CancellationToken.None);

        // Assert
        MockHandler.Requests.Should().HaveCount(1);
        MockHandler.Requests[0].Method.Should().Be(HttpMethod.Post);
        MockHandler.Requests[0].RequestUri?.ToString().Should().Contain("workitems/$Task");
    }

    [TestMethod]
    public async Task CreateAsync_ForBugType_UsesReproStepsField()
    {
        // Arrange
        MockHandler.WithJsonResponse(new { id = 1, rev = 1 });

        var client = CreateMockedClient();
        client.BaseAddress = new Uri("https://dev.azure.com/testorg/testproject/_apis/wit/");
        var workItemClient = new AzureDevOpsWorkItemClient(client);

        // Act
        await workItemClient.CreateAsync(1, "Bug Title", "Bug", "Repro steps here", CancellationToken.None);

        // Assert
        MockHandler.Requests.Should().HaveCount(1);
        var requestBody = await MockHandler.Requests[0].Content!.ReadAsStringAsync();
        requestBody.Should().Contain("Microsoft.VSTS.TCM.ReproSteps");
    }

    [TestMethod]
    public async Task CreateAsync_ForTaskType_UsesDescriptionField()
    {
        // Arrange
        MockHandler.WithJsonResponse(new { id = 1, rev = 1 });

        var client = CreateMockedClient();
        client.BaseAddress = new Uri("https://dev.azure.com/testorg/testproject/_apis/wit/");
        var workItemClient = new AzureDevOpsWorkItemClient(client);

        // Act
        await workItemClient.CreateAsync(1, "Task Title", "Task", "Task description", CancellationToken.None);

        // Assert
        MockHandler.Requests.Should().HaveCount(1);
        var requestBody = await MockHandler.Requests[0].Content!.ReadAsStringAsync();
        requestBody.Should().Contain("System.Description");
    }

    [TestMethod]
    public async Task UpdateAsync_WithValidData_MakesPatchRequest()
    {
        // Arrange
        MockHandler.WithJsonResponse(new { id = 1, rev = 2 });

        var client = CreateMockedClient();
        client.BaseAddress = new Uri("https://dev.azure.com/testorg/testproject/_apis/wit/");
        var workItemClient = new AzureDevOpsWorkItemClient(client);

        // Act
        await workItemClient.UpdateAsync(42, "Updated Title", "Task", "Updated description", CancellationToken.None);

        // Assert
        MockHandler.Requests.Should().HaveCount(1);
        MockHandler.Requests[0].Method.Should().Be(HttpMethod.Patch);
        MockHandler.Requests[0].RequestUri?.ToString().Should().Contain("workitems/42");
    }

    [TestMethod]
    public async Task CreateAsync_WhenRequestFails_ThrowsException()
    {
        // Arrange
        MockHandler.WithError(System.Net.HttpStatusCode.BadRequest, "Invalid work item data");

        var client = CreateMockedClient();
        client.BaseAddress = new Uri("https://dev.azure.com/testorg/testproject/_apis/wit/");
        var workItemClient = new AzureDevOpsWorkItemClient(client);

        // Act & Assert
        await FluentActions
            .Invoking(() => workItemClient.CreateAsync(1, "Title", "Task", "Desc", CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to create workitem*");
    }

    [TestMethod]
    public async Task UpdateAsync_WhenRequestFails_ThrowsException()
    {
        // Arrange
        MockHandler.WithError(System.Net.HttpStatusCode.NotFound, "Work item not found");

        var client = CreateMockedClient();
        client.BaseAddress = new Uri("https://dev.azure.com/testorg/testproject/_apis/wit/");
        var workItemClient = new AzureDevOpsWorkItemClient(client);

        // Act & Assert
        await FluentActions
            .Invoking(() => workItemClient.UpdateAsync(999, "Title", "Task", "Desc", CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to update workitem*");
    }
}
