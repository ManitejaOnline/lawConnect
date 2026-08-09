# 04 Database Design

## Overview

LawConnect uses PostgreSQL with EF Core Code First. All persisted entities inherit `BaseEntity` and share:

| Concern | Implementation |
|---------|----------------|
| Identity | `Guid Id` |
| Auditing | `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` |
| Soft delete | `IsDeleted`, `DeletedAt`, `DeletedBy` + global query filter |
| Concurrency | `RowVersion` (`uint`, concurrency token, incremented on update) |
| Domain events | Ignored by EF; dispatched after `SaveChanges` |

## Migration

- **Name:** `InitialCreate`
- **Location:** `src/LawConnect.Infrastructure/Persistence/Migrations/`
- **Apply:** `dotnet ef database update --project src/LawConnect.Infrastructure --startup-project src/LawConnect.Api`

## Seed data

### Roles

| Id | Name |
|----|------|
| `11111111-1111-1111-1111-111111111111` | Customer |
| `22222222-2222-2222-2222-222222222222` | Lawyer |
| `33333333-3333-3333-3333-333333333333` | Admin |

### Specializations (15)

Criminal Law, Civil Law, Family Law, Corporate Law, Property Law, Intellectual Property, Tax Law, Labor & Employment, Constitutional Law, Consumer Protection, Cyber Law, Immigration Law, Environmental Law, Banking & Finance, Medical Negligence.

Deterministic IDs live in `LawConnect.Shared.Constants.SeedIds`.

## ER diagram

```mermaid
erDiagram
    User ||--o| Customer : has
    User ||--o| Lawyer : has
    User ||--o{ UserRole : has
    Role ||--o{ UserRole : assigned
    User ||--o{ RefreshToken : owns
    User ||--o{ Device : owns
    User ||--o{ Notification : receives
    User ||--o{ Address : has
    User ||--o{ AuditLog : produces
    User ||--o{ OtpVerification : may_own

    Country ||--o{ State : contains
    State ||--o{ City : contains
    City ||--o{ Address : locates

    Lawyer ||--o{ LawyerSpecialization : has
    Specialization ||--o{ LawyerSpecialization : categorizes
    Lawyer ||--o{ LawyerAvailability : schedules
    Lawyer ||--o{ LawyerEducation : lists
    Lawyer ||--o{ LawyerExperience : lists
    Lawyer ||--o{ LawyerLanguage : speaks
    Lawyer ||--o{ LawyerDocument : uploads

    User {
        uuid Id PK
        string PhoneNumber UK
        string Email UK
        string FirstName
        string LastName
        bool IsActive
    }

    Role {
        uuid Id PK
        string Name UK
    }

    Customer {
        uuid Id PK
        uuid UserId FK_UK
    }

    Lawyer {
        uuid Id PK
        uuid UserId FK_UK
        string BarCouncilNumber UK
        decimal ConsultationFee
        string VerificationStatus
    }

    Specialization {
        uuid Id PK
        string Name UK
        string Slug UK
    }

    Country {
        uuid Id PK
        string Iso2Code UK
    }

    State {
        uuid Id PK
        uuid CountryId FK
        string Name
    }

    City {
        uuid Id PK
        uuid StateId FK
        string Name
    }
```

## Key relationships

| From | To | Cardinality | Delete behavior |
|------|----|-------------|-----------------|
| User → Customer | 1:1 | Cascade |
| User → Lawyer | 1:1 | Cascade |
| User ↔ Role via UserRole | M:N | User cascade / Role restrict |
| User → RefreshToken, Device, Notification, Address | 1:N | Cascade |
| User → AuditLog, OtpVerification | 1:N | SetNull |
| Country → State → City | 1:N | Restrict |
| City → Address | 1:N | SetNull |
| Lawyer → specialization/availability/education/experience/language/document | 1:N | Cascade |
| Specialization → LawyerSpecialization | 1:N | Restrict |

## Notable indexes / unique constraints

- `Users.PhoneNumber` unique
- `Users.Email` unique (filtered, non-null, not deleted)
- `Roles.Name` unique
- `UserRoles (UserId, RoleId)` unique (filtered)
- `Lawyers.UserId` unique; `BarCouncilNumber` unique (filtered)
- `RefreshTokens.Token` unique
- `Devices (UserId, DeviceIdentifier)` unique (filtered)
- `Countries.Iso2Code` / `Iso3Code` / `Name` unique
- `States (CountryId, Name)` unique (filtered)
- `Cities (StateId, Name)` unique (filtered)
- `Specializations.Name` unique; `Slug` unique (filtered)
- `LawyerSpecializations (LawyerId, SpecializationId)` unique (filtered)
- `LawyerAvailabilities (LawyerId, DayOfWeek, StartTime, EndTime)` unique (filtered)
- `LawyerLanguages (LawyerId, LanguageCode)` unique (filtered)
- `LawyerDocuments.StorageKey` unique

## Soft delete

Global query filter `!IsDeleted` applies to all `ISoftDeletable` entities. Physical deletes are converted to soft deletes by `AuditableEntityInterceptor`.
