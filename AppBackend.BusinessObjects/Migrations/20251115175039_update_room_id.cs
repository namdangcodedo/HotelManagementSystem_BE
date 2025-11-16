using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBackend.BusinessObjects.Migrations
{
    /// <inheritdoc />
    public partial class update_room_id : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Salary_CommonCode_StatusId",
                table: "Salary");

            migrationBuilder.AddForeignKey(
                name: "FK_Salary_CommonCode_StatusId",
                table: "Salary",
                column: "StatusId",
                principalTable: "CommonCode",
                principalColumn: "CodeId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Salary_CommonCode_StatusId",
                table: "Salary");

            migrationBuilder.AddForeignKey(
                name: "FK_Salary_CommonCode_StatusId",
                table: "Salary",
                column: "StatusId",
                principalTable: "CommonCode",
                principalColumn: "CodeId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
