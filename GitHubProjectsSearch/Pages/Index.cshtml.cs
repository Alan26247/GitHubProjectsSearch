using System;
using GitHubProjectsSearch.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GitHubProjectsSearch.Models.MySql;
using GitHubProjectsSearch.Models.GitHubResponce;
using GitHubProjectsSearch.Models.Search;

namespace GitHubProjectsSearchEngine.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        [RegularExpression(@"[A-Za-z0-9_ ]{2,50}")]
        public string SearchString { get; set; }

        public string Info { get; set; }

        public GitHubResponceModel GitHubResponceModel;

        private readonly IConfigurationRoot ConfigurationRoot;
        private readonly IHttpClientFactory clientFactory;

        private readonly string stringConnectionDb;
        private readonly Version versionDBMS;
        private readonly string urlGithubMethod = "https://api.github.com/search/repositories?q=";



        public IndexModel(IConfiguration configRoot, IHttpClientFactory clientFactory)
        {
            ConfigurationRoot = (IConfigurationRoot)configRoot;
            this.clientFactory = clientFactory;

            stringConnectionDb = ConfigurationRoot.GetConnectionString("DefaultConnectionMySql");

            urlGithubMethod = ConfigurationRoot["UrlGithubMethod"];

            versionDBMS = new Version(int.Parse(ConfigurationRoot["VersionDBMS:Major"]),
                                        int.Parse(ConfigurationRoot["VersionDBMS:Minor"]),
                                            int.Parse(ConfigurationRoot["VersionDBMS:Build"]));
        }

        public void OnGet()
        {
            Info = "";
        }

        public async Task OnPostAsync(string SearchString)
        {
            // проверка введенных данных
            if (!ModelState.IsValid)
            {
                Info = "Ошибка ввода данных.";
                return;
            }
            else
            {
                Info = "";
            }

            string response = "";

            // поиск запроса в базе данных
            using (MySqlDbContext db = new MySqlDbContext(stringConnectionDb, versionDBMS))
            {
                var searchRequests = db.SearchList.Where(p => p.Request == SearchString).FirstOrDefault();

                if(searchRequests != null)
                {
                        response = searchRequests.Response;
                }
                else
                {
                    // иначе запрос на githab
                    HttpClient client = clientFactory.CreateClient("github");
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, urlGithubMethod + SearchString);
                    HttpResponseMessage responseMessage = await client.SendAsync(request);

                    response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

                    // сохранение ответа в базе данных
                    SearchModel searchRequest = new SearchModel { Request = SearchString, Response = response };
                    db.SearchList.AddRange(searchRequest);
                    db.SaveChanges();
                }
            }

            this.GitHubResponceModel = JsonSerializer.Deserialize<GitHubResponceModel>(response);
        }
    }
}
