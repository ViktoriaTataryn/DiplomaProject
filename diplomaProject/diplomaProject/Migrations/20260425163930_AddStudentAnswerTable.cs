using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace diplomaProject.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentAnswerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HomeworkSubmissionId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    SelectedOptionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAnswers_AnswerOptions_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "AnswerOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentAnswers_HomeworkSubmissions_HomeworkSubmissionId",
                        column: x => x.HomeworkSubmissionId,
                        principalTable: "HomeworkSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_HomeworkSubmissionId",
                table: "StudentAnswers",
                column: "HomeworkSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_QuestionId",
                table: "StudentAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_SelectedOptionId",
                table: "StudentAnswers",
                column: "SelectedOptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentAnswers");
        }
    }
}
