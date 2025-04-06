using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace schedule_2.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubgroupIdToStudents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubgroupId",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentSubgroups",
                columns: table => new
                {
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SubgroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSubgroups", x => new { x.StudentId, x.SubgroupId });
                    table.ForeignKey(
                        name: "FK_StudentSubgroups_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentSubgroups_Subgroups_SubgroupId",
                        column: x => x.SubgroupId,
                        principalTable: "Subgroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_SubgroupId",
                table: "Students",
                column: "SubgroupId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSubgroups_SubgroupId",
                table: "StudentSubgroups",
                column: "SubgroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Subgroups_SubgroupId",
                table: "Students",
                column: "SubgroupId",
                principalTable: "Subgroups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Subgroups_SubgroupId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "StudentSubgroups");

            migrationBuilder.DropIndex(
                name: "IX_Students_SubgroupId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "SubgroupId",
                table: "Students");
        }
    }
}
