# PeopleManager API

A professional .NET 10 Web API for managing a list of people, built with clean architecture principles.

## Tech Stack

- .NET 10 Web API
- Entity Framework Core 10
- SQL Server / LocalDB
- QuestPDF (PDF Export)
- Swagger / OpenAPI

## Project Structure
PeopleManager/
├── Controllers/     # API endpoints
├── Services/        # Business logic (Interface + Implementation)
├── Data/            # DbContext + Migrations
├── Models/          # Database entities
├── DTOs/            # Data Transfer Objects
└── wwwroot/uploads/ # Uploaded images

## Getting Started

### Prerequisites
- .NET 10 SDK
- SQL Server / LocalDB

### Run the project

```bash
dotnet ef database update
dotnet run --launch-profile http
```

Open Swagger: `http://localhost:5240/swagger/index.html`

## API Endpoints

### Part A — Basic API

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/people | Get all people (with pagination) |
| GET | /api/people/{id} | Get person by ID |
| GET | /api/people/search?searchTerm= | Search by name |
| POST | /api/people | Create a person with optional image |
| GET | /api/people/export/pdf | Export people list to PDF |

### Part B — Enhancements

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/people?isActive=true | Filter by active status |
| GET | /api/people?page=1&pageSize=10 | Pagination |
| GET | /api/people?searchTerm=nab | Reverse keyboard search |
| PATCH | /api/people/{id}/status | Update person status |

## Features

### Part A
- Create a person with first name, last name, phone, email and optional image upload
- Get all people with pagination
- Search by name (partial match)
- Export active people to PDF

### Part B
- Split full name into FirstName + LastName with database migration
- Status field (Active/Inactive) with filtering
- Advanced search with partial name support
- Reverse keyboard search (typing in English finds Hebrew names, e.g. `nab` → finds `משה`)
- Pagination with max page size of 50

### Bonus Features
- Get person by ID
- Israeli phone number validation (05XXXXXXXX)
- Image type validation (.jpg/.jpeg/.png/.gif only)
- Image size validation (max 5MB)
- Unique email constraint in database

## Validations

| Field | Rule |
|-------|------|
| Email | Valid format, unique in system |
| Phone | Israeli mobile format (05XXXXXXXX) |
| Image | .jpg/.jpeg/.png/.gif only, max 5MB |
| FirstName | Required, max 50 characters |
| LastName | Required, max 50 characters |

## AI Usage (Part C)

This project was developed with AI assistance (Claude by Anthropic).

### Parts where AI was used
- Project structure and architecture planning
- Entity Framework setup and migrations
- Reverse keyboard mapping implementation
- PDF export with QuestPDF
- Code review and refactoring

### Example of AI-assisted improvement
The search logic was initially duplicated between `GetAllAsync` and `SearchByNameAsync`. After AI code review, the logic was extracted into a private `ApplySearchFilter` method — eliminating duplication and improving maintainability.

Additionally, the search was initially using `.ToLower().Contains()` which failed with Hebrew characters in SQL Server. AI suggested switching to `EF.Functions.Like()` which solved the issue correctly.

### Architectural decision
The Service layer was designed using an Interface (`IPersonService`) with Dependency Injection. This keeps the Controller clean — it only knows about the contract, not the implementation. This pattern makes the code testable, maintainable, and easy to extend.
