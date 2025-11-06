# View Fix Required for AssignWorker.cshtml

The Razor view needs to be updated to reference individual properties instead of `AssignmentRequest`.

## Find and Replace ALL Occurrences:

### 1. Hidden Field
**Find**: `asp-for="AssignmentRequest.RequestId"`  
**Replace**: `asp-for="RequestId"`

### 2. Worker Email Label
**Find**: `asp-for="AssignmentRequest.WorkerEmail"`  
**Replace**: `asp-for="WorkerEmail"`

### 3. Worker Email Select (2 occurrences)
**Find**: `asp-for="AssignmentRequest.WorkerEmail"`  
**Replace**: `asp-for="WorkerEmail"`

### 4. Worker Email Validation (2 occurrences)
**Find**: `asp-validation-for="AssignmentRequest.WorkerEmail"`
**Replace**: `asp-validation-for="WorkerEmail"`

### 5. Scheduled Date Label
**Find**: `asp-for="AssignmentRequest.ScheduledDate"`  
**Replace**: `asp-for="ScheduledDate"`

### 6. Scheduled Date Input
**Find**: `asp-for="AssignmentRequest.ScheduledDate"`  
**Replace**: `asp-for="ScheduledDate"`

### 7. Scheduled Date Validation
**Find**: `asp-validation-for="AssignmentRequest.ScheduledDate"`  
**Replace**: `asp-validation-for="ScheduledDate"`

### 8. Work Order Number Label
**Find**: `asp-for="AssignmentRequest.WorkOrderNumber"`  
**Replace**: `asp-for="WorkOrderNumber"`

### 9. Work Order Number Input
**Find**: `asp-for="AssignmentRequest.WorkOrderNumber"`  
**Replace**: `asp-for="WorkOrderNumber"`

### 10. Work Order Number Validation
**Find**: `asp-validation-for="AssignmentRequest.WorkOrderNumber"`  
**Replace**: `asp-validation-for="WorkOrderNumber"`

### 11. Notes Label (REMOVE - Notes property removed)
**Find**: `asp-for="AssignmentRequest.Notes"`  
**Replace**: Remove this section entirely (lines ~201-207)

### 12. Notes Textarea (REMOVE - Notes property removed)
**Find**: `<textarea asp-for="AssignmentRequest.Notes"`  
**Action**: Remove this entire div section

### 13. Notes Validation (REMOVE - Notes property removed)
**Find**: `asp-validation-for="AssignmentRequest.Notes"`  
**Action**: Already removed with section above

### 14. JavaScript - Work Order Generation
**Find**: `input[name="AssignmentRequest.WorkOrderNumber"]`  
**Replace**: `input[name="WorkOrderNumber"]`

## Summary

**Total Replacements**: 12 asp-for attributes  
**Total Removals**: 1 section (Notes field - 3 lines total)  
**JavaScript Updates**: 1 querySelector

All references to `AssignmentRequest.*` should be changed to just the property name (e.g., `RequestId`, `WorkerEmail`, etc.)

**Notes field removed**: The `ScheduleServiceWorkCommand` doesn't have a Notes property, so remove that entire form field section.
