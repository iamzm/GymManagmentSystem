# 🏋️ Power Fitness — Gym Management System

A full-featured **Gym Management System** built with **ASP.NET Core MVC (.NET 10)**, designed around clean, layered architecture and solid backend design patterns. The system manages gym members, trainers, subscription plans, class sessions, membership contracts and session bookings — behind role-based authentication, with a live analytics dashboard on top.

---

## 📖 Overview

Power Fitness is split into distinct modules, each responsible for one part of the gym's business:

| Module | Description |
|---|---|
| 🌐 **Public site** | Marketing landing page driven by real data: live member/trainer counters, the plans actually on sale and the classes genuinely coming up this week. |
| 🔐 **Accounts** | ASP.NET Core Identity: sign-in, self-registration, password change, profile, and role-based access (Admin / Trainer / Member). |
| 📊 **Dashboard** | The signed-in home screen: live gym metrics, booking-activity chart, plan distribution, renewals due and the week ahead. |
| 👥 **Members** | Register and manage members, with photo upload, a personal health record (incl. derived BMI), search and status filters. |
| 🏋️ **Trainers** | Trainer profiles with a specialty, photo, workload counters and the classes they lead. |
| 📅 **Sessions** | Gym classes with a category, an assigned trainer, a time window and a capacity — plus search and status filters. |
| 💳 **Plans** | Subscription plans with price, duration and an activate/deactivate toggle. |
| 🎫 **Memberships** | The contract linking a Member to a Plan: sell, renew and cancel, with expiry tracking and revenue captured at sale time. |
| 🔄 **My Plan** | A member's own screen: their current term, any upgrade or downgrade already booked, and the switch itself — always effective when the paid-for term ends. |
| 🗓️ **Sessions Schedule** | A weekly timetable you can page through, and a per-class roster where staff book members in and release seats. |

---

## 🏗️ Architecture

The solution follows an **N-Layer / Clean Architecture** approach, so that every layer has a single, clear responsibility and dependencies only ever point inward.

```
Gym_Managment_System/
│
├── GMS.Core/
│   ├── Domin/                      → Domain layer: entities, enums, repository & UoW contracts
│   ├── Service/                    → Business logic, implementations, AutoMapper profiles, Specifications
│   └── Service.Abstraction/        → Service interfaces (contracts) consumed by the presentation layer
│
├── GMS.Infrastructure/
│   ├── Presistence/                → EF Core DbContext, entity configurations, migrations, repositories, Identity
│   └── Presentation/               → Shared MVC presentation-layer controllers/helpers
│
├── Shared/
│   └── DTOs/                       → DTOs grouped per module (Members, Trainers, Plans, Sessions,
│                                     Memberships, Bookings, Analytics)
│
└── GMS.MVC/
    ├── Controllers/                → ASP.NET Core MVC controllers (talk only to the Service layer)
    ├── Models/                     → View models for the account screens and shared partials
    ├── Services/                   → Presentation-layer services (attachment storage, policy names)
    ├── Views/                      → Razor views (three layouts: app shell, public, auth)
    ├── wwwroot/css/app.css         → The design system (tokens + components)
    └── Program.cs                  → App startup, DI registration, middleware pipeline
```

**Why this structure?**
- The **Domain** layer has zero dependency on EF Core, ASP.NET, or any external framework — it only defines entities and contracts.
- The **Service** layer implements business rules against the abstractions, never against EF Core directly.
- The **MVC** layer never touches the database or entities directly — it only knows about DTOs and service interfaces.
- **DTOs** live in a shared project so the Domain entities never leak outside the Core.
- **Identity** lives in Infrastructure, where the framework dependency belongs; the Core knows nothing about it.

---

## 🧩 Design Patterns & Techniques

