namespace Korone.RccServiceArbiter.Rcc;

using Korone.RccServiceArbiter.Configuration;
using Microsoft.Extensions.Options;

public sealed class RccSoapClientFactory : IRccSoapClientFactory
{
    private readonly HttpClient _httpClient;
    private readonly ArbiterOptions _options;

    public RccSoapClientFactory(HttpClient httpClient, IOptions<ArbiterOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public IRccSoapClient Create(int port)
    {
        return new RccSoapClient(_httpClient, new Uri($"http://127.0.0.1:{port}/"), ResolveServiceUrl());
    }

    private string ResolveServiceUrl()
    {
        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            return _options.ServiceUrl;
        }

        if (Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var uri))
        {
            return uri.Host;
        }

        return _options.BaseUrl.Trim().TrimEnd('/');
    }
}
