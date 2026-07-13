using Korone.RccServiceArbiter.Rcc;
using Xunit;

namespace Korone.RccServiceArbiter.Tests;

public sealed class RccSoapEnvelopeTests
{
    private const string ServiceUrl = "pekora.zip";

    [Fact]
    public void OpenJobEx_ContainsJobAndScriptShape()
    {
        var xml = RccSoapEnvelope.ToRequestBody(RccSoapEnvelope.OpenJobEx(
            ServiceUrl,
            new Job
            {
                Id = "job-1",
                Category = 1,
                Cores = 2,
                ExpirationInSeconds = 60,
            },
            new ScriptExecution
            {
                Name = "ZUNA_GAME",
                Script = "{\"Mode\":\"GameServer\"}",
            }));

        Assert.Contains("OpenJobEx", xml);
        Assert.Contains("http://pekora.zip/", xml);
        Assert.DoesNotContain("http://roblox.com/", xml);
        Assert.Contains("<id>job-1</id>", xml);
        Assert.Contains("<expirationInSeconds>60</expirationInSeconds>", xml);
        Assert.Contains("<name>ZUNA_GAME</name>", xml);
        Assert.Contains("<![CDATA[{\"Mode\":\"GameServer\"}]]>", xml);
    }

    [Fact]
    public void ExecuteEx_ContainsJobIdAndScriptShape()
    {
        var xml = RccSoapEnvelope.ToRequestBody(RccSoapEnvelope.ExecuteEx(
            ServiceUrl,
            "job-2",
            RccScriptFactory.EvictPlayer(123, 1)));

        Assert.Contains("ExecuteEx", xml);
        Assert.Contains("http://pekora.zip/", xml);
        Assert.Contains("<jobID>job-2</jobID>", xml);
        Assert.Contains("Evict Player V1", xml);
        Assert.Contains("EvictPlayer", xml);
    }

    [Fact]
    public void CloseJob_ContainsJobId()
    {
        var xml = RccSoapEnvelope.ToRequestBody(RccSoapEnvelope.CloseJob(ServiceUrl, "job-3"));

        Assert.Contains("CloseJob", xml);
        Assert.Contains("http://pekora.zip/", xml);
        Assert.Contains("<jobID>job-3</jobID>", xml);
    }

    [Fact]
    public void SoapAction_UsesConfiguredServiceUrl()
    {
        Assert.Equal("http://pekora.zip/OpenJobEx", RccSoapEnvelope.SoapAction(ServiceUrl, "OpenJobEx"));
    }

    [Fact]
    public void BatchJob_ModernJsonPayload_UsesLegacyBatchJobSoapOperation()
    {
        var xml = RccSoapEnvelope.ToRequestBody(RccSoapEnvelope.BatchJob(ServiceUrl,
            new Job { Id = "render-1", Category = 2, Cores = 1, ExpirationInSeconds = 60 },
            new ScriptExecution { Name = "Avatar", Script = "{\"Mode\":\"Thumbnail\"}" }));
        Assert.Contains("BatchJob", xml); Assert.DoesNotContain("BatchJobEx", xml);
        Assert.Contains("<![CDATA[{\"Mode\":\"Thumbnail\"}]]>", xml);
    }

    [Fact]
    public void BatchJobResponse_ParsesScalarAndDependencyTable()
    {
        const string xml = """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body><BatchJobResponse>
              <BatchJobResult><type>LUA_TSTRING</type><value>aW1hZ2U=</value><table /></BatchJobResult>
              <BatchJobResult><type>LUA_TTABLE</type><value></value><table>
                <LuaValue><type>LUA_TSTRING</type><value>https://example.test/asset</value><table /></LuaValue>
              </table></BatchJobResult>
            </BatchJobResponse></soap:Body></soap:Envelope>
            """;
        var values = RccSoapClient.ParseBatchJobResponse(xml);
        Assert.Equal(2, values.Count); Assert.Equal("aW1hZ2U=", values[0].Value);
        Assert.Single(values[1].Table); Assert.Equal("https://example.test/asset", values[1].Table[0].Value);
    }
}
