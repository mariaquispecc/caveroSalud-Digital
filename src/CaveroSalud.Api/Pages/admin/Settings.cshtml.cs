using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaveroSalud.Api.Pages.Admin
{
    [Authorize(Roles = "Administrador,Admin")]
    public class SettingsModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
