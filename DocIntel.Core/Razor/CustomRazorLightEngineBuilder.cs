/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Reflection;

using RazorLight;
using RazorLight.Compilation;
using RazorLight.Generation;

namespace DocIntel.Core.Razor
{
    public class CustomRazorLightEngineBuilder : RazorLightEngineBuilder
    {
        public override RazorLightEngine Build()
        {
            var options = new RazorLightOptions();

            if (namespaces != null) options.Namespaces = namespaces;

            if (dynamicTemplates != null) options.DynamicTemplates = dynamicTemplates;

            if (metadataReferences != null) options.AdditionalMetadataReferences = metadataReferences;

            if (excludedAssemblies != null) options.ExcludedAssemblies = excludedAssemblies;

            if (prerenderCallbacks != null) options.PreRenderCallbacks = prerenderCallbacks;

            if (cachingProvider != null) options.CachingProvider = cachingProvider;

            //options.DisableEncoding = disableEncoding;


            var metadataReferenceManager =
                new DefaultMetadataReferenceManager(options.AdditionalMetadataReferences, options.ExcludedAssemblies);
            var assembly = operatingAssembly ?? Assembly.GetEntryAssembly();
            var compiler = new RoslynCompilationService(metadataReferenceManager, assembly);

            var sourceGenerator = new RazorSourceGenerator(CustomRazorEngine.Instance, project, options.Namespaces);
            var templateCompiler = new RazorTemplateCompiler(sourceGenerator, compiler, project, options);
            var templateFactoryProvider = new TemplateFactoryProvider();

            var engineHandler = new EngineHandler(options, templateCompiler, templateFactoryProvider, cachingProvider);

            return new RazorLightEngine(engineHandler);
        }
    }
}