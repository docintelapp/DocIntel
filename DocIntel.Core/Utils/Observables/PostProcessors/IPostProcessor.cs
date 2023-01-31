using System.Collections.Generic;
using System.Threading.Tasks;
using Synsharp.Telepath.Messages;

namespace DocIntel.Core.Utils.Observables;

public interface IPostProcessor
{
    Task Process(IEnumerable<SynapseNode> objects);
}