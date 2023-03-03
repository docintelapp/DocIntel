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
using System.Threading.Tasks;
using DocIntel.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DocIntel.Core.Repositories.EFCore
{
    public class ImportRuleEFRepository : IImportRuleRepository
    {

        public async Task<ImportRuleSet> Create(AmbientContext context, ImportRuleSet importRuleSet)
        {
            var entry = context.DatabaseContext.ImportRuleSets.Add(importRuleSet);
            await context.DatabaseContext.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<ImportRule> Create(AmbientContext context, ImportRule importRule)
        {
            var entry = context.DatabaseContext.ImportRules.Add(importRule);
            await context.DatabaseContext.SaveChangesAsync();
            return entry.Entity;
        }

        public bool Exists(AmbientContext context, Guid importRuleId)
        {
            return context.DatabaseContext.ImportRules.Any(_ => _.ImportRuleId == importRuleId);
        }

        public ImportRule Get(AmbientContext context, Guid importRuleId)
        {
            return context.DatabaseContext.ImportRules.SingleOrDefault(_ => _.ImportRuleId == importRuleId);
        }

        public IEnumerable<ImportRule> GetAll(AmbientContext context, Guid setId)
        {
            return context.DatabaseContext.ImportRules.Where(_ => _.ImportRuleSetId == setId);
        }

        public IEnumerable<ImportRuleSet> GetAllSets(AmbientContext context)
        {
            return context.DatabaseContext.ImportRuleSets.Include(_ => _.ImportRules);
        }

        public ImportRuleSet GetSet(AmbientContext context, Guid importRuleSetId)
        {
            return context.DatabaseContext.ImportRuleSets
                .Include(_ => _.ImportRules)
                .SingleOrDefault(_ => _.ImportRuleSetId == importRuleSetId);
        }

        public async Task RemoveSet(AmbientContext context, Guid importRuleSet)
        {
            context.DatabaseContext.ImportRuleSets.Remove(context.DatabaseContext.ImportRuleSets.SingleOrDefault(_ => _.ImportRuleSetId == importRuleSet));
            await context.DatabaseContext.SaveChangesAsync();
        }

        public async Task Remove(AmbientContext context, Guid importRule)
        {
            context.DatabaseContext.ImportRules.Remove(context.DatabaseContext.ImportRules.SingleOrDefault(_ => _.ImportRuleId == importRule));
            await context.DatabaseContext.SaveChangesAsync();
        }

        public bool SetExists(AmbientContext context, Guid importRuleSetId)
        {
            return context.DatabaseContext.ImportRuleSets.Any(_ => _.ImportRuleSetId == importRuleSetId);
        }

        public async Task<ImportRuleSet> Update(AmbientContext context, ImportRuleSet importRuleSet)
        {
            var res = context.DatabaseContext.ImportRuleSets.Update(importRuleSet);
            await context.DatabaseContext.SaveChangesAsync();
            return res.Entity;
        }

        public async Task<ImportRule> Update(AmbientContext context, ImportRule importRule)
        {
            var res = context.DatabaseContext.ImportRules.Update(importRule);
            await context.DatabaseContext.SaveChangesAsync();
            return res.Entity;
        }
    }
}