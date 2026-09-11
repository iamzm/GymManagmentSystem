using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationsModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "GymUserEmailValidCheck1",
                table: "Trainers");

            migrationBuilder.DropCheckConstraint(
                name: "GymUserPhoneValidCheck1",
                table: "Trainers");

            migrationBuilder.DropCheckConstraint(
                name: "GymUserEmailValidCheck",
                table: "Members");

            migrationBuilder.DropCheckConstraint(
                name: "GymUserPhoneValidCheck",
                table: "Members");

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobTitle = table.Column<int>(type: "int", nullable: false),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    MonthlySalary = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Photo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    BuildingNumber = table.Column<int>(type: "int", nullable: false),
                    Address_Street = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Address_City = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.CheckConstraint("EmployeeSalaryCheck", "MonthlySalary >= 0");
                    table.CheckConstraint("GymUserEmailValidCheck", "Email Like '_%@_%._%'");
                    table.CheckConstraint("GymUserPhoneValidCheck", "Phone Not Like '%[^0-9]%' and Len(Phone) >= 10");
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    SpentOn = table.Column<DateOnly>(type: "date", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    Vendor = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true),
                    Reference = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateOnly>(type: "date", nullable: false),
                    UpdatedAt = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.CheckConstraint("ExpenseAmountCheck", "Amount > 0");
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Supplier = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true),
                    PurchasedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ReorderLevel = table.Column<int>(type: "int", nullable: true),
                    Location = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true),
                    Condition = table.Column<int>(type: "int", nullable: true),
                    NextServiceOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateOnly>(type: "date", nullable: false),
                    UpdatedAt = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.CheckConstraint("InventoryQuantityCheck", "Quantity >= 0");
                    table.CheckConstraint("InventoryReorderLevelCheck", "ReorderLevel Is Null Or ReorderLevel >= 0");
                    table.CheckConstraint("InventoryUnitCostCheck", "UnitCost >= 0");
                });

            migrationBuilder.AddCheckConstraint(
                name: "GymUserEmailValidCheck2",
                table: "Trainers",
                sql: "Email Like '_%@_%._%'");

            migrationBuilder.AddCheckConstraint(
                name: "GymUserPhoneValidCheck2",
                table: "Trainers",
                sql: "Phone Not Like '%[^0-9]%' and Len(Phone) >= 10");

            migrationBuilder.AddCheckConstraint(
                name: "GymUserEmailValidCheck1",
                table: "Members",
                sql: "Email Like '_%@_%._%'");

            migrationBuilder.AddCheckConstraint(
                name: "GymUserPhoneValidCheck1",
                table: "Members",
                sql: "Phone Not Like '%[^0-9]%' and Len(Phone) >= 10");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Phone",
                table: "Employees",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SpentOn",
                table: "Expenses",
                column: "SpentOn");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Kind",
                table: "InventoryItems",
                column: "Kind");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropCheckConstraint(
                name: "GymUserEmailValidCheck2",
                table: "Trainers");

            migrationBuilder.DropCheckConstraint(
                name: "GymUserPhoneValidCheck2",
                table: "Trainers");

            migrationBuilder.DropCheckConstraint(
                name: "GymUserEmailValidCheck1",
                table: "Members");

            migrationBuilder.DropCheckConstraint(
                name: "GymUserPhoneValidCheck1",
                table: "Members");

            migrationBuilder.AddCheckConstraint(
                name: "GymUserEmailValidCheck1",
                table: "Trainers",
                sql: "Email Like '_%@_%._%'");

            migrationBuilder.AddCheckConstraint(
                name: "GymUserPhoneValidCheck1",
                table: "Trainers",
                sql: "Phone Not Like '%[^0-9]%' and Len(Phone) >= 10");

            migrationBuilder.AddCheckConstraint(
                name: "GymUserEmailValidCheck",
                table: "Members",
                sql: "Email Like '_%@_%._%'");

            migrationBuilder.AddCheckConstraint(
                name: "GymUserPhoneValidCheck",
                table: "Members",
                sql: "Phone Not Like '%[^0-9]%' and Len(Phone) >= 10");
        }
    }
}
