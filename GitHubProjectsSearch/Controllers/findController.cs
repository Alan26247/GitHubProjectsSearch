using GitHubProjectsSearch.Models;
using GitHubProjectsSearch.Models.GitHubResponce;
using GitHubProjectsSearch.Models.MySql;
using GitHubProjectsSearch.Models.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GitHubProjectsSearch.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class findController : ControllerBase
    {
        [BindProperty]
        public InputParametrs parametr { get; set; }

        private IConfigurationRoot ConfigurationRoot;
        private readonly IHttpClientFactory clientFactory;

        private readonly string stringConnectionDb;
        private readonly Version versionDBMS;
        private readonly string urlGithubMethod;



        public findController(IConfiguration configRoot, IHttpClientFactory clientFactory)
        {
            ConfigurationRoot = (IConfigurationRoot)configRoot;
            this.clientFactory = clientFactory;


            stringConnectionDb = ConfigurationRoot.GetConnectionString("DefaultConnectionMySql");

            urlGithubMethod = ConfigurationRoot["UrlGithubMethod"];

            versionDBMS = new Version(int.Parse(ConfigurationRoot["VersionDBMS:Major"]),
                                        int.Parse(ConfigurationRoot["VersionDBMS:Minor"]),
                                            int.Parse(ConfigurationRoot["VersionDBMS:Build"]));
        }

        [HttpGet]
        public ActionResult<SearchIdModel[]> Get()
        {
            using (MySqlDbContext db = new MySqlDbContext(stringConnectionDb, versionDBMS))
            {
                SearchModel[] searchModels = db.SearchList.ToArray();

                SearchIdModel[] searchIdModels = new SearchIdModel[searchModels.Length];

                for(int i = 0; i < searchModels.Length; i++)
                {
                    SearchIdModel itemIdModel = new SearchIdModel();

                    itemIdModel.Id = searchModels[i].Id;
                    itemIdModel.Request = searchModels[i].Request;

                    searchIdModels[i] = itemIdModel;
                }

                return searchIdModels;
            }
        }

        [HttpPost]
        public async Task<ActionResult<ProjectModel[]>> PostAsync([FromBody] InputParametrs parametr)
        {
            string jsonResponse = "";

            // поиск запроса в базе данных
            using (MySqlDbContext db = new MySqlDbContext(stringConnectionDb, versionDBMS))
            {
                SearchModel searchModel = db.SearchList.Where(p => p.Request == parametr.SearchString).FirstOrDefault();

                if (searchModel != null)
                {
                    // если в базе данных данный запрос имеется то получаем его
                    jsonResponse = searchModel.Response;
                }
                else
                {
                    // иначе запрос на githab
                    HttpClient client = clientFactory.CreateClient("github");
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, urlGithubMethod + parametr.SearchString);
                    HttpResponseMessage responseMessage = await client.SendAsync(request);

                    jsonResponse = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

                    // сохранение ответа в базе данных
                    SearchModel searchRequest = new SearchModel { Request = parametr.SearchString, Response = jsonResponse };
                    db.SearchList.AddRange(searchRequest);
                    await db.SaveChangesAsync();
                }
            }

            GitHubResponceModel gitHubResponceModel = JsonSerializer.Deserialize<GitHubResponceModel>(jsonResponse);

            return gitHubResponceModel.Projects;
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAsync(int id)
        {
            using (MySqlDbContext db = new MySqlDbContext(stringConnectionDb, versionDBMS))
            {
                SearchModel searchModel = db.SearchList.Where(p => p.Id == id).FirstOrDefault();

                if (searchModel == null)
                {
                    return NotFound();
                }
                else
                {
                    db.SearchList.Remove(searchModel);
                    await db.SaveChangesAsync();

                    return Ok();
                }
            }
        }
    }
}
