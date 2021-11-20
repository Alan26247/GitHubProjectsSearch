using System.Text.Json.Serialization;

namespace GitHubProjectsSearch.Models.GitHubResponce
{
    public class GitHubResponceModel
    {
        [JsonPropertyName("items")]
        public ProjectModel[] Projects { get; set; }
    }
}
