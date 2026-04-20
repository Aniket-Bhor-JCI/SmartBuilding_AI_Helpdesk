using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<AppDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher<User>>();

        await EnsureUserAsync(
            context,
            passwordHasher,
            name: "Admin User",
            email: "admin@smarthelpdesk.local",
            password: "Admin123!",
            role: UserRole.Admin);

        await EnsureUserAsync(
            context,
            passwordHasher,
            name: "Demo User",
            email: "user@smarthelpdesk.local",
            password: "User123!",
            role: UserRole.User);
    }

    private static async Task EnsureUserAsync(
        AppDbContext context,
        IPasswordHasher<User> passwordHasher,
        string name,
        string email,
        string password,
        UserRole role)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existingUser = await context.Users.FirstOrDefaultAsync(user => user.Email == normalizedEmail);
        if (existingUser is not null)
        {
            return;
        }

        var user = new User
        {
            Name = name,
            Email = normalizedEmail,
            Role = role
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        context.Users.Add(user);
        await context.SaveChangesAsync();
    }
}
