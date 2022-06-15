using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using MediaBrowser.Model.Serialization;
using System.Threading.Tasks;
using System.Threading;

namespace Jellyfin.Plugin.OpenDouban
{
    /// <summary>
    /// OddbApiClient.
    /// </summary>
    public sealed class OddbApiClient
    {
        private IHttpClientFactory httpClientFactory;
        private IJsonSerializer jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="OddbApiClient"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="jsonSerializer">Instance of the <see cref="IJsonSerializer"/> interface.</param>
        public OddbApiClient(IHttpClientFactory httpClientFactory, IJsonSerializer jsonSerializer)
        {
            this.httpClientFactory = httpClientFactory;
            this.jsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// ApiBaseUri
        /// </summary>
        private string _apiBaseUri;
        public string ApiBaseUri
        {
            get
            {
                if (string.IsNullOrEmpty(_apiBaseUri))
                {
                    return OddbPlugin.Instance?.Configuration.ApiBaseUri;
                }
                return _apiBaseUri;
            }
            set { _apiBaseUri = value; }
        }

        public async Task<List<ApiSubject>> FullSearch(string keyword, CancellationToken cancellationToken = default)
        {
            string url = $"{ApiBaseUri}/movies?q={keyword}&type=full";

            HttpResponseMessage response = await httpClientFactory.CreateClient().GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            Stream content = await response.Content.ReadAsStreamAsync(cancellationToken);
            List<ApiSubject> result = await jsonSerializer.DeserializeFromStreamAsync<List<ApiSubject>>(content);
            return result;
        }

        public async Task<List<ApiSubject>> PartialSearch(string keyword, CancellationToken cancellationToken = default)
        {
            string url = $"{ApiBaseUri}/movies?q={keyword}&type=partial";

            HttpResponseMessage response = await httpClientFactory.CreateClient().GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            Stream content = await response.Content.ReadAsStreamAsync(cancellationToken);
            List<ApiSubject> result = await jsonSerializer.DeserializeFromStreamAsync<List<ApiSubject>>(content);
            return result;
        }

        public async Task<ApiSubject> GetBySid(string sid, CancellationToken cancellationToken = default)
        {
            return await GetBySidWithOptions(sid, new Dictionary<string, string>(), cancellationToken);
        }

        public async Task<ApiSubject> GetBySidWithOptions(string sid, Dictionary<string, string> options, CancellationToken cancellationToken = default)
        {
            var posterSize = options.FirstOrDefault(x => x.Key.Equals("PosterSize")).Value;
            string url = $"{ApiBaseUri}/movies/{sid}?s={posterSize}";

            HttpResponseMessage response = await httpClientFactory.CreateClient().GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            Stream content = await response.Content.ReadAsStreamAsync(cancellationToken);
            ApiSubject result = await jsonSerializer.DeserializeFromStreamAsync<ApiSubject>(content);
            return result;
        }

        public async Task<List<ApiCelebrity>> GetCelebritiesBySid(string sid, CancellationToken cancellationToken = default)
        {
            string url = $"{ApiBaseUri}/movies/{sid}/celebrities";

            HttpResponseMessage response = await httpClientFactory.CreateClient().GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new List<ApiCelebrity>();
            }
            Stream content = await response.Content.ReadAsStreamAsync(cancellationToken);
            List<ApiCelebrity> result = await jsonSerializer.DeserializeFromStreamAsync<List<ApiCelebrity>>(content);
            return result;
        }

        public async Task<ApiCelebrity> GetCelebrityByCid(string cid, CancellationToken cancellationToken = default)
        {
            string url = $"{ApiBaseUri}/celebrities/{cid}";

            HttpResponseMessage response = await httpClientFactory.CreateClient().GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            Stream content = await response.Content.ReadAsStreamAsync(cancellationToken);
            ApiCelebrity result = await jsonSerializer.DeserializeFromStreamAsync<ApiCelebrity>(content);
            return result;
        }

        public async Task<List<ApiPhoto>> GetPhotoBySid(string sid, CancellationToken cancellationToken = default)
        {
            string url = $"{ApiBaseUri}/photo/{sid}";

            HttpResponseMessage response = await httpClientFactory.CreateClient().GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            Stream content = await response.Content.ReadAsStreamAsync(cancellationToken);
            List<ApiPhoto> result = await jsonSerializer.DeserializeFromStreamAsync<List<ApiPhoto>>(content);
            return result;
        }
    }

    public class ApiSubject
    {
        // "name": "哈利·波特与魔法石",
        public string Name { get; set; }
        // "originalName": "Harry Potter and the Sorcerer's Stone",
        public string OriginalName { get; set; }
        // "rating": "9.1",
        public float Rating { get; set; }
        // "img": "https://img9.doubanio.com/view/photo/s_ratio_poster/public/p2614949805.webp",
        public string Img { get; set; }
        // "sid": "1295038",
        public string Sid { get; set; }
        // "year": "2001",
        public int Year { get; set; }
        // "director": "克里斯·哥伦布",
        public string Director { get; set; }
        // "writer": "史蒂夫·克洛夫斯 / J·K·罗琳",
        public string Writer { get; set; }
        // "actor": "丹尼尔·雷德克里夫 / 艾玛·沃森 / 鲁伯特·格林特 / 艾伦·瑞克曼 / 玛吉·史密斯 / 更多...",
        public string Actor { get; set; }
        // "genre": "奇幻 / 冒险",
        public string Genre { get; set; }
        // "site": "www.harrypotter.co.uk",
        public string Site { get; set; }
        // "country": "美国 / 英国",
        public string Country { get; set; }
        // "language": "英语",
        public string Language { get; set; }
        // "screen": "2002-01-26(中国大陆) / 2020-08-14(中国大陆重映) / 2001-11-04(英国首映) / 2001-11-16(美国)",
        public string Screen { get; set; }
        public DateTime? ScreenTime
        {
            get
            {
                var items = Screen.Split("/");
                if (items.Length >= 0)
                {
                    var item = items[0].Split("(")[0];
                    DateTime result;
                    DateTime.TryParseExact(item, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out result);
                    return result;
                }
                return null;
            }
        }
        // "duration": "152分钟 / 159分钟(加长版)",
        public string Duration { get; set; }
        // "subname": "哈利波特1：神秘的魔法石(港/台) / 哈1 / Harry Potter and the Philosopher's Stone",
        public string Subname { get; set; }
        // "imdb": "tt0241527"
        public string Imdb { get; set; }
        public string Intro { get; set; }

        public List<ApiCelebrity> Celebrities { get; set; }
    }

    public class ApiCelebrity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Img { get; set; }
        public string Role { get; set; }
        public string Intro { get; set; }
        public string Gender { get; set; }
        public string Constellation { get; set; }
        public string Birthdate { get; set; }
        public string Birthplace { get; set; }
        public string Nickname { get; set; }
        public string Imdb { get; set; }
        public string Site { get; set; }
    }

    public class ApiPhoto
    {
        public string Id { get; set; }
        public string Small { get; set; }
        public string Medium { get; set; }
        public string Large { get; set; }
        public string Size { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
