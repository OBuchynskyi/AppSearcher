using AppSearcher.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AppSearcher
{
    public class Searcher
    {
        private readonly ILogger<Searcher> _logger;
        private readonly SearchParameters _searchParameters;
        private ISearchService _searchService;

        public Searcher(
            ISearchService searchService,
            ILogger<Searcher> logger,
            IOptions<SearchParameters> searchParameters)
        {
            _searchService = searchService;
            _logger = logger;
            _searchParameters = searchParameters.Value;
        }

        public async Task RunAsync()
        {
            _logger.LogInformation("Search is started from {startUrl} to find {keyWord}, Urls to scan: {urlsToScan}", _searchParameters.StartUrl, _searchParameters.KeyWord, _searchParameters.UrlsToScan);
                        
            var searchResult = await _searchService.StartKeywordSearchAsync();
            var searchResultFoundStatus = searchResult.Where(result => result.SerachStatus == Statuses.Found);

            _logger.LogInformation("Search was completed");

            if (searchResultFoundStatus.Any())
            {
                _logger.LogInformation("Keyword was found in following Urls:");
                foreach (var result in searchResultFoundStatus)
                {
                    _logger.LogInformation("{Urls}", result.Url);
                }
            }
            else
            {
                _logger.LogInformation("No matches with provided keyword");
            }
        }
    }
}
