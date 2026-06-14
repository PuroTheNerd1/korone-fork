using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace Korone.RccServiceArbiter.Rcc;

public sealed class RccSoapClient : IRccSoapClient
{
    private readonly HttpClient _httpClient;
    private readonly Uri _endpoint;
    private readonly string _serviceUrl;

    public RccSoapClient(HttpClient httpClient, Uri endpoint, string serviceUrl)
    {
        _httpClient = httpClient;
        _endpoint = endpoint;
        _serviceUrl = serviceUrl;
    }

    public Task OpenJobExAsync(Job job, ScriptExecution script, CancellationToken cancellationToken)
    {
        return SendAsync("OpenJobEx", RccSoapEnvelope.OpenJobEx(_serviceUrl, job, script), cancellationToken);
    }

    public Task ExecuteExAsync(string jobId, ScriptExecution script, CancellationToken cancellationToken)
    {
        return SendAsync("ExecuteEx", RccSoapEnvelope.ExecuteEx(_serviceUrl, jobId, script), cancellationToken);
    }

    public Task CloseJobAsync(string jobId, CancellationToken cancellationToken)
    {
        return SendAsync("CloseJob", RccSoapEnvelope.CloseJob(_serviceUrl, jobId), cancellationToken);
    }

    public async Task<IReadOnlyList<RccServiceJob>> GetAllJobsAsync(CancellationToken cancellationToken)
    {
        var response = await SendForResponseAsync("GetAllJobs", RccSoapEnvelope.GetAllJobs(_serviceUrl), cancellationToken);
        if (string.IsNullOrWhiteSpace(response))
        {
            return Array.Empty<RccServiceJob>();
        }

        var document = XDocument.Parse(response);
        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "Job")
            .Select(element => new RccServiceJob
            {
                Id = element.Elements().FirstOrDefault(child => child.Name.LocalName == "id")?.Value ?? string.Empty,
            })
            .Where(job => !string.IsNullOrWhiteSpace(job.Id))
            .ToList();
    }

    private async Task SendAsync(string action, XDocument document, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(action, document);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> SendForResponseAsync(string action, XDocument document, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(action, document);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private HttpRequestMessage CreateRequest(string action, XDocument document)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
        request.Headers.TryAddWithoutValidation("SOAPAction", RccSoapEnvelope.SoapAction(_serviceUrl, action));
        request.Content = new StringContent(RccSoapEnvelope.ToRequestBody(document), Encoding.UTF8);
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml; charset=utf-8");
        return request;
    }
}
