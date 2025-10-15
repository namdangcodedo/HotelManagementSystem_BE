using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBackend.BusinessObjects.Migrations
{
    /// <inheritdoc />
    public partial class InitialAmentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Medium_Customer_CustomerId",
                table: "Medium");

            migrationBuilder.DropIndex(
                name: "IX_Medium_CustomerId",
                table: "Medium");

            migrationBuilder.DropIndex(
                name: "IX_Customer_AvatarMediaId",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Medium");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Medium",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceKey",
                table: "Medium",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReferenceTable",
                table: "Medium",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Amenity",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Customer_AvatarMediaId",
                table: "Customer",
                column: "AvatarMediaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customer_AvatarMediaId",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Medium");

            migrationBuilder.DropColumn(
                name: "ReferenceKey",
                table: "Medium");

            migrationBuilder.DropColumn(
                name: "ReferenceTable",
                table: "Medium");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Amenity");

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "Medium",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Medium_CustomerId",
                table: "Medium",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_AvatarMediaId",
                table: "Customer",
                column: "AvatarMediaId",
                unique: true,
                filter: "[AvatarMediaId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Medium_Customer_CustomerId",
                table: "Medium",
                column: "CustomerId",
                principalTable: "Customer",
                principalColumn: "CustomerId");
        }
    }
}
