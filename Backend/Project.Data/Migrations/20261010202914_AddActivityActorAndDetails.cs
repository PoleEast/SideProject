using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityActorAndDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActorMemberId",
                table: "ActivityLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ActorName",
                table: "ActivityLogs",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ActivityLogDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityLogId = table.Column<int>(type: "int", nullable: false),
                    GroupMemberId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLogDetails_ActivityLogs_ActivityLogId",
                        column: x => x.ActivityLogId,
                        principalTable: "ActivityLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityLogDetails_GroupMembers_GroupMemberId",
                        column: x => x.GroupMemberId,
                        principalTable: "GroupMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_ActorMemberId",
                table: "ActivityLogs",
                column: "ActorMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogDetails_ActivityLogId",
                table: "ActivityLogDetails",
                column: "ActivityLogId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogDetails_GroupMemberId",
                table: "ActivityLogDetails",
                column: "GroupMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityLogs_GroupMembers_ActorMemberId",
                table: "ActivityLogs",
                column: "ActorMemberId",
                principalTable: "GroupMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityLogs_GroupMembers_ActorMemberId",
                table: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "ActivityLogDetails");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_ActorMemberId",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "ActorMemberId",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "ActorName",
                table: "ActivityLogs");
        }
    }
}