- **Repository + Unit of Work** — a generic repository (`IGenericRepository<T>`) plus specific repositories (Plan, Session, Membership, Booking) coordinated through a single `IUnitOfWork`, so every request commits as one atomic operation.
- **Specification Pattern** — encapsulates filtering/including logic (e.g. `MemberWithHealthRecordSpecification`) so repositories stay generic and query logic stays testable and reusable.
- **Service Manager Pattern** — a single `IServiceManger` exposes every service, so controllers depend on one entry point instead of seven.
- **DTO Pattern** — dedicated DTOs per operation (e.g. `CreateMemberDTO`, `MemberToUpdateDTO`, `MemberDetailsDTO`) instead of exposing entities to the views.
- **Result tuples for business outcomes** — the Membership and Booking services return `(bool Success, string Message)` so the UI can explain *why* an action was refused instead of showing a generic failure.
- **AutoMapper** — maps entities ↔ DTOs through dedicated profiles per module.
- **Fluent API Configurations** — each entity has its own `IEntityTypeConfiguration<T>` class instead of data annotations, keeping the entities clean.
- **Attachment Service** — a standalone `IAttachmentService` responsible for storing and deleting uploaded photos, declared in terms of `Stream` so the Core stays free of web-framework types.
- **DB Initializer** — applies pending migrations on startup, seeds reference data, roles and the bootstrap admin through `IDbInitilazer`.

---

## 🔐 Authentication & Authorization

Built on **ASP.NET Core Identity** with cookie authentication and a **fallback authorization policy** — every page requires a signed-in user unless it is explicitly marked `[AllowAnonymous]`.

**Roles**

| Role | Can do |
|---|---|
| **Admin** | Everything: create, edit and delete across every module. |
| **Trainer** | Read the gym records and the timetable; book and release class seats. |
| **Member** | Browse plans and the weekly class schedule. |

**How it is enforced**
- Named policies (`AppPolicies.AdminOnly`, `AppPolicies.StaffOnly`) instead of role strings scattered through the controllers.
- Role names live in `AppRoles` constants, so a typo becomes a compile error rather than a silent lockout.
- Every state-changing POST carries an anti-forgery token.
- Self-registration always lands in the least-privileged **Member** role; staff roles are granted by an administrator.
- Login failures give one message for both an unknown email and a wrong password, so the form can't be used to discover registered addresses. Accounts lock for 10 minutes after 5 failed attempts.
- Return URLs are validated with `Url.IsLocalUrl`, so `?returnUrl=` can't be turned into an open redirect.

**Bootstrap admin.** On first run the initializer seeds an administrator from configuration. The password is deliberately **blank in `appsettings.json`** — supply it per environment:

```bash
dotnet user-secrets set "Seed:AdminPassword" "<a strong password>" --project GMS.MVC
# or
export Seed__AdminPassword="<a strong password>"
```

If it is not set, the app logs a warning and seeds no admin. In **Development**, `appsettings.Development.json` supplies `Admin@123` for `admin@powerfitness.com` so a fresh clone can be signed into immediately — change it before deploying anywhere real.

---

## 🗂️ Domain Model

**Base entities**
- `BaseEntity` — `Id`, `CreatedAt`, `UpdatedAt` (inherited by every entity).
- `GymUser` (abstract) — shared identity fields: `Name`, `Email`, `Phone`, `DateOfBirth`, `Gender`, `Address`. Inherited by both `Member` and `Trainer`.
- `AppUser` (Identity) — the login account, deliberately separate from `Member`/`Trainer`: a person can exist in the gym records without ever signing in, and an admin account has no gym record at all.

**Core entities**
- **Member** *(inherits GymUser)* — has a `Photo`, one `HealthRecord`, many `MemberSession` bookings and many `MemberShip` contracts.
- **Trainer** *(inherits GymUser)* — has a `Specialties` value, a `Photo` and many `Session`s they lead.
- **HealthRecord** — `Height`, `Weight`, `BloodType`, optional `Note`. BMI is derived, never stored.
- **Category** — groups sessions (e.g. Boxing, CrossFit).
- **Session** — `Description`, `Capacity`, `StartDate`, `EndDate`, linked to one `Category` and one `Trainer`.
- **Plan** — `Name`, `Description`, `DurationDays`, `Price`, `IsActive` toggle.
- **MemberShip** — the contract between a `Member` and a `Plan`: `StartDate`, `EndDate`, `PricePaid`, and a computed `Status` (`Active` / `Expired`).
- **MemberSession** — the booking record linking a `Member` to a `Session`.

**Enums** — `Gender`, `BloodType` (8 types), `Specialties` (11 specialties).

**Money** — the currency is configuration, not something each view spells out. `Gym:Currency` sets the code, the numeric format, and whether the code leads or trails; views ask an injected `IMoneyFormatter` for a formatted amount. Defaults to **PKR**. Seeded plan prices live in `wwwroot/Data/plans.json`.

