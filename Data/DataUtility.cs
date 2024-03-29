﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TusharContactProApp.Models;

namespace TusharContactProApp.Data
{
    public static class DataUtility
    {
        public static string GetConnectionString(IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            string? databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            //return string.IsNullOrEmpty(databaseUrl) ? connectionString : BuildConnectionString(databaseUrl);

            if(string.IsNullOrEmpty(databaseUrl) ) {
                return connectionString;
            }
            else
            {
                return BuildConnectionString(databaseUrl);
            }
        }

        private static string BuildConnectionString(string databaseUrl)
        {
            var databaseUri = new Uri(databaseUrl);
            var userIndo = databaseUri.UserInfo.Split(':');
            var builder = new NpgsqlConnectionStringBuilder()
            {
                Host = databaseUri.Host,
                Port = databaseUri.Port,
                Username = userIndo[0],
                Password = userIndo[1],
                Database = databaseUri.LocalPath.TrimStart('/'),
                SslMode = SslMode.Require,
                TrustServerCertificate = true
            };

            return builder.ToString();
        }

        public static async Task ManageDataAsync(IServiceProvider svcProvider)
        {
            var dbContextSvc = svcProvider.GetRequiredService<ApplicationDbContext>();

            var userManagerSvc = svcProvider.GetRequiredService<UserManager<AppUser>>();

            await dbContextSvc.Database.MigrateAsync();

            await SeedDemoUserAsync(userManagerSvc);
        }

        private static async Task SeedDemoUserAsync(UserManager<AppUser> userManager)
        {
            AppUser demoUser = new AppUser()
            {
                UserName = "demologin@contactpro.com",
                Email = "demologin@contactpro.com",
                FirstName = "Demo",
                LastName = "Login",
                EmailConfirmed = true,

            };

            try
            {
                AppUser user = await userManager.FindByEmailAsync(demoUser.Email);

                if (user == null)
                {
                    await userManager.CreateAsync(demoUser, "Abc&123!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("************* ERROR *********");
                Console.WriteLine("Error Seeding Demo Login User");
                Console.WriteLine(ex.Message);
                Console.WriteLine("*****************************");

                throw;
            }
        }

    }
}
