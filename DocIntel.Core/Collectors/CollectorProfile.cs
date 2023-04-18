using AutoMapper;
using DocIntel.Core.Models;
using Synsharp.Telepath.Messages;

namespace DocIntel.Core.Collectors;

public class CollectorProfile : Profile
{
    public CollectorProfile()
    {
        CreateMap<DocumentImport, Document>()
            .ForMember(_ => _.DocumentTags, _ => _.Ignore());
        CreateMap<FileImport, DocumentFile>();
        CreateMap<NodeImport, SynapseNode>();
    }
}