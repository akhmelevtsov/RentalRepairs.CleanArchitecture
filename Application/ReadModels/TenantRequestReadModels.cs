// This file has been refactored and split into individual read model files.
// See Application/ReadModels/ folder for the new implementation.

// Individual Read Model Files:
// - Application/ReadModels/TenantRequestListItemReadModel.cs
// - Application/ReadModels/TenantRequestDetailsReadModel.cs
// - Application/ReadModels/TenantRequestChangeReadModel.cs

// Re-export types for backward compatibility during transition
global using TenantRequestListItemReadModel = RentalRepairs.Application.ReadModels.TenantRequestListItemReadModel;
global using TenantRequestDetailsReadModel = RentalRepairs.Application.ReadModels.TenantRequestDetailsReadModel;
