using System.Net;
using System.Text;

namespace Demo.IntegrationTests.TestDoubles;

/// <summary>Answers HTTP requests with queued responses and records what was sent.</summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Status, string Body)> _responses = new();

    public List<HttpRequestMessage> Requests { get; } = [];

    public StubHttpMessageHandler Respond(HttpStatusCode status, string body)
    {
        _responses.Enqueue((status, body));
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        // The last queued response is repeated once the queue is down to one.
        var (status, body) = _responses.Count > 1 ? _responses.Dequeue() : _responses.Peek();

        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
    }
}
