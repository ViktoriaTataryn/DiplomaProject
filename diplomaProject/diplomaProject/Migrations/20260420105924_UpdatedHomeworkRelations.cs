using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace diplomaProject.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedHomeworkRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LessonId",
                table: "Questions",
                newName: "HomeworkId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_HomeworkId",
                table: "Questions",
                column: "HomeworkId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Homeworks_HomeworkId",
                table: "Questions",
                column: "HomeworkId",
                principalTable: "Homeworks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Homeworks_HomeworkId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_HomeworkId",
                table: "Questions");

            migrationBuilder.RenameColumn(
                name: "HomeworkId",
                table: "Questions",
                newName: "LessonId");
        }
    }
}
