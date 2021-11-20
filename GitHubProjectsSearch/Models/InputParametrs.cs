using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace GitHubProjectsSearch.Models
{
    public class InputParametrs
    {
        [BindProperty]
        [RegularExpression(@"[A-Za-z0-9_ ]{2,50}")]
        public string SearchString { get; set; }
    }
}
