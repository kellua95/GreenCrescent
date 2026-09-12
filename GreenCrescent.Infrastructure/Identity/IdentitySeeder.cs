using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace GreenCrescent.Infrastructure.Identity
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
        {
            using var scope = services.CreateScope();

            var roleManager =
                scope.ServiceProvider.GetRequiredService<
                    RoleManager<IdentityRole>>();

            var userManager =
                scope.ServiceProvider.GetRequiredService<
                    UserManager<ApplicationUser>>();

            foreach (var roleName in AppRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(
                        new IdentityRole(roleName));

                    if (!result.Succeeded)
                    {
                        var errors = string.Join(
                            ", ",
                            result.Errors.Select(error =>
                                error.Description));

                        throw new InvalidOperationException(
                            $"تعذر إنشاء الصلاحية {roleName}: {errors}");
                    }
                }
            }

            var adminEmail =
                configuration["InitialAdmin:Email"];

            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                throw new InvalidOperationException(
                    "لم يتم تحديد البريد الإلكتروني لأول مدير.");
            }

            var admin = await userManager.FindByEmailAsync(
                adminEmail.Trim());

            if (admin is null)
            {
                throw new InvalidOperationException(
                    $"لا يوجد حساب مسجل بالبريد {adminEmail}.");
            }

            if (!admin.EmailConfirmed)
            {
                admin.EmailConfirmed = true;

                var confirmationResult =
                    await userManager.UpdateAsync(admin);

                if (!confirmationResult.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        confirmationResult.Errors.Select(error =>
                            error.Description));

                    throw new InvalidOperationException(
                        $"تعذر تأكيد حساب المدير: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(
                    admin,
                    AppRoles.Admin))
            {
                var roleResult = await userManager.AddToRoleAsync(
                    admin,
                    AppRoles.Admin);

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        roleResult.Errors.Select(error =>
                            error.Description));

                    throw new InvalidOperationException(
                        $"تعذر منح صلاحية المدير: {errors}");
                }
            }
        }
    }
}
