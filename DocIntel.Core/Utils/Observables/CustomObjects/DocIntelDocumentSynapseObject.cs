using System;
using Synsharp;
using Synsharp.Attribute;
using Synsharp.Types;

namespace DocIntel.Core.Utils.Observables.CustomObjects;

/// <summary>
/// Represents a DocIntel document in a Synapse server.
/// </summary>
[SynapseForm("_di:document")]
public class DIDocumentSynapseObject: SynapseObject<Str>
{
    public DIDocumentSynapseObject(Guid documentDocumentId)
    {
        SetValue(documentDocumentId.ToString());
    }

    public DIDocumentSynapseObject()
    {
            
    }
}