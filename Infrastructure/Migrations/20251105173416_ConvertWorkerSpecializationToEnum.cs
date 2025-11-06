using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalRepairs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertWorkerSpecializationToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add a temporary column for the new enum values
            migrationBuilder.AddColumn<int>(
                name: "SpecializationTemp",
                table: "Workers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Step 2: Migrate existing data from string to enum integer values
            migrationBuilder.Sql(@"
           UPDATE Workers
    SET SpecializationTemp = 
                    CASE 
                 WHEN Specialization IS NULL OR Specialization = '' THEN 0
          WHEN Specialization LIKE '%Plumb%' THEN 1
         WHEN Specialization LIKE '%Electric%' THEN 2
      WHEN Specialization LIKE '%HVAC%' OR Specialization LIKE '%Heat%' OR Specialization LIKE '%Cool%' THEN 3
         WHEN Specialization LIKE '%Carpent%' OR Specialization LIKE '%Wood%' THEN 4
 WHEN Specialization LIKE '%Paint%' THEN 5
               WHEN Specialization LIKE '%Lock%' THEN 6
  WHEN Specialization LIKE '%Appliance%' THEN 7
           WHEN Specialization LIKE '%General%' OR Specialization LIKE '%Maintenance%' THEN 0
     ELSE 0  -- Default to General Maintenance
         END
  ");

            // Step 3: Drop the old string column
            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "Workers");

            // Step 4: Rename the temporary column to Specialization
            migrationBuilder.RenameColumn(
                name: "SpecializationTemp",
                table: "Workers",
                newName: "Specialization");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add temporary string column
            migrationBuilder.AddColumn<string>(
                name: "SpecializationTemp",
                table: "Workers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Step 2: Convert enum values back to strings
            migrationBuilder.Sql(@"
         UPDATE Workers
  SET SpecializationTemp = 
    CASE Specialization
            WHEN 0 THEN 'General Maintenance'
         WHEN 1 THEN 'Plumbing'
                     WHEN 2 THEN 'Electrical'
      WHEN 3 THEN 'HVAC'
   WHEN 4 THEN 'Carpentry'
             WHEN 5 THEN 'Painting'
          WHEN 6 THEN 'Locksmith'
         WHEN 7 THEN 'Appliance Repair'
 ELSE 'General Maintenance'
   END
   ");

            // Step 3: Drop the int column
            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "Workers");

            // Step 4: Rename temp column back
            migrationBuilder.RenameColumn(
                name: "SpecializationTemp",
                table: "Workers",
                newName: "Specialization");
        }
    }
}
