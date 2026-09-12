using System.Security.Claims;
using GreenCrescent.Application.Common;
using Microsoft.AspNetCore.Components.Authorization;

namespace GreenCrescent.Web.Services;

public sealed class CurrentUserService(
    AuthenticationStateProvider authenticationStateProvider)
    : ICurrentUserService
{
    public async Task<string?> GetUserIdAsync()
    {
        var state =
            await authenticationStateProvider
                .GetAuthenticationStateAsync();

        return state.User.FindFirstValue(
            ClaimTypes.NameIdentifier);
    }
}