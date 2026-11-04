# WO CM Review Automation

An enterprise-grade internal web application that automates the monthly Change Management (CM) review process from Excel files.

## Features
- **Excel Upload & Parsing**: Import monthly Work Order Excel files.
- **Dynamic Business Rules**: Filter work orders based on configurable approvers, vendors, domains, etc.
- **Application Normalization**: Automatically map various application aliases to a standardized name.
- **Monthly Summary Reports**: Generate accurate report summaries with drill-down capabilities.
- **Exporting**: Export to Excel and PDF formats.

## Architecture
- Clean Architecture implementation
- ASP.NET Core Blazor (Interactive Server)
- Entity Framework Core with SQL Server
- MediatR (CQRS)
- MudBlazor UI
- Central Package Management

## Setup
1. Configure SQL Server connection string in `appsettings.json`.
2. Run database migrations to initialize the schema and seed data.
3. Build and run the `WO_CM_Review_Automation.Web` project.
