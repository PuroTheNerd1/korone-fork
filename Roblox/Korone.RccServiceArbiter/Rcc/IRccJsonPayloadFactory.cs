using Korone.RccServiceArbiter.Models;

namespace Korone.RccServiceArbiter.Rcc;

public interface IRccJsonPayloadFactory
{
    string CreateGameServerPayload(StartGameServerRequest request, int gameServerPort);
}
