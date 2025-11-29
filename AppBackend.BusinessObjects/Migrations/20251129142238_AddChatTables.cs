using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBackend.BusinessObjects.Migrations
{
    /// <inheritdoc />
    public partial class AddChatTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankConfig",
                columns: table => new
                {
                    BankConfigId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BankCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BankBranch = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankConfig", x => x.BankConfigId);
                });

            migrationBuilder.CreateTable(
                name: "ChatSession",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    GuestIdentifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSummarized = table.Column<bool>(type: "bit", nullable: false),
                    ConversationSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSession", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_ChatSession_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "AccountId");
                });

            migrationBuilder.CreateTable(
                name: "ChatMessage",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenCount = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessage", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_ChatMessage_ChatSession_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ChatSession",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_SessionId",
                table: "ChatMessage",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSession_AccountId",
                table: "ChatSession",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankConfig");

            migrationBuilder.DropTable(
                name: "ChatMessage");

            migrationBuilder.DropTable(
                name: "ChatSession");
        }
    }
}
