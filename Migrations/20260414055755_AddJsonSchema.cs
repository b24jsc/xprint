using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xprint.Migrations
{
    /// <inheritdoc />
    public partial class AddJsonSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JsonSchemaData",
                table: "Reports",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JsonSchemaData",
                table: "Reports");
        }
    }
}
