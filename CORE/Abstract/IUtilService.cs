namespace CORE.Abstract;

public interface IUtilService
{
    HttpContent GetHttpContentObject(object obj);
    public bool IsValidToken(string tokenString);

    public int? GetUserIdFromToken(string? tokenString);
    public int? GetCompanyIdFromToken(string? tokenString);
    public string GenerateRefreshToken();
    public string GetTokenStringFromHeader(string jwtToken);
}