**Phone numbers** — validated as Pakistani mobiles (`03` followed by nine digits, e.g. `03001234567`) in the DTOs. The database check constraint only requires digits, deliberately: which national numbering plan applies is a validation rule, not something to bake into the schema, or serving another country would mean a migration.

### Changing plan

A member can upgrade or downgrade themselves from **My Plan**. The change never takes effect mid-term:

- A monthly plan renewing on the 10th hands over to the new plan **on the 10th** — the member keeps every day they have already paid for, and cover never breaks.
- A change is stored as an ordinary `MemberShip` row with a future start date, so there is no separate "pending change" table and no schema for it.
- Only one change can be queued at a time; choosing again replaces it, and it can be cancelled any time before it starts.
- The price is locked in when the change is booked, so a later price change does not affect it.
- With no active term, the chosen plan simply starts today.

Because a future-dated contract is still a row in `MemberShips`, "which plan is this member on?" had to become precise: **Active** means started *and* not yet ended, **Scheduled** means dated to begin later, and booking eligibility asks whether the member has cover *on the day of the class* rather than today.

**Business rules captured in the model**
- A session's status (Upcoming / Ongoing / Completed) is derived from comparing `StartDate`/`EndDate` to the current time — never stored as a static flag.
- A membership's `Active`/`Expired` status is computed on the fly from `EndDate`, so it is always accurate.
- A membership's `EndDate` is always derived from the plan's duration — a contract cannot outlive what was paid for.
- `PricePaid` is copied onto the contract at sale time, so a later price change never rewrites what a member paid.
- A renewal is recorded as a **new** contract rather than an edit, so subscription history stays intact.
- Sessions have a bounded `Capacity`, enforced at the booking level.
- A unique index on `(MemberId, SessionId)` means the database itself refuses a double booking, so a race between two requests can't slip past the service-level check.

**Booking rules** — every booking is validated against four rules: the class must still be in the future, it must have a free seat, the member must hold a live membership covering that date, and they must not already be booked into an overlapping class. The booking dropdown only offers members who pass all four, so it never presents a choice that would be rejected on submit.

---

## 🎨 The UI

A single design system in `wwwroot/css/app.css` — CSS custom properties for colour, type, spacing, radius, shadow and motion, then components built on those tokens: the app shell, cards, stat tiles, data tables, status pills, avatars, forms, toasts, empty states, meters and the weekly timetable grid.

- **Palette** — electric violet as the brand, volt green as the accent used sparingly for the one thing on a screen that should shout, and slate neutrals with a faint violet cast so greys sit beside the brand instead of fighting it. Volt always carries dark ink; white on volt fails contrast badly.
- **Type** — *Sora* for headings and figures (geometric, with enough character to carry a display number) and *Plus Jakarta Sans* for body text (modern and highly legible at small sizes). Both have full system fallback stacks.
- **Background** — a soft two-point radial wash behind the page, so large empty areas have some life without competing with the content on them.

- **Three layouts** — `_Layout` (the back-office shell with a dark sidebar), `_PublicLayout` (marketing pages) and `_AuthLayout` (split-screen sign-in).
- **Light and dark themes** — the toggle re-points the same tokens, so no component knows which theme it is in. The choice is stored per browser and applied before first paint, so there is no flash of the wrong theme.
- **Responsive** — the sidebar collapses to an overlay under 860px, the timetable reflows from seven columns to one, and tables scroll inside their own container rather than the page.
- **Accessible** — visible focus rings, `aria-label`s on icon-only controls, semantic headings, and a `prefers-reduced-motion` block that disables animation.
- **No chart library** — the two dashboard charts are drawn from the data as plain elements; two small shapes don't justify a dependency.

---

## 📊 Analytics Dashboard

The `AnalyticsService` computes every figure live from the database on each dashboard load:

- Members: total, active (people, not contracts — a renewal must not count twice), new this month
- Trainers and their workload
- Sessions: upcoming, ongoing, completed, and total bookings
- Memberships: active, expired and expiring within 7 days
- Revenue: total and this month
- Plan distribution across live contracts, and booking activity per day for the coming week

---

## 🛠️ Tech Stack

