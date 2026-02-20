using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;

namespace web_test_jwt2.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        
        public void OnGet()
        {
            var fullName = User.FindFirst("Username")?.Value;
            var id = User.FindFirst("UserId")?.Value;
            var role = User.FindFirst("Role")?.Value;
            
        }
    }
}
