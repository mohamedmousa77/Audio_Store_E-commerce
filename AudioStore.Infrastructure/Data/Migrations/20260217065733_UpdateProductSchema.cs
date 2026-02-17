using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AudioStore.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Features",
                table: "Products",
                newName: "Specifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Specifications",
                table: "Products",
                newName: "Features");
        }
    }
}
