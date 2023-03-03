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

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;

using RazorLight.Instrumentation;

using InjectDirective = RazorLight.Instrumentation.InjectDirective;
using ModelDirective = RazorLight.Instrumentation.ModelDirective;

namespace DocIntel.Core.Razor
{
    public class CustomRazorEngine
    {
        public static RazorEngine Instance
        {
            get
            {
                var configuration = RazorConfiguration.Default;
                var razorProjectEngine = RazorProjectEngine.Create(configuration, new NullRazorProjectFileSystem(),
                    builder =>
                    {
                        InjectDirective.Register(builder);
                        ModelDirective.Register(builder);

                        //In RazorLanguageVersion > 3.0 (at least netcore 3.0) the directives are registered out of the box.
                        if (!RazorLanguageVersion.TryParse("3.0", out var razorLanguageVersion)
                            || configuration.LanguageVersion.CompareTo(razorLanguageVersion) < 0)
                        {
                            NamespaceDirective.Register(builder);
                            FunctionsDirective.Register(builder);
                            InheritsDirective.Register(builder);
                            SectionDirective.Register(builder);
                        }

                        var sectionDirective = builder.Features.OfType<SectionDirectivePass>().FirstOrDefault();
                        if (sectionDirective == null) SectionDirective.Register(builder);

                        builder.Features.Add(new ModelExpressionPass());
                        builder.Features.Add(new RazorLightTemplateDocumentClassifierPass());
                        builder.Features.Add(new RazorLightAssemblyAttributeInjectionPass());
#if NETSTANDARD2_0
				   builder.Features.Add(new InstrumentationPass());
#endif
                        //builder.Features.Add(new ViewComponentTagHelperPass());

                        builder.AddTargetExtension(new TemplateTargetExtension
                        {
                            TemplateTypeName = "global::RazorLight.Razor.RazorLightHelperResult"
                        });

                        OverrideRuntimeNodeWriterTemplateTypeNamePhase.Register(builder);
                    });

                return razorProjectEngine.Engine;
            }
        }

        private class NullRazorProjectFileSystem : RazorProjectFileSystem
        {
            public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
            {
                throw new NotImplementedException();
            }


#if (NETCOREAPP3_0 || NETCOREAPP3_1)
            [Obsolete]
#endif
            public override RazorProjectItem GetItem(string path)
            {
                throw new NotImplementedException();
            }

#if (NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0 || NET6_0 || NET7_0)
            public override RazorProjectItem GetItem(string path, string fileKind)
            {
                throw new NotImplementedException();
            }
#endif
        }
    }
}