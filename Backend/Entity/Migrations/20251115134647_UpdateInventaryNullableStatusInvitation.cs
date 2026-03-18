using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entity.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInventaryNullableStatusInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inventary_InvitationCode",
                schema: "System",
                table: "Inventary");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "System",
                table: "Inventary",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "InvitationCode",
                schema: "System",
                table: "Inventary",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.CreateIndex(
                name: "IX_Inventary_InvitationCode",
                schema: "System",
                table: "Inventary",
                column: "InvitationCode",
                unique: true,
                filter: "[InvitationCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inventary_InvitationCode",
                schema: "System",
                table: "Inventary");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "System",
                table: "Inventary",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvitationCode",
                schema: "System",
                table: "Inventary",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventary_InvitationCode",
                schema: "System",
                table: "Inventary",
                column: "InvitationCode",
                unique: true);
        }
    }
}
