using Korone.RccServiceArbiter.Rcc;
using Roblox.Rendering;

namespace Korone.RccServiceArbiter.Rendering;

public interface IRenderScriptCatalog
{
    ScriptExecution Create(RenderRequest request);
}

