using Microsoft.AspNetCore.Mvc;
using KinTraffic.Api.Services.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace KinTraffic.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting(AuthenticationRateLimiting.PolicyName)]
public sealed class AuthController : ControllerBase
{
    private const string CsrfHeaderName = "X-KinTraffic-CSRF";
    private const string CsrfHeaderValue = "1";

    private readonly AuthenticationService _authentication;
    private readonly AuthenticationOptions _options;

    public AuthController(
        AuthenticationService authentication,
        IOptions<AuthenticationOptions> options)
    {
        _authentication = authentication;
        _options = options.Value;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authentication.LoginAsync(
            request.UserName,
            request.Password,
            HttpContext.Connection.RemoteIpAddress,
            cancellationToken);
        if (result.Result == LoginAttemptResult.RateLimited)
        {
            var retryAfterSeconds = (long)Math.Ceiling(result.RetryAfter?.TotalSeconds ?? _options.AccountWindowSeconds!.Value);
            Response.Headers.RetryAfter = Math.Max(1, retryAfterSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
            return StatusCode(StatusCodes.Status429TooManyRequests, new { error = "rate_limit_exceeded" });
        }

        if (result.Result != LoginAttemptResult.Succeeded || result.Tokens is null)
        {
            return Unauthorized(new { error = "invalid_credentials" });
        }

        SetRefreshCookie(result.Tokens);
        SetAccessTokenHeader(result.Tokens);
        return Ok(ToResponse(result.Tokens));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!HasValidCsrfRequest())
        {
            return Forbid();
        }

        var result = await _authentication.RefreshAsync(
            Request.Cookies[_options.RefreshCookieName!],
            cancellationToken);
        if (!result.Succeeded || result.Tokens is null)
        {
            ClearRefreshCookie();
            return Unauthorized(new { error = "invalid_session" });
        }

        SetRefreshCookie(result.Tokens);
        SetAccessTokenHeader(result.Tokens);
        return Ok(ToResponse(result.Tokens));
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!HasValidCsrfRequest())
        {
            return Forbid();
        }

        await _authentication.LogoutAsync(
            Request.Cookies[_options.RefreshCookieName!],
            cancellationToken);
        ClearRefreshCookie();
        return NoContent();
    }

    private bool HasValidCsrfRequest()
    {
        var origin = Request.Headers.Origin.ToString();
        var fetchSite = Request.Headers["Sec-Fetch-Site"].ToString();
        return Request.Headers[CsrfHeaderName] == CsrfHeaderValue &&
               (_options.AllowedOrigins ?? []).Contains(origin, StringComparer.Ordinal) &&
             fetchSite is "same-origin" or "same-site";
    }

    private void SetRefreshCookie(AuthenticationSessionTokens tokens)
    {
        Response.Cookies.Append(_options.RefreshCookieName!, tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = Enum.Parse<SameSiteMode>(_options.RefreshCookieSameSite!, ignoreCase: true),
            Path = _options.RefreshCookiePath,
            Expires = tokens.RefreshTokenExpiresAt,
            IsEssential = true
        });
    }

    private void SetAccessTokenHeader(AuthenticationSessionTokens tokens) =>
        Response.Headers.Authorization = $"Bearer {tokens.AccessToken}";

    private void ClearRefreshCookie() => Response.Cookies.Delete(_options.RefreshCookieName!, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = Enum.Parse<SameSiteMode>(_options.RefreshCookieSameSite!, ignoreCase: true),
        Path = _options.RefreshCookiePath
    });

    private static AuthenticationResponse ToResponse(AuthenticationSessionTokens tokens) =>
        new(tokens.AccessTokenExpiresAt);
}

public sealed record LoginRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(256)]
    public required string UserName { get; init; }

    [System.ComponentModel.DataAnnotations.Required]
    public required string Password { get; init; }
    }

public sealed record AuthenticationResponse(DateTimeOffset ExpiresAt);
