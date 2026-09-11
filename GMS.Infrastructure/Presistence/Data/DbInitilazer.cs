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

            if (_seedOptions.SeedDemoData) {
                await SeedDemoDataAsync();
                await SeedMemberCohortAsync();
                await SeedOperationsDemoDataAsync();
            }
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

            // The Operations Tables Stand Alone — No Foreign Keys Into The People Above — But They
            // Are Demo Data Too, So A Reset That Left Them Behind Would Reload Duplicates.
            _dbContext.Employees.RemoveRange(_dbContext.Employees);
            _dbContext.InventoryItems.RemoveRange(_dbContext.InventoryItems);
            _dbContext.Expenses.RemoveRange(_dbContext.Expenses);
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
        /// A Membership Base Of Realistic Size, With Its Trading History.
        ///
        /// The Six Named Members Above Exist To Show The People Screens. They Are Not A Gym: Six
        /// Subscriptions Cannot Carry Six Staff And A Rent Bill, So Before This The Profit And
        /// Loss View Reported A -9,000% Margin And Read As Broken Rather Than As Bad News. This
        /// Adds The Rest Of The Roll And Their Renewals Across The Last Six Months, So Revenue And
        /// Costs Describe One Coherent Gym.
        ///
        /// Guarded On Its Own Count Rather Than On SeedDemoDataAsync's, So It Still Fills An
        /// Existing Database That Already Has The Six.
        /// </summary>
        private async Task SeedMemberCohortAsync() {
            const int target = 120;
            var existing = await _dbContext.Members.CountAsync();
            if (existing >= target) return;

            var plans = await _dbContext.Plans.OrderBy(P => P.Price).ToListAsync();
            if (plans.Count == 0) return;

            string[] firstNames = [
                "Adeel", "Aiman", "Amna", "Anum", "Arsalan", "Asad", "Ayesha", "Bilal", "Danish", "Faiza",
                "Farhan", "Hamid", "Hareem", "Hassan", "Hiba", "Imran", "Iqra", "Junaid", "Kamran", "Kashif",
                "Khadija", "Laiba", "Mahnoor", "Maryam", "Mehwish", "Moiz", "Nabeel", "Nimra", "Noman", "Rabia",
                "Rehan", "Sadia", "Saad", "Sahar", "Salman", "Samra", "Shahzad", "Sobia", "Sohail", "Tahir",
                "Talha", "Uzair", "Wajiha", "Waqas", "Yasir", "Zara", "Zeeshan", "Zoya", "Areeba", "Basit",
            ];
            string[] lastNames = [
                "Ahmed", "Akhtar", "Ali", "Aslam", "Baig", "Butt", "Chaudhry", "Farooq", "Gill", "Hashmi",
                "Hussain", "Iqbal", "Javed", "Khan", "Malik", "Mirza", "Mughal", "Nawaz", "Qureshi", "Rana",
                "Rashid", "Saeed", "Shah", "Sheikh", "Siddiqui", "Tariq", "Yousaf", "Zafar",
            ];
            (string Street, string City)[] places = [
                ("DHA Phase 5", "Lahore"), ("Gulberg III", "Lahore"), ("Model Town", "Lahore"),
                ("Clifton Block 5", "Karachi"), ("Gulshan e Iqbal", "Karachi"), ("North Nazimabad", "Karachi"),
                ("F 7 Markaz", "Islamabad"), ("G 11 Markaz", "Islamabad"), ("I 8 Markaz", "Islamabad"),
                ("Bahria Town", "Rawalpindi"), ("Saddar", "Rawalpindi"),
            ];
            BloodType[] bloodTypes = [BloodType.OPositive, BloodType.APositive, BloodType.BPositive,
                                      BloodType.ABPositive, BloodType.ONegative, BloodType.ANegative];

            // Fixed Seed: The Sample Data Must Look The Same On Every Machine, Or Two People
            // Comparing Screens Would See Different Numbers And Suspect A Bug.
            var random = new Random(20260911);
            var today = DateTime.Now.Date;
            var windowStart = today.AddMonths(-6);

            var taken = (await _dbContext.Members.Select(M => M.Email).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var members = new List<Member>();

            for (var i = existing; i < target; i++) {
                var first = firstNames[random.Next(firstNames.Length)];
                var last = lastNames[random.Next(lastNames.Length)];
                var email = $"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}{i}@example.com";
                if (!taken.Add(email)) continue;

                var place = places[random.Next(places.Length)];
                var female = "Ayesha Amna Anum Faiza Hareem Hiba Iqra Khadija Laiba Mahnoor Maryam Mehwish Nimra Rabia Sadia Sahar Samra Sobia Wajiha Zara Zoya Aiman Areeba".Contains(first);

                members.Add(NewMember(
                    $"{first} {last}", email,
                    // 0321 Plus A Zero-Padded Counter Keeps Every Number Unique And 11 Digits Long.
                    $"0321{(4_000_000 + i):D7}",
                    female ? Gender.Female : Gender.Male,
                    1985 + random.Next(20),
                    155 + random.Next(35),
                    50 + random.Next(45),
                    bloodTypes[random.Next(bloodTypes.Length)],
                    place.Street, place.City));
            }

            if (members.Count == 0) return;
            _dbContext.Members.AddRange(members);
            await _dbContext.SaveChangesAsync();

            // Each Member Joins At Some Point In The Window And Then Renews On Their Plan's Own
            // Cycle Up To Today. That Is What Puts Revenue In Every Month Instead Of One Spike,
            // And It Is How A Real Roll Behaves.
            var memberships = new List<MemberShip>();
            foreach (var member in members) {
                var plan = plans[random.Next(plans.Count)];
                var joined = windowStart.AddDays(random.Next(0, 170));

                for (var start = joined; start <= today; start = start.AddDays(plan.DurationDays)) {
                    memberships.Add(new MemberShip {
                        MemberId = member.Id,
                        PlanId = plan.Id,
                        CreatedAt = DateOnly.FromDateTime(start),
                        EndDate = start.AddDays(plan.DurationDays),
                        PricePaid = plan.Price,
                    });
                }
            }

            _dbContext.MemberShips.AddRange(memberships);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Seeded {Members} additional demo members and {Contracts} membership contracts.",
                                   members.Count, memberships.Count);
        }

        /// <summary>
        /// Sample Staff, Stock And Spending So The Operations Screens And The Profit And Loss
        /// View Have Something To Show On A Fresh Clone. Guarded Independently Of The People
        /// Seeder Above, So Adding These Modules To An Existing Database Still Fills Them.
        /// </summary>
        private async Task SeedOperationsDemoDataAsync() {
            var today = DateOnly.FromDateTime(DateTime.Now);

            if (!await _dbContext.Employees.AnyAsync()) {
                _dbContext.Employees.AddRange(
                    NewEmployee("Nadia Hussain", "nadia.hussain@powerfitness.pk", "03451234701", Gender.Female,
                                JobTitle.Receptionist, EmploymentType.FullTime, 35_000m, 1996, 14, "G 11 3", "Islamabad", monthsAgo: 26),
                    NewEmployee("Imran Qureshi", "imran.qureshi@powerfitness.pk", "03451234702", Gender.Male,
                                JobTitle.Manager, EmploymentType.FullTime, 75_000m, 1985, 7, "F 7 Markaz", "Islamabad", monthsAgo: 41),
                    NewEmployee("Rashid Mehmood", "rashid.mehmood@powerfitness.pk", "03451234703", Gender.Male,
                                JobTitle.Maintenance, EmploymentType.FullTime, 28_000m, 1990, 22, "Bahria Town", "Rawalpindi", monthsAgo: 17),
                    NewEmployee("Shazia Bibi", "shazia.bibi@powerfitness.pk", "03451234704", Gender.Female,
                                JobTitle.Cleaner, EmploymentType.PartTime, 18_000m, 1993, 5, "Saddar", "Rawalpindi", monthsAgo: 11),
                    NewEmployee("Tariq Jameel", "tariq.jameel@powerfitness.pk", "03451234705", Gender.Male,
                                JobTitle.Security, EmploymentType.FullTime, 25_000m, 1987, 31, "G 9 Markaz", "Islamabad", monthsAgo: 33),
                    NewEmployee("Kiran Shah", "kiran.shah@powerfitness.pk", "03451234706", Gender.Female,
                                JobTitle.Sales, EmploymentType.Contract, 32_000m, 1998, 9, "Blue Area", "Islamabad", monthsAgo: 6),
                    // One Former Employee, So The Status Filter And The Payroll Exclusion Both Have
                    // Something Real To Act On Rather Than Reading As Dead Options.
                    NewEmployee("Faisal Abbas", "faisal.abbas@powerfitness.pk", "03451234707", Gender.Male,
                                JobTitle.FloorSupervisor, EmploymentType.FullTime, 40_000m, 1991, 3, "I 8 Markaz", "Islamabad",
                                monthsAgo: 29, isActive: false)
                );
            }

            if (!await _dbContext.InventoryItems.AnyAsync()) {
                _dbContext.InventoryItems.AddRange(
                    // Equipment — A Spread Of Conditions And Service Dates, Including One Overdue.
                    NewEquipment("Commercial Treadmill", "Life Fitness T5, 4 units on the cardio floor", 4, 425_000m,
                                 "Fitness World, Rawalpindi", "Cardio floor", ItemCondition.Good, serviceInDays: 24, boughtMonthsAgo: 19),
                    NewEquipment("Elliptical Cross Trainer", "Front-drive, self-powered", 3, 285_000m,
                                 "Fitness World, Rawalpindi", "Cardio floor", ItemCondition.Good, serviceInDays: 55, boughtMonthsAgo: 19),
                    NewEquipment("Olympic Barbell 20kg", "Knurled, 200kg rated", 8, 32_000m,
                                 "Iron Grip, Lahore", "Free weights", ItemCondition.New, serviceInDays: null, boughtMonthsAgo: 4),
                    NewEquipment("Rubber Bumper Plate Set", "5kg to 25kg pairs", 6, 78_000m,
                                 "Iron Grip, Lahore", "Free weights", ItemCondition.Good, serviceInDays: null, boughtMonthsAgo: 4),
                    NewEquipment("Adjustable Bench", "Flat, incline and decline", 6, 46_000m,
                                 "Iron Grip, Lahore", "Free weights", ItemCondition.Good, serviceInDays: 71, boughtMonthsAgo: 14),
                    NewEquipment("Cable Crossover Machine", "Dual stack, 90kg per side", 1, 520_000m,
                                 "Fitness World, Rawalpindi", "Strength area", ItemCondition.NeedsService, serviceInDays: -9, boughtMonthsAgo: 28),
                    NewEquipment("Spin Bike", "Belt drive, magnetic resistance", 10, 68_000m,
                                 "Cycle Pro, Karachi", "Studio 1", ItemCondition.Good, serviceInDays: 12, boughtMonthsAgo: 9),
                    NewEquipment("Rowing Machine", "Air resistance, performance monitor", 2, 195_000m,
                                 "Fitness World, Rawalpindi", "Cardio floor", ItemCondition.OutOfService, serviceInDays: -3, boughtMonthsAgo: 33),

                    // Consumables — Two Deliberately At Or Below Their Reorder Level.
                    NewConsumable("Gym Towels", "White cotton, member issue", 64, 450m, "Textile Mart, Faisalabad", reorderLevel: 40),
                    NewConsumable("Whey Protein 1kg", "Chocolate, retail counter", 9, 6_800m, "Nutrition Hub, Lahore", reorderLevel: 12),
                    NewConsumable("Mineral Water 1.5L", "Cases of 12", 18, 1_100m, "Aqua Supplies, Islamabad", reorderLevel: 25),
                    NewConsumable("Disinfectant Spray 5L", "Equipment wipe-down", 7, 2_400m, "CleanCo, Rawalpindi", reorderLevel: 4),
                    NewConsumable("Chalk Blocks", "Box of 8", 14, 900m, "Iron Grip, Lahore", reorderLevel: 6),
                    NewConsumable("Resistance Bands", "Assorted strengths", 22, 1_600m, "Cycle Pro, Karachi", reorderLevel: 10)
                );
            }

            if (!await _dbContext.Expenses.AnyAsync()) {
                var expenses = new List<Expense>();

                // Six Months Of Running Costs. Recurring Lines Are Generated Rather Than Typed Out,
                // So The Profit And Loss Chart Has A Real Shape Instead Of One Spike.
                for (var back = 5; back >= 0; back--) {
                    var month = new DateOnly(today.Year, today.Month, 1).AddMonths(-back);
                    var label = new DateTime(month.Year, month.Month, 1).ToString("MMMM yyyy");

                    expenses.Add(NewExpense($"Premises rent — {label}", ExpenseCategory.Rent, 150_000m,
                                            month.AddDays(2), PaymentMethod.BankTransfer, "Gulberg Properties", $"RENT-{month:yyyyMM}"));
                    expenses.Add(NewExpense($"Staff salaries — {label}", ExpenseCategory.Salaries, 213_000m,
                                            month.AddDays(28) > today ? today : month.AddDays(28), PaymentMethod.BankTransfer, null, $"PAY-{month:yyyyMM}"));
                    expenses.Add(NewExpense($"Electricity — {label}", ExpenseCategory.Utilities, 48_000m + back * 3_000m,
                                            month.AddDays(11), PaymentMethod.Easypaisa, "IESCO", $"UTIL-{month:yyyyMM}"));
                    expenses.Add(NewExpense($"Water and gas — {label}", ExpenseCategory.Utilities, 12_000m,
                                            month.AddDays(12), PaymentMethod.JazzCash, "SNGPL", null));
                    expenses.Add(NewExpense($"Cleaning supplies — {label}", ExpenseCategory.Supplies, 9_500m,
                                            month.AddDays(6), PaymentMethod.Cash, "CleanCo, Rawalpindi", null));
                }

                // One-Off Purchases, Dated Into Particular Months So Not Every Month Looks Alike.
                expenses.Add(NewExpense("Cable crossover repair", ExpenseCategory.Maintenance, 18_000m,
                                        today.AddDays(-21), PaymentMethod.Cash, "Fitness World, Rawalpindi", "SRV-8821"));
                expenses.Add(NewExpense("Olympic barbells and plates", ExpenseCategory.Equipment, 180_000m,
                                        today.AddMonths(-4).AddDays(3), PaymentMethod.BankTransfer, "Iron Grip, Lahore", "INV-2026-0142"));
                expenses.Add(NewExpense("Ramadan membership campaign", ExpenseCategory.Marketing, 45_000m,
                                        today.AddMonths(-2).AddDays(8), PaymentMethod.Card, "Meta Ads", null));
                expenses.Add(NewExpense("Instagram and billboard spots", ExpenseCategory.Marketing, 28_000m,
                                        today.AddMonths(-1).AddDays(14), PaymentMethod.Card, "Adsell Media", null));
                expenses.Add(NewExpense("Public liability insurance", ExpenseCategory.Insurance, 60_000m,
                                        today.AddMonths(-3).AddDays(5), PaymentMethod.Cheque, "Jubilee Insurance", "POL-77213"));
                expenses.Add(NewExpense("Trade licence renewal", ExpenseCategory.Taxes, 22_000m,
                                        today.AddMonths(-5).AddDays(9), PaymentMethod.BankTransfer, "CDA", "LIC-2026"));
                expenses.Add(NewExpense("Studio mirror replacement", ExpenseCategory.Maintenance, 35_000m,
                                        today.AddMonths(-2).AddDays(19), PaymentMethod.Cash, "Glass House, Islamabad", null));

                _dbContext.Expenses.AddRange(expenses);
            }

            await _dbContext.SaveChangesAsync();
        }

        #region Operations Demo Builders
        private static Employee NewEmployee(string name, string email, string phone, Gender gender, JobTitle jobTitle,
                                            EmploymentType employmentType, decimal salary, int birthYear, int buildingNumber,
                                            string street, string city, int monthsAgo, bool isActive = true) {
            var hired = DateOnly.FromDateTime(DateTime.Now).AddMonths(-monthsAgo);
            return new Employee {
                Name = name,
                Email = email,
                Phone = phone,
                Gender = gender,
                DateOfBirth = new DateOnly(birthYear, 5, 12),
                JobTitle = jobTitle,
                EmploymentType = employmentType,
                MonthlySalary = salary,
                IsActive = isActive,
                Address = new Address { BuildingNumber = buildingNumber, Street = street, City = city },
                CreatedAt = hired,
                UpdatedAt = hired,
            };
        }

        private static InventoryItem NewEquipment(string name, string description, int quantity, decimal unitCost,
                                                  string supplier, string location, ItemCondition condition,
                                                  int? serviceInDays, int boughtMonthsAgo) {
            var today = DateOnly.FromDateTime(DateTime.Now);
            return new InventoryItem {
                Name = name,
                Description = description,
                Kind = ItemKind.Equipment,
                Quantity = quantity,
                UnitCost = unitCost,
                Supplier = supplier,
                Location = location,
                Condition = condition,
                // A Negative Number Of Days Is A Service That Has Already Slipped.
                NextServiceOn = serviceInDays.HasValue ? today.AddDays(serviceInDays.Value) : null,
                PurchasedOn = today.AddMonths(-boughtMonthsAgo),
                CreatedAt = today,
                UpdatedAt = today,
            };
        }

        private static InventoryItem NewConsumable(string name, string description, int quantity, decimal unitCost,
                                                   string supplier, int reorderLevel) {
            var today = DateOnly.FromDateTime(DateTime.Now);
            return new InventoryItem {
                Name = name,
                Description = description,
                Kind = ItemKind.Consumable,
                Quantity = quantity,
                UnitCost = unitCost,
                Supplier = supplier,
                ReorderLevel = reorderLevel,
                PurchasedOn = today.AddDays(-21),
                CreatedAt = today,
                UpdatedAt = today,
            };
        }

        private static Expense NewExpense(string title, ExpenseCategory category, decimal amount, DateOnly spentOn,
                                          PaymentMethod method, string? vendor, string? reference) => new() {
            Title = title,
            Category = category,
            Amount = amount,
            SpentOn = spentOn,
            PaymentMethod = method,
            Vendor = vendor,
            Reference = reference,
            CreatedAt = spentOn,
            UpdatedAt = spentOn,
        };
        #endregion

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
