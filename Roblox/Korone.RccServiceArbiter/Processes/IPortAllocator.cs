using Korone.RccServiceArbiter.Configuration;

namespace Korone.RccServiceArbiter.Processes;

public interface IPortAllocator
{
    int Allocate(PortRange range);
    void Release(int port);
}
