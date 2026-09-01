using Domin.Contract;
using Domin.Enums;
using Domin.Entities;
using Domin.GymEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Presistence.Identity;
using System.Text.Json;

namespace Presistence.Data {
    public class DbInitilazer(
        GymDbContext _dbContext,
        UserManager<AppUser> _userManager,
        RoleManager<IdentityRole> _roleManager,
        SeedOptions _seedOptions,
        ILogger<DbInitilazer> _logger) : IDbInitilazer {

        public async Task InitilazeAsync() {
            // Check If Any Migration 
            if (_dbContext.Database.GetPendingMigrations().Any()) {
                await _dbContext.Database.MigrateAsync();
            }

            await SeedCategoriesAsync();
            await SeedPlansAsync();
            await SeedRolesAsync();
            await SeedAdminAsync();

            if (_seedOptions.SeedDemoData) await SeedDemoDataAsync();
        }

        #region ==== Reference Data ====
        private async Task SeedCategoriesAsync() {
            if (await _dbContext.Categories.AnyAsync()) return;
            var categoriesData = LoadDataFromJsonFile<Category>("categories.json");
            if (categoriesData.Count == 0) return;
            _dbContext.Categories.AddRange(categoriesData);
            await _dbContext.SaveChangesAsync();
        }

        private async Task SeedPlansAsync() {
            if (await _dbContext.Plans.AnyAsync()) return;
            var plansData = LoadDataFromJsonFile<Plan>("plans.json");
            if (plansData.Count == 0) return;
            _dbContext.Plans.AddRange(plansData);
            await _dbContext.SaveChangesAsync();
        }
        #endregion

        #region ==== Identity ====
        private async Task SeedRolesAsync() {
            foreach (var role in AppRoles.All) {
                if (!await _roleManager.RoleExistsAsync(role))
                    await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        /// <summary>
        /// Creates The First Administrator So The App Is Never Locked Out Of Itself On A Fresh
        /// Database. Credentials Come From Configuration, Never From A Hard-Coded Literal.
        /// </summary>
        private async Task SeedAdminAsync() {
            var email = _seedOptions.AdminEmail;
            var password = _seedOptions.AdminPassword;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) {
                // Silently Skipping Would Leave A Fresh Deployment With No Way In At All.
                _logger.LogWarning(
                    "No administrator was seeded because Seed:AdminEmail / Seed:AdminPassword are not configured. " +
                    "Set them (user secrets or the Seed__AdminPassword environment variable) and restart, " +
                    "otherwise nobody can sign in.");
                return;
            }

            if (await _userManager.FindByEmailAsync(email) is not null) return;

            var admin = new AppUser {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = _seedOptions.AdminFullName
            };

            var result = await _userManager.CreateAsync(admin, password);
            if (result.Succeeded) {
                await _userManager.AddToRoleAsync(admin, AppRoles.Admin);
                _logger.LogInformation("Seeded the administrator account {Email}.", email);
            }
            else {
                _logger.LogError("Seeding the administrator account failed: {Errors}",
                    string.Join("; ", result.Errors.Select(E => E.Description)));
            }
        }
        #endregion

        #region ==== Demo Data ====
        /// <summary>
        /// Optional Sample Records So A Freshly Cloned Copy Shows A Populated Dashboard Instead
        /// Of Six Zeroes. Controlled By <c>Seed:DemoData</c> And Skipped Once Members Exist.
        /// </summary>
        private async Task SeedDemoDataAsync() {
            if (await _dbContext.Members.AnyAsync() || await _dbContext.Trainers.AnyAsync()) return;

            var trainers = new List<Trainer> {
                NewTrainer("Omar Hassan", "omar.hassan@powerfitness.com", "01012345601", Gender.Male, Specialties.Bodybuilding, 1994),
                NewTrainer("Nadia Farouk", "nadia.farouk@powerfitness.com", "01012345602", Gender.Female, Specialties.WeightLoss, 1991),
                NewTrainer("Karim Adel", "karim.adel@powerfitness.com", "01012345603", Gender.Male, Specialties.CrossFit, 1989),
                NewTrainer("Salma Nabil", "salma.nabil@powerfitness.com", "01012345604", Gender.Female, Specialties.NutritionCoaching, 1993),
            };
            _dbContext.Trainers.AddRange(trainers);

            var members = new List<Member> {
                NewMember("Youssef Amin", "youssef.amin@example.com", "01112345601", Gender.Male, 1997, 178, 82, BloodType.OPositive),
                NewMember("Mariam Saad", "mariam.saad@example.com", "01112345602", Gender.Female, 1999, 165, 58, BloodType.APositive),
                NewMember("Hana Youssef", "hana.youssef@example.com", "01112345603", Gender.Female, 1996, 170, 63, BloodType.BPositive),
                NewMember("Tarek Mostafa", "tarek.mostafa@example.com", "01112345604", Gender.Male, 1992, 183, 91, BloodType.ABPositive),
                NewMember("Laila Ibrahim", "laila.ibrahim@example.com", "01112345605", Gender.Female, 2000, 162, 55, BloodType.ONegative),
                NewMember("Ahmed Zaki", "ahmed.zaki@example.com", "01112345606", Gender.Male, 1988, 175, 88, BloodType.ANegative),
            };
            _dbContext.Members.AddRange(members);
            await _dbContext.SaveChangesAsync();

            var plans = await _dbContext.Plans.OrderBy(P => P.Price).ToListAsync();
            var categories = await _dbContext.Categories.ToListAsync();
            if (plans.Count == 0 || categories.Count == 0) return;

            // Memberships: Most Members Currently Subscribed, One Expired To Exercise Both States.
            var today = DateTime.Now.Date;
            for (int i = 0; i < members.Count; i++) {
                var plan = plans[i % plans.Count];
                var expired = i == members.Count - 1;
                var start = expired ? today.AddDays(-(plan.DurationDays + 20)) : today.AddDays(-(i * 5 + 3));
                _dbContext.MemberShips.Add(new MemberShip {
                    MemberId = members[i].Id,
                    PlanId = plan.Id,
                    CreatedAt = DateOnly.FromDateTime(start),
                    EndDate = start.AddDays(plan.DurationDays),
                    PricePaid = plan.Price
                });
            }

            // Sessions Spread Across Completed / Ongoing / Upcoming So Every Status Chip Is Visible.
            var now = DateTime.Now;
            var sessions = new List<Session> {
                NewSession("Full body strength circuit focused on compound lifts and controlled tempo.", 20, now.AddDays(-6).Date.AddHours(18), now.AddDays(-6).Date.AddHours(19), categories[0].Id, trainers[0].Id),
                NewSession("Morning fat burning HIIT with rowing, skipping and bodyweight intervals.", 18, now.AddDays(-2).Date.AddHours(8), now.AddDays(-2).Date.AddHours(9), categories[categories.Count > 1 ? 1 : 0].Id, trainers[1].Id),
                NewSession("Ongoing open mat conditioning session for all fitness levels.", 15, now.AddHours(-1), now.AddHours(1), categories[categories.Count > 2 ? 2 : 0].Id, trainers[2].Id),
                NewSession("Evening boxing fundamentals: stance, footwork, jab-cross combinations.", 16, now.AddDays(1).Date.AddHours(19), now.AddDays(1).Date.AddHours(20).AddMinutes(30), categories[categories.Count > 2 ? 2 : 0].Id, trainers[2].Id),
                NewSession("CrossFit WOD with olympic lifting technique work and a metcon finisher.", 12, now.AddDays(2).Date.AddHours(17), now.AddDays(2).Date.AddHours(18), categories[categories.Count > 3 ? 3 : 0].Id, trainers[0].Id),
                NewSession("Guided mobility and recovery flow to close out the training week.", 22, now.AddDays(4).Date.AddHours(9), now.AddDays(4).Date.AddHours(10), categories[categories.Count > 1 ? 1 : 0].Id, trainers[3].Id),
            };
            _dbContext.Sessions.AddRange(sessions);
            await _dbContext.SaveChangesAsync();

            // A Handful Of Bookings On The Upcoming Sessions.
            var upcoming = sessions.Where(S => S.StartDate > now).ToList();
            for (int i = 0; i < upcoming.Count; i++) {
                for (int j = 0; j <= i && j < members.Count; j++) {
                    _dbContext.MemberSessions.Add(new MemberSession {
                        MemberId = members[j].Id,
                        SessionId = upcoming[i].Id,
                        CreatedAt = DateOnly.FromDateTime(today)
                    });
                }
            }
            await _dbContext.SaveChangesAsync();
        }

        private static Trainer NewTrainer(string name, string email, string phone, Gender gender, Specialties specialty, int birthYear) => new() {
            Name = name,
            Email = email,
            Phone = phone,
            Gender = gender,
            Specialties = specialty,
            DateOfBirth = new DateOnly(birthYear, 5, 12),
            Address = new Address { BuildingNumber = 14, Street = "El Nasr", City = "Cairo" },
            CreatedAt = DateOnly.FromDateTime(DateTime.Now.AddMonths(-8))
        };

        private static Member NewMember(string name, string email, string phone, Gender gender, int birthYear, decimal height, decimal weight, BloodType bloodType) => new() {
            Name = name,
            Email = email,
            Phone = phone,
            Gender = gender,
            DateOfBirth = new DateOnly(birthYear, 3, 21),
            Address = new Address { BuildingNumber = 7, Street = "Gameat El Dowal", City = "Giza" },
            CreatedAt = DateOnly.FromDateTime(DateTime.Now.AddMonths(-3)),
            HealthRecord = new HealthRecord { Height = height, Weight = weight, BloodType = bloodType }
        };

        private static Session NewSession(string description, int capacity, DateTime start, DateTime end, int categoryId, int trainerId) => new() {
            Description = description,
            Capacity = capacity,
            StartDate = start,
            EndDate = end,
            CategoryId = categoryId,
            TrainerId = trainerId,
            CreatedAt = DateOnly.FromDateTime(DateTime.Now)
        };
        #endregion

        private static List<T> LoadDataFromJsonFile<T>(string fileName) {
            // Path.Combine With Separate Segments, So The Lookup Works On Windows And Linux Alike.
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Data", fileName);
            if (!File.Exists(filePath)) return [];
            string data = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<T>>(data, options) ?? [];
        }
    }
}
