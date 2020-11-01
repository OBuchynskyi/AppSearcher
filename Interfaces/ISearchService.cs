using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppSearcher.Interfaces
{
    public interface ISearchService
    {
        Task<IEnumerable<PageSerachResult>> StartKeywordSearchAsync();
    }
}