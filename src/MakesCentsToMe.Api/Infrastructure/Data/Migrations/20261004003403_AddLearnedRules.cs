using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MakesCentsToMe.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLearnedRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutoCategorized",
                table: "Transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LearnedRuleId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LearnedRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NormalizedVendor = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Pattern = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnedRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnedRules_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_LearnedRuleId",
                table: "Transactions",
                column: "LearnedRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnedRules_CategoryId",
                table: "LearnedRules",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnedRules_Pattern",
                table: "LearnedRules",
                column: "Pattern",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_LearnedRules_LearnedRuleId",
                table: "Transactions",
                column: "LearnedRuleId",
                principalTable: "LearnedRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_LearnedRules_LearnedRuleId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "LearnedRules");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_LearnedRuleId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsAutoCategorized",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "LearnedRuleId",
                table: "Transactions");
        }
    }
}
