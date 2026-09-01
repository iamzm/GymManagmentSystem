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

            // The Reset Runs First So The Seeders Below See An Empty Slate And Lay Everything
            // Down Again — Otherwise They Would Skip Tables The Reset Had Just Cleared.
            if (_seedOptions.ResetDemoData) {
                if (_seedOptions.IsDevelopment) {
                    await ResetDemoDataAsync();
                }
                else {
                    _logger.LogWarning(
                        "Seed:ResetDemoData is on, but this is not the Development environment, so the " +
                        "destructive reset was refused. Left to run it would delete every member, trainer, " +
                        "session, membership, booking and plan on each restart. Set it to false.");
                }
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

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing is not null) {
                await RecoverAdminAsync(existing, password);
                return;
            }

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

        /// <summary>
        /// The Administrator Already Exists. Normally There Is Nothing To Do — But With
        /// <c>Seed:ResetAdminPassword</c> On, Put The Configured Password Back, Clear Any Lockout
        /// And Restore The Admin Role, So A Deployment Nobody Can Sign Into Is Recoverable Without
        /// Hand-Editing The Database.
        /// </summary>
        private async Task RecoverAdminAsync(AppUser admin, string password) {
            if (!_seedOptions.ResetAdminPassword) {
                _logger.LogInformation(
                    "Administrator {Email} already exists; leaving it untouched. If nobody can sign in, " +
                    "set Seed:ResetAdminPassword=true once to reset it.", admin.Email);
                return;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(admin);
            var reset = await _userManager.ResetPasswordAsync(admin, token, password);

            if (!reset.Succeeded) {
                _logger.LogError("Resetting the administrator password failed: {Errors}",
                    string.Join("; ", reset.Errors.Select(E => E.Description)));
                return;
            }

            // A Reset Is Useless While The Account Is Still Serving Out A Lockout, Because Identity
            // Refuses A Locked-Out Sign-In Before It Ever Checks The Password.
            await _userManager.SetLockoutEndDateAsync(admin, null);
            await _userManager.ResetAccessFailedCountAsync(admin);

            if (!await _userManager.IsInRoleAsync(admin, AppRoles.Admin))
                await _userManager.AddToRoleAsync(admin, AppRoles.Admin);

            if (!admin.IsActive) {
                admin.IsActive = true;
                await _userManager.UpdateAsync(admin);
            }

            _logger.LogWarning(
                "Seed:ResetAdminPassword is on — the password for {Email} has been reset and any lockout " +
                "cleared. Turn it off now, or it resets again on every restart.", admin.Email);
        }
        #endregion

        #region ==== Demo Data ====
        /// <summary>
        /// Clears The Seeded Content So A Fresh Set Can Replace It: People, Their Sessions,
        /// Memberships And Bookings, Plus The Reference Data From The JSON Files — Plans And
        /// Categories — Since Their Prices Change With The Market Being Served. Login Accounts
        /// Survive, Because Credentials Are Not Sample Records.
        /// </summary>
        private async Task ResetDemoDataAsync() {
            var memberCount = await _dbContext.Members.CountAsync();
            var trainerCount = await _dbContext.Trainers.CountAsync();
            var planCount = await _dbContext.Plans.CountAsync();
            if (memberCount == 0 && trainerCount == 0 && planCount == 0) return;

            _logger.LogWarning(
                "Seed:ResetDemoData is on — deleting {Members} member(s), {Trainers} trainer(s) and " +
                "{Plans} plan(s) with their sessions, memberships and bookings, then reloading from " +
                "the seed files. Turn it off after the reload so it does not run again on the next start.",
                memberCount, trainerCount, planCount);

            // Children First: The Cascades Would Cover Most Of This, But Being Explicit Keeps The
            // Order Obvious And Survives Anyone Changing A Delete Behaviour Later.
            _dbContext.MemberSessions.RemoveRange(_dbContext.MemberSessions);
            _dbContext.MemberShips.RemoveRange(_dbContext.MemberShips);
            await _dbContext.SaveChangesAsync();

            _dbContext.Sessions.RemoveRange(_dbContext.Sessions);
            await _dbContext.SaveChangesAsync();

            _dbContext.Members.RemoveRange(_dbContext.Members);
            _dbContext.Trainers.RemoveRange(_dbContext.Trainers);
            await _dbContext.SaveChangesAsync();

            // Safe Only Now That Every Membership And Session Referencing Them Is Gone.
            _dbContext.Plans.RemoveRange(_dbContext.Plans);
            _dbContext.Categories.RemoveRange(_dbContext.Categories);
            await _dbContext.SaveChangesAsync();

            // Those Member Rows Are Gone, So Any Account Pointing At One Is Now Pointing At Nothing.
            var linked = await _dbContext.Users.Where(U => U.MemberId != null || U.TrainerId != null).ToListAsync();
            foreach (var user in linked) {
                user.MemberId = null;
                user.TrainerId = null;
            }
            if (linked.Count > 0) await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Optional Sample Records So A Freshly Cloned Copy Shows A Populated Dashboard Instead
        /// Of Six Zeroes. Controlled By <c>Seed:DemoData</c> And Skipped Once Members Exist.
        /// </summary>
        private async Task SeedDemoDataAsync() {
            if (await _dbContext.Members.AnyAsync() || await _dbContext.Trainers.AnyAsync()) return;

            var trainers = new List<Trainer> {
                NewTrainer("Bilal Ahmed", "bilal.ahmed@powerfitness.pk", "03001234501", Gender.Male, Specialties.Bodybuilding, 1994, "Gulberg III", "Lahore"),
                NewTrainer("Ayesha Siddiqui", "ayesha.siddiqui@powerfitness.pk", "03001234502", Gender.Female, Specialties.WeightLoss, 1991, "Clifton Block 5", "Karachi"),
                NewTrainer("Hamza Sheikh", "hamza.sheikh@powerfitness.pk", "03001234503", Gender.Male, Specialties.CrossFit, 1989, "F-7 Markaz", "Islamabad"),
                NewTrainer("Sana Malik", "sana.malik@powerfitness.pk", "03001234504", Gender.Female, Specialties.NutritionCoaching, 1993, "Bahria Town", "Rawalpindi"),
            };
            _dbContext.Trainers.AddRange(trainers);

            var members = new List<Member> {
                NewMember("Ali Raza", "ali.raza@example.com", "03211234601", Gender.Male, 1997, 178, 82, BloodType.OPositive, "DHA Phase 5", "Lahore"),
                NewMember("Fatima Khan", "fatima.khan@example.com", "03211234602", Gender.Female, 1999, 165, 58, BloodType.APositive, "Gulshan-e-Iqbal", "Karachi"),
                NewMember("Zainab Iqbal", "zainab.iqbal@example.com", "03211234603", Gender.Female, 1996, 170, 63, BloodType.BPositive, "G-11 Markaz", "Islamabad"),
                NewMember("Usman Tariq", "usman.tariq@example.com", "03211234604", Gender.Male, 1992, 183, 91, BloodType.ABPositive, "Model Town", "Lahore"),
                NewMember("Hina Yousaf", "hina.yousaf@example.com", "03211234605", Gender.Female, 2000, 162, 55, BloodType.ONegative, "Saddar", "Rawalpindi"),
                NewMember("Ahmed Nawaz", "ahmed.nawaz@example.com", "03211234606", Gender.Male, 1988, 175, 88, BloodType.ANegative, "North Nazimabad", "Karachi"),
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

            await SeedDemoLoginsAsync(members[0], trainers[0]);
        }

        /// <summary>
        /// Gives One Demo Member And One Demo Trainer A Login, So A Fresh Clone Can Be Signed Into
        /// As Each Role Rather Than Only As The Administrator. Development Sample Data Only —
        /// It Rides Along With <c>Seed:SeedDemoData</c>.
        /// </summary>
        private async Task SeedDemoLoginsAsync(Member member, Trainer trainer) {
            var password = _seedOptions.DemoPassword;
            if (string.IsNullOrWhiteSpace(password)) return;

            await CreateLoginAsync(member.Email, member.Name, AppRoles.Member, password, memberId: member.Id);
            await CreateLoginAsync(trainer.Email, trainer.Name, AppRoles.Trainer, password, trainerId: trainer.Id);
        }

        private async Task CreateLoginAsync(string email, string fullName, string role, string password,
                                            int? memberId = null, int? trainerId = null) {
            var existing = await _userManager.FindByEmailAsync(email);
            if (existing is not null) {
                await RecoverAdminAsync(existing, password);
                return;
            }

            var user = new AppUser {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                MemberId = memberId,
                TrainerId = trainerId
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded) {
                await _userManager.AddToRoleAsync(user, role);
                _logger.LogInformation("Seeded the demo {Role} login {Email}.", role, email);
            }
            else {
                _logger.LogError("Seeding the demo {Role} login failed: {Errors}",
                    role, string.Join("; ", result.Errors.Select(E => E.Description)));
            }
        }

        private static Trainer NewTrainer(string name, string email, string phone, Gender gender, Specialties specialty, int birthYear, string street, string city) => new() {
            Name = name,
            Email = email,
            Phone = phone,
            Gender = gender,
            Specialties = specialty,
            DateOfBirth = new DateOnly(birthYear, 5, 12),
            Address = new Address { BuildingNumber = 14, Street = street, City = city },
            CreatedAt = DateOnly.FromDateTime(DateTime.Now.AddMonths(-8))
        };

        private static Member NewMember(string name, string email, string phone, Gender gender, int birthYear, decimal height, decimal weight, BloodType bloodType, string street, string city) => new() {
            Name = name,
            Email = email,
            Phone = phone,
            Gender = gender,
            DateOfBirth = new DateOnly(birthYear, 3, 21),
            Address = new Address { BuildingNumber = 7, Street = street, City = city },
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
