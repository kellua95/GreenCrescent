using System;
using System.Collections.Generic;
using System.Text;

namespace GreenCrescent.Infrastructure.Identity
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Staff = "Staff";
        public const string Viewer = "Viewer";

        public static readonly string[] All =
        [
            Admin,
            Staff,
            Viewer
        ];
    }
}
