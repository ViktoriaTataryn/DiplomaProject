using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace diplomaProject.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToModuleAndLessonIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Modules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LessonIndex",
                table: "Lessons",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "LessonIndex",
                table: "Lessons");
        }
    }
}
