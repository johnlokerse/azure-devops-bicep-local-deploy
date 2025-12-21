---
applyTo: "**/*Tests.cs,**/*Test.cs,**/*.Tests/**/*.cs"
description: "Unit testing guidelines for Bicep local-deploy extension handlers"
---

# Unit Testing Guidelines for Bicep Extensions

## Project Setup
- Use MSTest as the test framework
- Use Moq for mocking dependencies
- Use FluentAssertions for readable assertions
- Target .NET 9

## Test Project Structure
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" />
  <PackageReference Include="MSTest.TestAdapter" />
  <PackageReference Include="MSTest.TestFramework" />
  <PackageReference Include="Moq" />
  <PackageReference Include="FluentAssertions" />
</ItemGroup>
```

## Handler testability

### Dependency Injection Pattern
- **Always** abstract external dependencies (HTTP clients, file systems, APIs) behind interfaces
- Inject dependencies via constructor
- Avoid direct access to external resources in handlers

❌ **Avoid** - Hard to test:
```csharp
public class MyHandler : TypedResourceHandler<MyProperties, MyIdentifiers>
{
    protected override Task<ExtensibilityOperationSuccessResponse> CreateOrUpdate(...)
    {
        // Direct dependency - can't test without real external system
        var client = new HttpClient();
        // ...
    }
}
```

✅ **Prefer** - Testable with mocks:
```csharp
public class MyHandler : TypedResourceHandler<MyProperties, MyIdentifiers>
{
    private readonly IMyService _service;

    public MyHandler(IMyService service)
    {
        _service = service;
    }

    protected override async Task<ExtensibilityOperationSuccessResponse> CreateOrUpdate(...)
    {
        await _service.DoWorkAsync(...);
        return CreateSuccessResponse(properties, identifiers);
    }
}
```

## Test Class structure

```csharp
[TestClass]
public class MyHandlerTests
{
    private Mock<IMyService> _mockService = null!;
    private MyHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        // Use strict behavior to fail on unexpected calls
        _mockService = new Mock<IMyService>(MockBehavior.Strict);
        _handler = new MyHandler(_mockService.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Verify all setups were invoked
        _mockService.VerifyAll();
    }
}
```

## Test naming convention
- Use descriptive names: `MethodName_Scenario_ExpectedResult`
- Examples:
  - `CreateOrUpdate_WhenResourceExists_UpdatesResource`
  - `Get_WhenResourceNotFound_ThrowsNotFoundException`
  - `Delete_RemovesResourceSuccessfully`

## Arrange-Act-Assert Pattern
- **Arrange**: Set up mocks and test data
- **Act**: Call the method under test
- **Assert**: Verify the result

```csharp
[TestMethod]
public async Task CreateOrUpdate_WritesResourceAndReturnsSuccess()
{
    // Arrange
    var properties = new MyProperties { Name = "Test" };
    var identifiers = new MyIdentifiers { Id = "123" };

    _mockService
        .Setup(s => s.CreateAsync(identifiers.Id, properties.Name, It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

    // Act
    var response = await _handler.CreateOrUpdate(properties, identifiers, CancellationToken.None);

    // Assert
    response.Should().NotBeNull();
    response.Resource.Should().NotBeNull();
}
```

## Testing exceptions

```csharp
[TestMethod]
public async Task CreateOrUpdate_WhenServiceFails_ThrowsException()
{
    // Arrange
    _mockService
        .Setup(s => s.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new InvalidOperationException("Service unavailable"));

    // Act & Assert
    await FluentActions
        .Invoking(() => _handler.CreateOrUpdate(properties, identifiers, CancellationToken.None))
        .Should()
        .ThrowAsync<InvalidOperationException>()
        .WithMessage("Service unavailable");
}
```

## Data-Driven tests
Use `[DataRow]` for testing multiple scenarios:

```csharp
[TestMethod]
[DataRow("project1", "Description 1")]
[DataRow("project-with-dashes", "Another description")]
[DataRow("ProjectWithCaps", "")]
public async Task CreateOrUpdate_HandlesVariousInputs(string name, string description)
{
    // Test implementation
}
```

## Testing Strategy by component

| Component | What to Test | Approach |
|-----------|--------------|----------|
| **Handlers** | Business logic, validation, error handling | Mock all dependencies |
| **Services** | External integrations, API calls | Isolated test environments |
| **Validators** | Input validation rules | Direct instantiation |

## Service integration tests
For services that interact with external systems, use isolated test environments:

```csharp
[TestClass]
public class MyServiceTests
{
    private string _testDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
```

## Running tests

```powershell
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~MyHandlerTests"

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Common FluentAssertions Patterns

```csharp
// Null checks
result.Should().NotBeNull();
result.Should().BeNull();

// Object comparison
result.Should().BeEquivalentTo(expected);

// Collection assertions
results.Should().HaveCount(3);
results.Should().Contain(item);

// String assertions
message.Should().StartWith("Error:");
message.Should().Contain("not found");
```

## Key principles

1. **Use Dependency Injection** — Abstract external dependencies behind interfaces
2. **Apply Loose Coupling** — Handlers depend on abstractions, not implementations
3. **Use Strict Mocking** — Fail on unexpected calls to catch bugs early
4. **Test One Scenario Per Method** — Keep tests focused and readable
5. **Verify All Mocks** — Ensure all expected calls were made
