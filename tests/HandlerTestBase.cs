using System.Net.Http.Headers;
using System.Text;
using DevOpsExtension.Tests.Helpers;

namespace DevOpsExtension.Tests;

/// <summary>
/// Base class for handler tests providing common setup and helper methods.
/// </summary>
public abstract class HandlerTestBase
{
    protected MockHttpMessageHandler MockHandler = null!;
    protected Configuration TestConfiguration = null!;

    [TestInitialize]
    public virtual void Setup()
    {
        MockHandler = new MockHttpMessageHandler();
        TestConfiguration = new Configuration
        {
            AccessToken = "test-pat-token"
        };
    }

    [TestCleanup]
    public virtual void Cleanup()
    {
        MockHandler = null!;
    }

    /// <summary>
    /// Creates an HttpClient configured with the mock handler and basic auth header.
    /// </summary>
    protected HttpClient CreateMockedClient()
    {
        var client = new HttpClient(MockHandler);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{TestConfiguration.AccessToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        return client;
    }

    /// <summary>
    /// Verifies that a request was made to the specified URL path.
    /// </summary>
    protected void VerifyRequestMade(string urlContains, HttpMethod? method = null)
    {
        var request = MockHandler.Requests.FirstOrDefault(r => 
            r.RequestUri?.ToString().Contains(urlContains, StringComparison.OrdinalIgnoreCase) == true);
        
        request.Should().NotBeNull($"Expected a request containing '{urlContains}' but none was found. Requests made: {string.Join(", ", MockHandler.Requests.Select(r => r.RequestUri?.ToString()))}");
        
        if (method != null)
        {
            request!.Method.Should().Be(method);
        }
    }

    /// <summary>
    /// Gets the request body as a string.
    /// </summary>
    protected async Task<string?> GetRequestBodyAsync(int requestIndex = 0)
    {
        if (requestIndex >= MockHandler.Requests.Count)
        {
            return null;
        }

        var request = MockHandler.Requests[requestIndex];
        if (request.Content == null)
        {
            return null;
        }

        return await request.Content.ReadAsStringAsync();
    }
}
