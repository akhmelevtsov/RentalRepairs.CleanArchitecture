using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalRepairs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AddressStreetNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AddressStreetName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AddressCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AddressPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SuperintendentFirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SuperintendentLastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SuperintendentEmailAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SuperintendentMobilePhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Units = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NoReplyEmailAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactInfoFirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactInfoLastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactInfoEmailAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContactInfoMobilePhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Specialization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactInfoFirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactInfoLastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactInfoEmailAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContactInfoMobilePhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PropertyCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestsCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tenants_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UrgencyLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsEmergency = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TenantEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TenantUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PropertyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PropertyPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SuperintendentFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SuperintendentEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedWorkerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AssignedWorkerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WorkOrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ClosureNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    WorkCompletedSuccessfully = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantRequests_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_Code",
                table: "Properties",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Property_CreatedAt",
                table: "Properties",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Property_CreatedBy",
                table: "Properties",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Property_IsDeleted",
                table: "Properties",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRequest_CreatedAt",
                table: "TenantRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRequest_CreatedBy",
                table: "TenantRequests",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRequest_IsDeleted",
                table: "TenantRequests",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRequests_AssignedWorkerEmail",
                table: "TenantRequests",
                column: "AssignedWorkerEmail");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRequests_Code",
                table: "TenantRequests",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantRequests_IsEmergency",
                table: "TenantRequests",
                column: "IsEmergency");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRequests_PropertyId",
                table: "TenantRequests",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRequests_ScheduledDate",
                table: "TenantRequests",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRequests_Status",
                table: "TenantRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRequests_TenantId",
                table: "TenantRequests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRequests_UrgencyLevel",
                table: "TenantRequests",
                column: "UrgencyLevel");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRequests_WorkOrderNumber",
                table: "TenantRequests",
                column: "WorkOrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Tenant_CreatedAt",
                table: "Tenants",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tenant_CreatedBy",
                table: "Tenants",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tenant_IsDeleted",
                table: "Tenants",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Property_Unit",
                table: "Tenants",
                columns: new[] { "PropertyId", "UnitNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_PropertyId",
                table: "Tenants",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Worker_CreatedAt",
                table: "Workers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Worker_CreatedBy",
                table: "Workers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Worker_IsDeleted",
                table: "Workers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_Email",
                table: "Workers",
                column: "ContactInfoEmailAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workers_IsActive",
                table: "Workers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_Specialization",
                table: "Workers",
                column: "Specialization");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantRequests");

            migrationBuilder.DropTable(
                name: "Workers");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "Properties");
        }
    }
}
