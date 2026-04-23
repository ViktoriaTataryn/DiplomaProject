using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace diplomaProject.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "CourseRegistrations",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "CourseRegistrations");
        }
    }
}
