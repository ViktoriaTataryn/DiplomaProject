using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace diplomaProject.Migrations
{
    /// <inheritdoc />
    public partial class FinalUserProgressSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserProgresses",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "LessonId",
                table: "UserProgresses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModuleId",
                table: "UserProgresses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserProgresses_LessonId",
                table: "UserProgresses",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProgresses_ModuleId",
                table: "UserProgresses",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProgresses_UserId",
                table: "UserProgresses",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserProgresses_AspNetUsers_UserId",
                table: "UserProgresses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProgresses_Lessons_LessonId",
                table: "UserProgresses",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProgresses_Modules_ModuleId",
                table: "UserProgresses",
                column: "ModuleId",
                principalTable: "Modules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserProgresses_AspNetUsers_UserId",
                table: "UserProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProgresses_Lessons_LessonId",
                table: "UserProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProgresses_Modules_ModuleId",
                table: "UserProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserProgresses_LessonId",
                table: "UserProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserProgresses_ModuleId",
                table: "UserProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserProgresses_UserId",
                table: "UserProgresses");

            migrationBuilder.DropColumn(
                name: "LessonId",
                table: "UserProgresses");

            migrationBuilder.DropColumn(
                name: "ModuleId",
                table: "UserProgresses");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserProgresses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
