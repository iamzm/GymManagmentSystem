using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class RelaxPhoneCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "GymUserPhoneValidCheck1",
                table: "Trainers");

            migrationBuilder.DropCheckConstraint(
                name: "GymUserPhoneValidCheck",
                table: "Members");

            migrationBuilder.AddCheckConstraint(
                name: "GymUserPhoneValidCheck1",
                table: "Trainers",
                sql: "Phone Not Like '%[^0-9]%' and Len(Phone) >= 10");

            migrationBuilder.AddCheckConstraint(
                name: "GymUserPhoneValidCheck",
                table: "Members",
                sql: "Phone Not Like '%[^0-9]%' and Len(Phone) >= 10");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "GymUserPhoneValidCheck1",
                table: "Trainers");

            migrationBuilder.DropCheckConstraint(
                name: "GymUserPhoneValidCheck",
                table: "Members");

            migrationBuilder.AddCheckConstraint(
                name: "GymUserPhoneValidCheck1",
                table: "Trainers",
                sql: "Phone Like '01%' and Phone Not Like '%[^0-9]%'");

            migrationBuilder.AddCheckConstraint(
                name: "GymUserPhoneValidCheck",
                table: "Members",
                sql: "Phone Like '01%' and Phone Not Like '%[^0-9]%'");
        }
    }
}
