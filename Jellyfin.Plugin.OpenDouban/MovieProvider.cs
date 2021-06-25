using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.OpenDouban.Service;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.OpenDouban
{
    public class MovieProvider : IHasOrder, IRemoteMetadataProvider<Movie, MovieInfo>
    {
        public string Name => "Open Douban Movie Provider";
        public int Order => 3;

        private ApiClient apiClient;
        private ILogger logger;
        private IHttpClientFactory httpClientFactory;

        public MovieProvider(IHttpClientFactory httpClientFactory, IJsonSerializer jsonSerializer, ILogger<MovieProvider> logger)
        {
            this.apiClient = new ApiClient(httpClientFactory, jsonSerializer);
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            ApiSubject subject = null;
            

            string sid = info.GetProviderId(OpenDoubanPlugin.ProviderID);
            if (!string.IsNullOrEmpty(sid))
            {
                logger.LogInformation($"[Open DOUBAN] GetMetadata of [sid]: \"{sid}\"");
                subject = await apiClient.GetBySid(sid);
            }
            else if (!string.IsNullOrEmpty(info.Name))
            {
                string pattern = OpenDoubanPlugin.Instance?.Configuration.Pattern;
                string name = Regex.Replace(info.Name, pattern, "");
                List<ApiSubject> res = await apiClient.PartialSearch(name);
                
                // Getting 1st item from the result
                var has = res;
                if (has.Any())
                {
                    sid = has.FirstOrDefault().Sid;
                    subject = await apiClient.GetBySid(sid);
                }
            }

            var result = new MetadataResult<Movie>();
            if (subject == null) return result;

            var x = subject;

            result.Item = new Movie
            {
                ProviderIds = new Dictionary<string, string> { { OpenDoubanPlugin.ProviderID, x.Sid } },
                Name = x?.Name,
                OriginalTitle = x?.Subname,
                CommunityRating = x?.Rating,
                Overview = x?.Intro,
                ProductionYear = x?.Year,
                HomePageUrl = "https://www.douban.com",
                // ProductionLocations = [x?.Country],
                // PremiereDate = null,   
            };

            info.SetProviderId(OpenDoubanPlugin.ProviderID, x.Sid);
            if(!string.IsNullOrEmpty(x.Imdb)) {
                info.SetProviderId(MetadataProvider.Imdb, x.Imdb);
            }
            
            result.QueriedById = true;
            result.HasMetadata = true;

            x.Celebrities = await apiClient.GetCelebritiesBySid(sid);
            
            x.Celebrities.ForEach(c => result.AddPerson(new MediaBrowser.Controller.Entities.PersonInfo
            {
                Name = c.Name,
                Type = c.Role.Equals("导演") ? PersonType.Director : c.Role.Equals("演员") ? PersonType.Actor : c.Role,
                Role = c.Role,
                ImageUrl = c.Img,
                ProviderIds = new Dictionary<string, string> { { OpenDoubanPlugin.ProviderID, c.Id } },
            }));

            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo info, CancellationToken cancellationToken)
        {
            List<ApiSubject> list = new List<ApiSubject>();

            string sid = info.GetProviderId(OpenDoubanPlugin.ProviderID);
            if (!string.IsNullOrEmpty(sid))
            {
                logger.LogInformation($"[Open DOUBAN] GetSearchResults of [sid]: \"{sid}\"");
                ApiSubject res = await apiClient.GetBySid(sid);
                list.Add(res);
            }
            else if (!string.IsNullOrEmpty(info.Name))
            {
                logger.LogInformation($"[Open DOUBAN] GetSearchResults of [name]: \"{info.Name}\"");
                List<ApiSubject> res = await apiClient.PartialSearch(info.Name);
                list.AddRange(res);
            }

            if (!list.Any())
            {
                logger.LogInformation($"[Open DOUBAN] GetSearchResults Found Nothing...");
            }

            return list.Select(x =>
            {
                // int year = 0; int.TryParse(x?.Year, out year);
                return new RemoteSearchResult
                {
                    ProviderIds = new Dictionary<string, string> { { OpenDoubanPlugin.ProviderID, x.Sid } },
                    ImageUrl = x?.Img,
                    ProductionYear = x?.Year,
                    Name = x?.Name
                };
            });
        }

        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            logger.LogInformation("[DOUBAN] GetImageResponse url: {0}", url);
            HttpResponseMessage response = await httpClientFactory.CreateClient().GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return response;
        }
    }
}
