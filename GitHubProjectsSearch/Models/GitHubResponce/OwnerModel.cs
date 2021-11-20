using System.Text.Json.Serialization;

namespace GitHubProjectsSearch.Models.GitHubResponce
{
    public class OwnerModel
    {
        [JsonPropertyName("login")]
        public string Login { get; set; }
    }
}
