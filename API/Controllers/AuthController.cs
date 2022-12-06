﻿using API.ActionFilters;
using BLL.Abstract;
using CORE.Abstract;
using CORE.Config;
using CORE.Helper;
using CORE.Localization;
using DTO.Auth;
using DTO.Responses;
using DTO.Token;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using IResult = DTO.Responses.IResult;

namespace API.Controllers;

[Route("api/[controller]")]
[ServiceFilter(typeof(LogActionFilter))]
[Authorize]
public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly ConfigSettings _configSettings;
    private readonly ITokenService _tokenService;
    private readonly IUtilService _utilService;

    public AuthController(IAuthService authService, ConfigSettings configSettings, IUtilService utilService,
        ITokenService tokenService)
    {
        _authService = authService;
        _configSettings = configSettings;
        _utilService = utilService;
        _tokenService = tokenService;
    }

    [SwaggerOperation(Summary = "login")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(IDataResult<LoginResponseDto>))]
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        var userSalt = await _authService.GetUserSaltAsync(request.Email);

        if (string.IsNullOrEmpty(userSalt))
            return Ok(new ErrorDataResult<Result>(Messages.InvalidUserCredentials.Translate()));

        request.Password = SecurityHelper.HashPassword(request.Password, userSalt);

        var userDto = await _authService.LoginAsync(request);
        if (!userDto.Success) return Ok(userDto);

        var securityHelper = new SecurityHelper(_configSettings);

        var accessTokenExpireDate = DateTime.UtcNow.AddHours(_configSettings.AuthSettings.TokenExpirationTimeInHours);

        var loginResponseDto = new LoginResponseDto
        {
            User = userDto.Data,
            AccessToken = securityHelper.CreateTokenForUser(userDto.Data, accessTokenExpireDate),
            AccessTokenExpireDate = accessTokenExpireDate,
            RefreshToken = _utilService.GenerateRefreshToken(),
            RefreshTokenExpireDate =
                accessTokenExpireDate.AddMinutes(_configSettings.AuthSettings.RefreshTokenAdditionalMinutes)
        };
        await _tokenService.AddAsync(loginResponseDto);

        return Ok(new SuccessDataResult<LoginResponseDto>(loginResponseDto));
    }

    [SwaggerOperation(Summary = "send email for reset password")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(IResult))]
    [HttpGet("verificationCode")]
    [AllowAnonymous]
    public IActionResult SendVerificationCode([FromQuery] string email)
    {
        return Ok(_authService.SendVerificationCodeToEmailAsync(email));
    }

    [SwaggerOperation(Summary = "refesh access token")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(IResult))]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto refreshTokenDto)
    {
        var response = await _tokenService.GetAsync(refreshTokenDto);
        if (response.Data == null) return Unauthorized();
        await _tokenService.SoftDeleteAsync(response.Data.TokenId);

        var securityHelper = new SecurityHelper(_configSettings);

        var accessTokenExpireDate = DateTime.UtcNow.AddHours(_configSettings.AuthSettings.TokenExpirationTimeInHours);

        var loginResponseDto = new LoginResponseDto
        {
            User = response.Data.User,
            AccessToken = securityHelper.CreateTokenForUser(response.Data.User, accessTokenExpireDate),
            AccessTokenExpireDate = accessTokenExpireDate,
            RefreshToken = _utilService.GenerateRefreshToken(),
            RefreshTokenExpireDate =
                accessTokenExpireDate.AddMinutes(_configSettings.AuthSettings.RefreshTokenAdditionalMinutes)
        };
        await _tokenService.AddAsync(loginResponseDto);

        return Ok(new SuccessDataResult<LoginResponseDto>(loginResponseDto));
    }

    [SwaggerOperation(Summary = "reset password")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(IResult))]
    [HttpPost("resetPassword")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
    {
        var response = await _authService.ResetPasswordAsync(request);
        return Ok(response);
    }

    [SwaggerOperation(Summary = "login by token")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(IDataResult<LoginResponseDto>))]
    [HttpGet("loginByToken")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginByToken()
    {
        if (string.IsNullOrEmpty(HttpContext.Request.Headers.Authorization))
            return Unauthorized(new ErrorResult(Messages.CanNotFoundUserIdInYourAccessToken.Translate()));
        var response = await _authService.LoginByTokenAsync(HttpContext.Request.Headers.Authorization!);
        if (!response.Success) return BadRequest(response.Data);

        var securityHelper = new SecurityHelper(_configSettings);

        var accessTokenExpireDate = DateTime.UtcNow.AddHours(_configSettings.AuthSettings.TokenExpirationTimeInHours);

        var loginResponseDto = new LoginResponseDto
        {
            User = response.Data,
            AccessToken = securityHelper.CreateTokenForUser(response.Data, accessTokenExpireDate),
            AccessTokenExpireDate = accessTokenExpireDate,
            RefreshToken = _utilService.GenerateRefreshToken(),
            RefreshTokenExpireDate =
                accessTokenExpireDate.AddMinutes(_configSettings.AuthSettings.RefreshTokenAdditionalMinutes)
        };
        await _tokenService.AddAsync(loginResponseDto);

        return Ok(new SuccessDataResult<LoginResponseDto>(loginResponseDto));
    }

    [SwaggerOperation(Summary = "logout")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(IResult))]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var accessToken =
            _utilService.GetTokenStringFromHeader(HttpContext.Request.Headers[_configSettings.AuthSettings.HeaderName]);
        var response = await _authService.LogoutAsync(accessToken);
        return Ok(response);
    }
}