namespace GreenCrescent.Application.Common;

public interface ICurrentUserService
{
    Task<string?> GetUserIdAsync();
}