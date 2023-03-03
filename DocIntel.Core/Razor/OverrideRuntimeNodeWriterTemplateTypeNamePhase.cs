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

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using RazorLight;

namespace DocIntel.Core.Razor
{
    public class OverrideRuntimeNodeWriterTemplateTypeNamePhase : RazorEnginePhaseBase
    {
        private readonly string _templateTypeName;

        public OverrideRuntimeNodeWriterTemplateTypeNamePhase(string templateTypeName)
        {
            _templateTypeName = templateTypeName;
        }

        public static void Register(RazorProjectEngineBuilder builder)
        {
            var defaultRazorCSharpLoweringPhase = builder.Phases.SingleOrDefault(x =>
            {
                var type = x.GetType();
                // This type is not public, so we can't use typeof() operator to match to x.GetType() value.
                // Additionally, we can't use Type.GetType("Microsoft.AspNetCore.Razor.Language.DefaultRazorCSharpLoweringPhase, Microsoft.AspNetCore.Razor.Language")
                // because apparently it can fail during Azure Functions rolling upgrades? Per user report: https://github.com/toddams/RazorLight/issues/322
                var assemblyQualifiedNameOfTypeWeCareAbout =
                    "Microsoft.AspNetCore.Razor.Language.DefaultRazorCSharpLoweringPhase, Microsoft.AspNetCore.Razor.Language, ";
                return type.AssemblyQualifiedName.Substring(0, assemblyQualifiedNameOfTypeWeCareAbout.Length) ==
                       assemblyQualifiedNameOfTypeWeCareAbout;
            });

            if (defaultRazorCSharpLoweringPhase == null)
                throw new RazorLightException(
                    "SetTemplateTypePhase cannot be registered as DefaultRazorCSharpLoweringPhase could not be located");

            // This phase needs to run just before DefaultRazorCSharpLoweringPhase
            var phaseIndex = builder.Phases.IndexOf(defaultRazorCSharpLoweringPhase);
            builder.Phases.Insert(phaseIndex,
                new OverrideRuntimeNodeWriterTemplateTypeNamePhase("global::RazorLight.Razor.RazorLightHelperResult"));
        }

        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var documentNode = codeDocument.GetDocumentIntermediateNode();
            ThrowForMissingDocumentDependency(documentNode);

            documentNode.Target =
                new RuntimeNodeWriterTemplateTypeNameCodeTarget(documentNode.Target, _templateTypeName);
        }

        internal class RuntimeNodeWriterTemplateTypeNameCodeTarget : CodeTarget
        {
            private readonly CodeTarget _target;
            private readonly string _templateTypeName;

            public RuntimeNodeWriterTemplateTypeNameCodeTarget(CodeTarget target, string templateTypeName)
            {
                _target = target;
                _templateTypeName = templateTypeName;
            }

            public override IntermediateNodeWriter CreateNodeWriter()
            {
                var writer = _target.CreateNodeWriter();
                if (writer is RuntimeNodeWriter runtimeNodeWriter)
                    runtimeNodeWriter.TemplateTypeName = _templateTypeName;

                return writer;
            }

            public override TExtension GetExtension<TExtension>()
            {
                return _target.GetExtension<TExtension>();
            }

            public override bool HasExtension<TExtension>()
            {
                return _target.HasExtension<TExtension>();
            }
        }
    }
}