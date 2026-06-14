namespace Korone.RccServiceArbiter.Rcc;

public interface IRccSoapClient
{
    Task OpenJobExAsync(Job job, ScriptExecution script, CancellationToken cancellationToken);
    Task ExecuteExAsync(string jobId, ScriptExecution script, CancellationToken cancellationToken);
    Task CloseJobAsync(string jobId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RccServiceJob>> GetAllJobsAsync(CancellationToken cancellationToken);
}

public interface IRccSoapClientFactory
{
    IRccSoapClient Create(int port);
}
