using System.Collections.Generic;
using System.Threading.Tasks;
using Synsharp;

namespace DocIntel.Core.Utils.Observables;

public interface IPostProcessor
{
    Task Process(IEnumerable<SynapseObject> objects);
}