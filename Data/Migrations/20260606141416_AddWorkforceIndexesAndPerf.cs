using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GwsWorkforce.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkforceIndexesAndPerf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConversationMessages_ConversationId",
                table: "ConversationMessages");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerDefinitions_Key",
                table: "WorkerDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserKnowledgeItems_ApplicationUserId",
                table: "UserKnowledgeItems",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserKnowledgeItems_ApplicationUserId_Category",
                table: "UserKnowledgeItems",
                columns: new[] { "ApplicationUserId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ApplicationUserId",
                table: "Conversations",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ApplicationUserId_UpdatedAtUtc_CreatedAtUtc",
                table: "Conversations",
                columns: new[] { "ApplicationUserId", "UpdatedAtUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_ConversationId_CreatedAtUtc",
                table: "ConversationMessages",
                columns: new[] { "ConversationId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkerDefinitions_Key",
                table: "WorkerDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_UserKnowledgeItems_ApplicationUserId",
                table: "UserKnowledgeItems");

            migrationBuilder.DropIndex(
                name: "IX_UserKnowledgeItems_ApplicationUserId_Category",
                table: "UserKnowledgeItems");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_ApplicationUserId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_ApplicationUserId_UpdatedAtUtc_CreatedAtUtc",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_ConversationMessages_ConversationId_CreatedAtUtc",
                table: "ConversationMessages");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_ConversationId",
                table: "ConversationMessages",
                column: "ConversationId");
        }
    }
}
