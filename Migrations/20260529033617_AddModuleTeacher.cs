using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdaptiveLearningSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleTeacher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TeacherId",
                table: "LearningModules",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningModules_TeacherId",
                table: "LearningModules",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_LearningModules_AspNetUsers_TeacherId",
                table: "LearningModules",
                column: "TeacherId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LearningModules_AspNetUsers_TeacherId",
                table: "LearningModules");

            migrationBuilder.DropIndex(
                name: "IX_LearningModules_TeacherId",
                table: "LearningModules");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "LearningModules");
        }
    }
}
