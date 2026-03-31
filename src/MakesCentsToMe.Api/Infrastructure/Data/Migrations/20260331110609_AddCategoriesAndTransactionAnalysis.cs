using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MakesCentsToMe.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriesAndTransactionAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "Transactions",
                newName: "SuggestedCategory");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Confidence",
                table: "Transactions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedVendor",
                table: "Transactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawCategory",
                table: "Transactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Transactions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SuggestedCategoryId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedNormalizedVendor",
                table: "Transactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "IsDefault", "Name" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-0000-0000-000000000001"), true, "Dining" },
                    { new Guid("a1000000-0000-0000-0000-000000000002"), true, "Education" },
                    { new Guid("a1000000-0000-0000-0000-000000000003"), true, "Entertainment" },
                    { new Guid("a1000000-0000-0000-0000-000000000004"), true, "Fees and Charges" },
                    { new Guid("a1000000-0000-0000-0000-000000000005"), true, "Gifts and Donations" },
                    { new Guid("a1000000-0000-0000-0000-000000000006"), true, "Groceries" },
                    { new Guid("a1000000-0000-0000-0000-000000000007"), true, "Healthcare" },
                    { new Guid("a1000000-0000-0000-0000-000000000008"), true, "Housing" },
                    { new Guid("a1000000-0000-0000-0000-000000000009"), true, "Income" },
                    { new Guid("a1000000-0000-0000-0000-00000000000a"), true, "Insurance" },
                    { new Guid("a1000000-0000-0000-0000-00000000000b"), true, "Personal Care" },
                    { new Guid("a1000000-0000-0000-0000-00000000000c"), true, "Shopping" },
                    { new Guid("a1000000-0000-0000-0000-00000000000d"), true, "Transfer" },
                    { new Guid("a1000000-0000-0000-0000-00000000000e"), true, "Transportation" },
                    { new Guid("a1000000-0000-0000-0000-00000000000f"), true, "Travel" },
                    { new Guid("a1000000-0000-0000-0000-000000000010"), true, "Uncategorized" },
                    { new Guid("a1000000-0000-0000-0000-000000000011"), true, "Utilities" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CategoryId",
                table: "Transactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Dedup",
                table: "Transactions",
                columns: new[] { "AccountId", "Date", "Description", "Amount" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Status",
                table: "Transactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Categories_CategoryId",
                table: "Transactions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Categories_CategoryId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_CategoryId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_Dedup",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_Status",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "NormalizedVendor",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RawCategory",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SuggestedCategoryId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SuggestedNormalizedVendor",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "SuggestedCategory",
                table: "Transactions",
                newName: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions",
                column: "AccountId");
        }
    }
}
