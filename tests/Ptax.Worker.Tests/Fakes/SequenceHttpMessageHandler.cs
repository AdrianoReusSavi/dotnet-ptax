using System.Net;
using System.Text;

namespace Ptax.Worker.Tests.Fakes;

public class SequenceHttpMessageHandler(params HttpStatusCode[] statusCodes) : HttpMessageHandler
{
    public int Calls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var statusCode = statusCodes[Math.Min(Calls, statusCodes.Length - 1)];

        Calls++;

        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("""{ "value": [] }""", Encoding.UTF8, "application/json")
        });
    }
}