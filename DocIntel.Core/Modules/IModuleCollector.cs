using DocIntel.Core.Models;

namespace DocIntel.Core.Modules;

public interface IModuleCollector
{
    SubmittedDocument Collect();
    void Collect(SubmittedDocument submitted);
}