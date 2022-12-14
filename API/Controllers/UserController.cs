using API.ActionFilters;
using API.Attributes;
using BLL.Abstract;
using CORE.Abstract;
using CORE.Config;
using CORE.Constants;
using DTO.Responses;
using DTO.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using IResult = DTO.Responses.IResult;
using Path = System.IO.Path;

namespace API.Controllers;

[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UserController : Controller
{
    private readonly ConfigSettings _configSettings;
    private readonly IWebHostEnvironment _environment;
    private readonly IUserService _userService;
    private readonly IUtilService _utilService;

    public UserController(IUserService userService, ConfigSettings configSettings, IWebHostEnvironment environment,
        IUtilService utilService)
    {
        _userService = userService;
        _configSettings = configSettings;
        _environment = environment;
        _utilService = utilService;
    }

    [SwaggerOperation(Summary = "get users as paginated list")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(IDataResult<List<UserToListDto>>))]
    [HttpGet("paginate")]
    [ValidateToken]
    [ServiceFilter(typeof(LogActionFilter))]
    public async Task<IActionResult> GetAsPaginated()
    {
        var pageIndex =
            Convert.ToInt32(HttpContext.Request.Headers[_configSettings.RequestSettings.PageIndex]);
        var pageSize =
            Convert.ToInt32(HttpContext.Request.Headers[_configSettings.RequestSettings.PageSize]);
        var response = await _userService.GetAsPaginatedListAsync(pageIndex, pageSize);

        return Ok(response);
    }

    [SwaggerOperation(Summary = "get users")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(IDataResult<List<UserToListDto>>))]
    [HttpGet]
    [ValidateToken]
    [ServiceFilter(typeof(LogActionFilter))]
    public async Task<IActionResult> Get()
    {
        var response = await _userService.GetAsync();
        return Ok(response);
    }

    [ServiceFilter(typeof(LogActionFilter))]
    [SwaggerOperation(Summary = "get profile info")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(IDataResult<List<UserToListDto>>))]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfileInfo()
    {
        var id = _utilService.GetUserIdFromToken(HttpContext.Request.Headers[Constants.AuthHeader]).Value;
        var response = await _userService.GetAsync(id);
        return Ok(response);
    }

    [SwaggerOperation(Summary = "get user")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(IDataResult<UserToListDto>))]
    [HttpGet("{id}")]
    [ValidateToken]
    [ServiceFilter(typeof(LogActionFilter))]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        var response = await _userService.GetAsync(id);
        return Ok(response);
    }

    [AllowAnonymous]
    [SwaggerOperation(Summary = "create user")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(IResult))]
    [HttpPost("register")]
    [ServiceFilter(typeof(LogActionFilter))]
    public async Task<IActionResult> Add([FromBody] UserToAddDto dto)
    {
        var response = await _userService.AddAsync(dto);
        return Ok(response);
    }

    [SwaggerOperation(Summary = "update user")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(IResult))]
    [HttpPut("{id}")]
    [ValidateToken]
    [ServiceFilter(typeof(LogActionFilter))]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UserToUpdateDto dto)
    {
        var response = await _userService.UpdateAsync(id, dto);
        return Ok(response);
    }

    [SwaggerOperation(Summary = "delete user")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(IResult))]
    [HttpDelete("{id}")]
    [ValidateToken]
    [ServiceFilter(typeof(LogActionFilter))]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var response = await _userService.SoftDeleteAsync(id);
        return Ok(response);
    }

    [SwaggerOperation(Summary = "upload user image")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(string))]
    [HttpPost("image")]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        var originalFileName = file.FileName;
        var fileExtension = file.FileName[(file.FileName.LastIndexOf('.') + 1)..];
        var guid = Guid.NewGuid();
        var folderName = "users";
        var uploads = Path.Combine(_environment.WebRootPath, folderName);
        var fileName = guid + "." + fileExtension;
        var filePath = $"{folderName}/{fileName}";

        if (file.Length <= 0) return Ok(fileName);
        using (var fileStream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        var id = _utilService.GetUserIdFromToken(HttpContext.Request.Headers[Constants.AuthHeader]).Value;

        var response = await _userService.UpdateProfilePhotoAsync(id, filePath);
        return Ok(response);
    }


    [ServiceFilter(typeof(LogActionFilter))]
    [SwaggerOperation(Summary = "delete image")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(void))]
    [HttpDelete("image")]
    public async Task<IActionResult> DeleteImage()
    {
        var id = _utilService.GetUserIdFromToken(HttpContext.Request.Headers[Constants.AuthHeader]).Value;

        var response = await _userService.DeleteProfilePhotoAsync(id);
        return Ok(response);
    }
}