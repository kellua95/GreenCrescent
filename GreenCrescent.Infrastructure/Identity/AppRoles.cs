namespace GreenCrescent.Infrastructure.Identity;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string ApplicationsStaff = "ApplicationsStaff";
    public const string Viewer = "Viewer";

    public static readonly string[] All =
    [
        Admin,
        Staff,
        ApplicationsStaff,
        Viewer
    ];

    public static string GetDisplayName(string role) => role switch
    {
        Admin => "مدير النظام",
        Staff => "موظف الكفالات",
        ApplicationsStaff => "موظف الطلبات",
        Viewer => "موظف التقارير",
        _ => role
    };
}