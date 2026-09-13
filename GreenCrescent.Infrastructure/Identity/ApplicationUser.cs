using Microsoft.AspNetCore.Identity;

namespace GreenCrescent.Infrastructure.Identity
{
    public sealed class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime? DeactivatedAtUtc { get; set; }
    }
}
