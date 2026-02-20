using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace PibesDelDestino.Metrics
{
    public class SearchHistory : CreationAuditedEntity<Guid>
    {
        public string Term { get; set; }      
        public int ResultCount { get; set; } 

        protected SearchHistory() { } // EF Core

        public SearchHistory(Guid id, string term, int resultCount) : base(id)
        {
            Term = term;
            ResultCount = resultCount;
        }
    }
}
