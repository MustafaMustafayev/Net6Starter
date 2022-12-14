using AutoMapper;
using BLL.Abstract;
using CORE.Abstract;
using CORE.Helper;
using CORE.Localization;
using DAL.UnitOfWorks.Abstract;
using DTO.Auth;
using DTO.Responses;
using DTO.User;

namespace BLL.Concrete;

public class AuthService : IAuthService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilService _utilService;

    public AuthService(IUnitOfWork unitOfWork, IMapper mapper, IUtilService utilService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _utilService = utilService;
    }

    public async Task<string?> GetUserSaltAsync(string userEmail)
    {
        return await _unitOfWork.UserRepository.GetUserSaltAsync(userEmail);
    }

    public async Task<IDataResult<UserToListDto>> LoginAsync(LoginDto dto)
    {
        var data =
            await _unitOfWork.UserRepository.GetAsync(m =>
                m.Email == dto.Email && m.Password == dto.Password);
        if (data == null)
            return new ErrorDataResult<UserToListDto>(Messages.InvalidUserCredentials.Translate());

        return new SuccessDataResult<UserToListDto>(_mapper.Map<UserToListDto>(data),
            Messages.Success.Translate());
    }

    public async Task<IDataResult<UserToListDto>> LoginByTokenAsync(string token)
    {
        var userId = _utilService.GetUserIdFromToken(token);
        if (userId is null)
            return new ErrorDataResult<UserToListDto>(Messages.CanNotFoundUserIdInYourAccessToken
                .Translate());

        var data = await _unitOfWork.UserRepository.GetAsync(m => m.UserId == userId);
        if (data == null)
            return new ErrorDataResult<UserToListDto>(Messages.InvalidUserCredentials.Translate());

        return new SuccessDataResult<UserToListDto>(_mapper.Map<UserToListDto>(data),
            Messages.Success.Translate());
    }

    public IResult SendVerificationCodeToEmailAsync(string email)
    {
        //TODO SEND MAIL TO EMAIL
        return new SuccessResult(Messages.VerificationCodeSent.Translate());
    }

    public async Task<IResult> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var data = await _unitOfWork.UserRepository.GetAsync(m => m.Email == dto.Email);

        if (data is null) return new ErrorResult(Messages.UserIsNotExist.Translate());

        if (data.LastVerificationCode is null ||
            !data.LastVerificationCode.Equals(dto.VerificationCode))
            return new ErrorResult(Messages.InvalidVerificationCode.Translate());

        data.Salt = SecurityHelper.GenerateSalt();
        data.Password = SecurityHelper.HashPassword(dto.Password, data.Salt);
        await _unitOfWork.CommitAsync();

        return new SuccessResult(Messages.PasswordResetted.Translate());
    }

    public async Task<IResult> LogoutAsync(string accessToken)
    {
        var tokens = await _unitOfWork.TokenRepository.GetActiveTokensAsync(accessToken);

        tokens.ForEach(m => m.IsDeleted = true);
        await _unitOfWork.CommitAsync();

        return new SuccessResult(Messages.Success.Translate());
    }
}