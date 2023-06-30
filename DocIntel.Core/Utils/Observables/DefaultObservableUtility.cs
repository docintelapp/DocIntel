using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Models;
using DocIntel.Core.Utils.Observables.Extractors;
using DocIntel.Core.Utils.Observables.PostProcessors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synsharp.Telepath.Messages;

namespace DocIntel.Core.Utils.Observables;

public class DefaultObservableUtility : IObservablesUtility
{
    private static readonly List<Type> Transforms = new()
    {
        typeof(LigatureRemover)
    };

    private static readonly List<Type> Extractors = new()
    {
        typeof(RegexDomainExtractor),
        typeof(RegexHashExtractor),
        typeof(RegexIpExtractor),
        typeof(RegexUrlExtractor)
    };

    private static readonly List<Type> PostProcessors = new()
    {
        typeof(SuspiciousTld)
    };

    private readonly List<IExtractor> _extractors;
    private readonly ILogger<DefaultObservableUtility> _logger;
    private readonly List<IPostProcessor> _postProcessors;
    private readonly List<ITextTransform> _transforms;

    public DefaultObservableUtility(IServiceProvider serviceProvider, ILogger<DefaultObservableUtility> logger)
    {
        _logger = logger;
        // Take advantage of dynamic injection
        _extractors = Extractors.Select(_ => (IExtractor) serviceProvider.GetRequiredService(_)).ToList();
        _transforms = Transforms.Select(_ => (ITextTransform) serviceProvider.GetRequiredService(_)).ToList();
        _postProcessors = PostProcessors.Select(_ => (IPostProcessor) serviceProvider.GetRequiredService(_)).ToList();
    }

    public async IAsyncEnumerable<SynapseNode> ExtractDataAsync(Document document, DocumentFile file, string content)
    {
        _logger.LogDebug("Running observable text transforms");
        foreach (var transform in _transforms)
        {
            _logger.LogTrace($"Running observable text transform '{transform.GetType().FullName}'");
            content = transform.Transform(content);
        }

        _logger.LogDebug("Running extractors on extracted text");
        foreach (var extractor in _extractors)
        {
            _logger.LogTrace($"Running extractors '{extractor.GetType().FullName}' on extracted text");
            var observables = extractor.Extract(content);

            await foreach (var observable in observables)
            {
                yield return observable;
            }
        }   
        _logger.LogDebug("Extraction complete");
    }

    public async Task AnnotateAsync(Document document, DocumentFile file, IEnumerable<SynapseNode> objects)
    {
        _logger.LogDebug($"Running {_postProcessors.Count} post processor on extracted observables");
        foreach (var postProcessor in _postProcessors)
        {
            _logger.LogDebug($"Running post processor '{postProcessor}' on observables");
            await postProcessor.Process(objects);
        }
        _logger.LogDebug("Annotation complete");
    }
}
