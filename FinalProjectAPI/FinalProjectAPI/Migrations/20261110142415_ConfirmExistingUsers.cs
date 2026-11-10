using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProjectAPI.Migrations
{
    /// <inheritdoc />
    public partial class ConfirmExistingUsers : Migration
    {
        /// <inheritdoc />
        // Accounts created before email confirmation existed never got an email,
        // so treat them as confirmed instead of locking them out.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [AspNetUsers] SET [EmailConfirmed] = 1 WHERE [EmailConfirmed] = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible: we can't tell which accounts were unconfirmed before
        }
    }
}
