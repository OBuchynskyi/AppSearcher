
using AppSearcher.Common;
using AppSearcher.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AppSearcher.Services
{
    public class SearchService : ISearchService
    {
        private readonly ILogger<SearchService> _logger;
        private readonly SearchParameters _searchParameters;
        private ConcurrentBag<PageSerachResult> _searchResult = new ConcurrentBag<PageSerachResult>();
        private List<string> urls = new List<string>();
        private List<string> urlsUsedForGathering = new List<string>();
        private readonly HttpClient _httpClient;
        private static readonly object LockObject = new object();

        public SearchService(ILogger<SearchService> logger, IHttpClientFactory httpClientFactory, IOptions<SearchParameters> searchParameters)
        {
            _logger = logger;
            _searchParameters = searchParameters.Value;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IEnumerable<PageSerachResult>> StartKeywordSearchAsync()
        {
            urls.Add(_searchParameters.StartUrl);            
            await GatherUrlsAsync(urls.First());
            _logger.LogInformation("{count} urls was found", urls.Count());

            var progress = new Progress<int>();
            progress.ProgressChanged += DisplaySearchProgress;

            Console.Write($"Search is in process percentage of completed: ");
            
            await urls.ForEachAsyncPartitioner(35, url => CheckPageForKeywordAsync(url, progress));

            return _searchResult;
        }

        private void DisplaySearchProgress(object sender, int percentageCompleted)
        {
            lock (LockObject)
            {
                Console.Write($"{percentageCompleted:D2}%");
                if (percentageCompleted != 100)
                {
                    Console.SetCursorPosition(Console.CursorLeft - 3, Console.CursorTop);
                }
                else
                {
                    Console.WriteLine();
                }
            }
        }

        private async Task CheckPageForKeywordAsync(string url, IProgress<int> progress)
        {
            var result = await _httpClient.GetAsync(url);
            if (result.IsSuccessStatusCode)
            {
                var content = Encoding.UTF8.GetString(await result.Content.ReadAsByteArrayAsync());
                bool matchResult = false;

                if (!string.IsNullOrEmpty(content))
                {
                    matchResult = Regex.IsMatch(content,
                        $"\\W{_searchParameters.KeyWord}\\W",
                        RegexOptions.Singleline);
                }

                _searchResult.Add(new PageSerachResult()
                {
                    Url = url,
                    SerachStatus = matchResult ? Statuses.Found : Statuses.NotFound
                });
            }
            else
            {
                _searchResult.Add(new PageSerachResult()
                {
                    Url = url,
                    SerachStatus = Statuses.Error,
                    Error = result.StatusCode.ToString()
                });
            }

            progress.Report((_searchResult.Count * 100) / urls.Count);
        }

        private async Task GatherUrlsAsync(string startUrl)
        {
            if (string.IsNullOrEmpty(startUrl)) throw new ArgumentException($"Invalid parameter: {nameof(startUrl)}");

            urlsUsedForGathering.Add(startUrl);

            var result = await _httpClient.GetAsync(startUrl);
            if (result.IsSuccessStatusCode)
            {
                var content = Encoding.UTF8.GetString(await result.Content.ReadAsByteArrayAsync());
                var matchCollection = Regex.Matches(content,
                                        "href=\"((https?://|/).*?)\"",
                                        RegexOptions.Singleline);

                foreach (Match match in matchCollection)
                {
                    var resultUrl = match.Groups[1].Value;
                    var absoluteUrl = resultUrl.StartsWith("http") ? resultUrl : $"{startUrl}{resultUrl}";
                    if (!urls.Contains(absoluteUrl))
                    {
                        urls.Add(absoluteUrl);

                        if (urls.Count >= _searchParameters.UrlsToScan)
                        {
                            break;
                        }
                    }
                }
            }

            if (urls.Count < _searchParameters.UrlsToScan)
            {
                var nextUrlToScan = urls.Where(url => !urlsUsedForGathering.Contains(url)).FirstOrDefault();
                if (!string.IsNullOrEmpty(nextUrlToScan))
                {
                    await GatherUrlsAsync(nextUrlToScan);
                }
            }
        }
    }
}
