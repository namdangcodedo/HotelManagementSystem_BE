using AppBackend.BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Extensions
{
    public static class SeedData
    {
        public static void Initialize(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IOTShowroomContext>();

            // Run migrations if needed
            context.Database.Migrate();

            // ---- Roles ----
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Role { RoleName = "Admin", Description = "Administrator" },
                    new Role { RoleName = "Manager", Description = "Manager" },
                    new Role { RoleName = "User", Description = "Regular User" }
                );
                context.SaveChanges();
            }

            // ---- Users ----
            if (!context.Users.Any())
            {
                var adminRoleId = context.Roles.First(r => r.RoleName == "Admin").RoleId;
                var managerRoleId = context.Roles.First(r => r.RoleName == "Manager").RoleId;
                var userRoleId = context.Roles.First(r => r.RoleName == "User").RoleId;

                context.Users.AddRange(
                    new User { FullName = "Nguyen Van A", Email = "admin@example.com", PasswordHash = "123456hash", Phone = "0901234567", RoleId = adminRoleId, CreatedAt = DateTime.UtcNow },
                    new User { FullName = "Tran Thi B", Email = "manager@example.com", PasswordHash = "123456hash", Phone = "0902234567", RoleId = managerRoleId, CreatedAt = DateTime.UtcNow },
                    new User { FullName = "Le Van C", Email = "instructor@example.com", PasswordHash = "123456hash", Phone = "0903234567", RoleId = userRoleId, CreatedAt = DateTime.UtcNow }
                );
                context.SaveChanges();
            }

        }
    }
}
