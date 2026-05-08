using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddSpendingLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpendingLimits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Food = table.Column<double>(type: "double precision", nullable: false),
                    Education = table.Column<double>(type: "double precision", nullable: false),
                    Transport = table.Column<double>(type: "double precision", nullable: false),
                    Entertainment = table.Column<double>(type: "double precision", nullable: false),
                    Shopping = table.Column<double>(type: "double precision", nullable: false),
                    Subscriptions = table.Column<double>(type: "double precision", nullable: false),
                    Mobile = table.Column<double>(type: "double precision", nullable: false),
                    Others = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpendingLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpendingLimits_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpendingLimits_UserId",
                table: "SpendingLimits",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpendingLimits");
        }
    }
}
