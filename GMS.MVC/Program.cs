using Domin.Contract;
using GMS.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Presistence.Data;
using Presistence.Identity;
using Presistence.Repositories;
using Services.Abstraction.Contract;
using Services.Implmentations;
using Services.Mapping;

namespace GMS.MVC {
    public class Program {
        public static async Task Main(string[] args) {

            var builder = WebApplication.CreateBuilder(args);

            #region Add Services
            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Database Configuration
            builder.Services.AddDbContext<GymDbContext>(options => {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnections"));
            });

            // Seeding Options (Bootstrap Admin Credentials + Optional Demo Records)
            var seedOptions = builder.Configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>() ?? new SeedOptions();
            // The Destructive Demo Reset Is Development-Only, And That Is Enforced Here Rather
            // Than Left To A Comment In A Config File That Nobody Reads Before Deploying.
            seedOptions.IsDevelopment = builder.Environment.IsDevelopment();
            builder.Services.AddSingleton(seedOptions);

            #region Identity
            builder.Services.AddIdentity<AppUser, IdentityRole>(options => {
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;

                options.User.RequireUniqueEmail = true;

                // A Short Lockout After Repeated Failures Blunts Password Guessing.
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
            })
            .AddEntityFrameworkStores<GymDbContext>()
            .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options => {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            builder.Services.AddAuthorization(options => {
                // Anything Not Explicitly Opened Up With [AllowAnonymous] Requires A Signed-In User.
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                options.AddPolicy(AppPolicies.AdminOnly, policy => policy.RequireRole(AppRoles.Admin));
                options.AddPolicy(AppPolicies.StaffOnly, policy => policy.RequireRole(AppRoles.Admin, AppRoles.Trainer));
            });
            #endregion

            // Allow DI To DbInitilazer 
            builder.Services.AddScoped<IDbInitilazer, DbInitilazer>();

            // Allow DI To UnitOfWork 
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Allow DI To The Service Manager (Members, Trainers, Plans, Sessions, Memberships, Bookings, Analytics)
            builder.Services.AddScoped<IServiceManger, ServiceManger>();

            // Allow DI To The Attachment Service (Profile Photo Uploads)
            builder.Services.AddScoped<IAttachmentService, AttachmentService>();

            // Resolves The Signed-In Account To Its Gym Record, For The Member Self-Service Screens
            builder.Services.AddScoped<IMemberContext, MemberContext>();

            // Currency Is Configuration, Not Something Each View Spells Out For Itself
            builder.Services.Configure<CurrencyOptions>(builder.Configuration.GetSection(CurrencyOptions.SectionName));
            builder.Services.AddSingleton<IMoneyFormatter, MoneyFormatter>();

            // Add AutoMapper To Services
            builder.Services.AddAutoMapper(M => {
                M.AddProfile(new MemberProfile());
                M.AddProfile(new PlanProfile());
                M.AddProfile(new SessionProfile());
                M.AddProfile(new TrainerProfile());
                M.AddProfile(new MembershipProfile());
                M.AddProfile(new BookingProfile());
            });

            #endregion

            var app = builder.Build();

            #region Add Kestrel Middelware

            // Database Initilaizer
            using (var scope = app.Services.CreateScope()) {
                var dbInitilaizer = scope.ServiceProvider.GetRequiredService<IDbInitilazer>();
                await dbInitilaizer.InitilazeAsync();
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment()) {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Renders The Friendly 404 / 403 Pages Instead Of A Blank Browser Error.
            app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"); 
            #endregion

            app.Run();
        }
    }
}

// 1. Get Into Solution Folder
// Get-ChildItem -Recurse | Unblock-File
// dotnet clean
// dotnet build
