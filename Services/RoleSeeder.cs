using Microsoft.AspNetCore.Identity;
using NetSentinel.API.Models.Identity;

namespace NetSentinel.API.Services;

public static class RoleSeeder
{
    private static readonly string[] Roles =
    {
        "Admin",
        "SecurityAnalyst",
        "Viewer"
    };

    public static async Task SeedAsync(
        IServiceProvider services)
    {
        var roleManager =
            services.GetRequiredService<
                RoleManager<IdentityRole<int>>>();

        foreach (var role in Roles)
        {
            if (await roleManager
                .RoleExistsAsync(role))
            {
                continue;
            }

            var result =
                await roleManager.CreateAsync(
                    new IdentityRole<int>(role));

            if (!result.Succeeded)
            {
                var errors =
                    string.Join(
                        "; ",
                        result.Errors.Select(
                            x => x.Description));

                throw new InvalidOperationException(
                    $"Failed to create role " +
                    $"'{role}': {errors}");
            }
        }
    }
}