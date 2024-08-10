namespace Api.Controllers
{
    using Microsoft.AspNetCore.Localization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;
    using System.Globalization;

    [ApiController]
    [Route("[controller]")]
    public class CultureController : ControllerBase
    {
        private readonly IStringLocalizer<UserController> _localizer;
        public CultureController(IStringLocalizer<UserController> localizer)
        {
            _localizer = localizer;
        }
        [HttpPost("SetCulture/{culture}")]
        public IActionResult SetCulture(string culture)
        {
            if (string.IsNullOrEmpty(culture))
            {
                return BadRequest(_localizer["RequiredCulture"].Value ?? "Culture is required.");
            }

            try
            {
                var cultureInfo = new CultureInfo(culture);
                CultureInfo.CurrentCulture = cultureInfo;
                CultureInfo.CurrentUICulture = cultureInfo;

                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(cultureInfo)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                );

                return Ok(_localizer["CultureSetTo"].Value + culture ?? $"Culture set to {culture}");
            }
            catch (CultureNotFoundException)
            {
                return BadRequest(_localizer["InvalidCulture"] ?? "Invalid culture.");
            }
        }
    }

}
