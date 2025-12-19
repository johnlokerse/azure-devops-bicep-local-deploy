using System.Net;
using System.Text;
using System.Text.Json;

namespace DevOpsExtension.Tests.Helpers;

/// <summary>
/// A mock HTTP message handler that allows setting up expected requests and responses for testing.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<MockHttpResponse> _responses = new();
    private readonly List<HttpRequestMessage> _requests = new();

    /// <summary>
    /// Gets the list of requests that were made to this handler.
    /// </summary>
    public IReadOnlyList<HttpRequestMessage> Requests => _requests;

    /// <summary>
    /// Queues a response to be returned for the next request.
    /// </summary>
    public MockHttpMessageHandler WithResponse(HttpStatusCode statusCode, object? content = null, Dictionary<string, string>? headers = null)
    {
        var responseContent = content switch
        {
            null => string.Empty,
            string s => s,
            _ => JsonSerializer.Serialize(content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        };

        _responses.Enqueue(new MockHttpResponse(statusCode, responseContent, headers));
        return this;
    }

    /// <summary>
    /// Queues a successful JSON response.
    /// </summary>
    public MockHttpMessageHandler WithJsonResponse(object content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return WithResponse(statusCode, content);
    }

    /// <summary>
    /// Queues a not found response.
    /// </summary>
    public MockHttpMessageHandler WithNotFound(string? message = null)
    {
        return WithResponse(HttpStatusCode.NotFound, message ?? "Not found");
    }

    /// <summary>
    /// Queues an error response.
    /// </summary>
    public MockHttpMessageHandler WithError(HttpStatusCode statusCode, string message)
    {
        return WithResponse(statusCode, new { message });
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(request);

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException($"No mock response configured for request: {request.Method} {request.RequestUri}");
        }

        var mockResponse = _responses.Dequeue();
        var response = new HttpResponseMessage(mockResponse.StatusCode)
        {
            Content = new StringContent(mockResponse.Content, Encoding.UTF8, "application/json"),
            RequestMessage = request
        };

        if (mockResponse.Headers != null)
        {
            foreach (var header in mockResponse.Headers)
            {
                response.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return Task.FromResult(response);
    }

    private record MockHttpResponse(HttpStatusCode StatusCode, string Content, Dictionary<string, string>? Headers);
}