- **.NET 10** / **ASP.NET Core MVC**
- **Entity Framework Core 10** (Code-First, Fluent API, Migrations)
- **ASP.NET Core Identity**
- **SQL Server**
- **AutoMapper**
- **Razor Views** with tag helpers, partial views and shared layouts
- **Sora** + **Plus Jakarta Sans** (Google Fonts), **Bootstrap Icons** (self-hosted)
- **Bootstrap 5** grid and dropdowns, with a custom design system on top

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local or containerized)

### Run it

```bash
# 1. A SQL Server to point at (skip if you already have one)
docker run -d --name gymsql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<strong password>' \
  -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest

# 2. Point the app at it (or edit ConnectionStrings:DefaultConnections)
export ConnectionStrings__DefaultConnections="Server=localhost,1433;Database=GymManagmentSystem;User Id=sa;Password=<strong password>;TrustServerCertificate=True;"

# 3. Run — migrations are applied and reference data seeded on startup
dotnet run --project GMS.MVC
```

Then sign in at `/Account/Login`. In Development the seeder creates one login per role:

| Role | Email | Password |
|---|---|---|
| Admin | `admin@powerfitness.com` | `Admin@123` |
| Trainer | `bilal.ahmed@powerfitness.pk` | `Demo@123` |
| Member | `ali.raza@example.com` | `Demo@123` |

The trainer and member logins only appear when `Seed:SeedDemoData` and `Seed:DemoPassword` are set, and they are linked to the matching sample records. Anyone registering at `/Account/Register` lands in the **Member** role.

### Seeding

The `Seed` configuration section controls startup seeding:

| Key | Purpose |
|---|---|
| `Seed:AdminEmail` | Email for the bootstrap administrator. |
| `Seed:AdminPassword` | Its password. **Blank in `appsettings.json` on purpose** — supply per environment. |
| `Seed:AdminFullName` | Display name for that account. |
| `Seed:SeedDemoData` | When `true` (Development default), seeds sample trainers, members, memberships, sessions and bookings on an empty database, so a fresh clone shows a populated dashboard instead of six zeroes. |
| `Seed:DemoPassword` | Password for the demo member and trainer logins created alongside the sample records, so every role can be signed into. Blank means no demo logins. |
| `Seed:ResetDemoData` | **Destructive, off by default.** Turn on once to wipe the seeded content — people, sessions, memberships, bookings, plans and categories — and reload it all from the seed files, then turn it off. Login accounts are left alone. Use it after changing currency or plan prices, since plans are otherwise only seeded into an empty table. Development only. |

Categories and plans are seeded from `wwwroot/Data/*.json` when their tables are empty — so to change plan prices on a database that already has them, edit the JSON and run once with `Seed:ResetDemoData`.

### Serving a different market

Two settings and one file, no code:

| What | Where |
|---|---|
| Currency code and formatting | `Gym:Currency` in `appsettings.json` |
| Plan names, durations and prices | `wwwroot/Data/plans.json` |
| Mobile number format | the `[RegularExpression]` on the phone fields in the member and trainer DTOs |

The database only requires a phone to be digits — which national numbering plan applies is a validation rule, so changing country does not mean a migration.

---

## 🧭 Modules & Controllers

| Controller | Responsibilities | Access |
|---|---|---|
| `HomeController` | Public landing page, privacy, friendly error/status pages | Anonymous |
| `AccountController` | Login, register, logout, profile, change password, access denied | Mixed |
| `DashboardController` | Live analytics dashboard | Staff |
| `MembersController` | List (search + status filter), create, details, health record, edit, delete; photo upload | Staff reads, Admin writes |
| `TrainersController` | List (search + specialty filter), create, details, edit, delete; photo upload | Staff reads, Admin writes |
| `SessionController` | List (search + status filter), create, details, edit, delete | Staff reads, Admin writes |
| `PlansController` | List, details, edit, activate/deactivate | All read, Admin writes |
| `MembershipsController` | List (search + status filter), create, details, renew, cancel | Staff reads, Admin writes |
| `SessionsScheduleController` | Weekly timetable, class roster, book a seat, release a seat | All read, Staff books |

---

## 📌 Possible Next Steps

- Let members book their own classes (accounts already resolve to their gym record)
- A user-management screen so an admin can assign roles without touching the database
- Unit tests for the Service layer
- API endpoints alongside the MVC views
- Pagination on the list views (search and filtering are in place)
- Email notifications for expiring memberships and booking confirmations

---

## 📄 License

This project is open for learning and personal portfolio purposes.
