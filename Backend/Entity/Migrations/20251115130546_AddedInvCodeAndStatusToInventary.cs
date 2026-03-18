using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entity.Migrations
{
    /// <inheritdoc />
    public partial class AddedInvCodeAndStatusToInventary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvitationCode",
                schema: "System",
                table: "Inventary",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "System",
                table: "Inventary",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Inventary_InvitationCode",
                schema: "System",
                table: "Inventary",
                column: "InvitationCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inventary_InvitationCode",
                schema: "System",
                table: "Inventary");

            migrationBuilder.DropColumn(
                name: "InvitationCode",
                schema: "System",
                table: "Inventary");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "System",
                table: "Inventary");
        }
    }
}
