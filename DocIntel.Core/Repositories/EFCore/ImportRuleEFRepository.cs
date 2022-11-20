/* DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau
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

        public async Task Create(AmbientContext context, ImportRuleSet importRuleSet, AppUser appUser)
        {
            context.DatabaseContext.ImportRuleSets.Add(importRuleSet);
            await context.DatabaseContext.SaveChangesAsync();
        }

        public async Task Create(AmbientContext context, ImportRule importRule, AppUser appUser)
        {
            context.DatabaseContext.ImportRules.Add(importRule);
            await context.DatabaseContext.SaveChangesAsync();
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
            return context.DatabaseContext.ImportRules.Where(_ => _.ImportRuleId == setId);
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

        public async Task Remove(AmbientContext context, ImportRuleSet importRuleSet, AppUser appUser)
        {
            context.DatabaseContext.ImportRuleSets.Remove(importRuleSet);
            await context.DatabaseContext.SaveChangesAsync();
        }

        public async Task Remove(AmbientContext context, ImportRule importRule, AppUser appUser)
        {
            context.DatabaseContext.ImportRules.Remove(importRule);
            await context.DatabaseContext.SaveChangesAsync();
        }

        public bool SetExists(AmbientContext context, Guid importRuleSetId)
        {
            return context.DatabaseContext.ImportRuleSets.Any(_ => _.ImportRuleSetId == importRuleSetId);
        }

        public async Task Update(AmbientContext context, ImportRuleSet importRuleSet, AppUser appUser)
        {
            context.DatabaseContext.ImportRuleSets.Update(importRuleSet);
            await context.DatabaseContext.SaveChangesAsync();
        }

        public async Task Update(AmbientContext context, ImportRule importRule, AppUser appUser)
        {
            context.DatabaseContext.ImportRules.Update(importRule);
            await context.DatabaseContext.SaveChangesAsync();
        }
    }
}