namespace Application.Abstractions.Auth;
public interface ITokenService
{
    (string accessToken, DateTime expiresAt) CreateAccessToken(Guid userId, string email);
    (string refreshToken, DateTime expiresAt) CreateRefreshToken();
}
