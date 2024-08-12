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
        private readonly ILogger<CultureController> _logger;

        public CultureController(IStringLocalizer<UserController> localizer, ILogger<CultureController> logger)
        {
            _localizer = localizer;
            _logger = logger;
        }

        [HttpPost("SetCulture/{culture}")]
        public IActionResult SetCulture(string culture)
        {
            if (string.IsNullOrEmpty(culture))
            {
                _logger.LogWarning("SetCulture called with null or empty culture.");
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
            catch (CultureNotFoundException ex)
            {
                _logger.LogWarning(ex, "Invalid culture '{Culture}' passed to SetCulture.", culture);
                return BadRequest(_localizer["InvalidCulture"].Value ?? "Invalid culture.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while setting culture to '{Culture}'.", culture);
                return StatusCode(500, _localizer["UnexpectedError"].Value ?? "An error occurred while processing your request.");
            }
        }
    }
}
