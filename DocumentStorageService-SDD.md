# Document Storage Service - Software Design Document

## 1. Overview

### Purpose

Document Storage Service cung cấp khả năng:

* Upload file
* Download file
* Delete file (soft delete + trash bin)
* Restore file from trash
* Purge file vĩnh viễn (permanent delete)
* Quản lý metadata
* Hỗ trợ nhiều storage provider
* Multi-tenant (project-scoped isolation)

Business layer không phụ thuộc storage implementation.

### Supported Providers

* AWS S3
* MinIO
* Local File System

### Technology Stack

Backend:

* ASP.NET Core 9
* C#
* Entity Framework Core
* SQL Server (default) hoặc PostgreSQL

Architecture:

* Clean Architecture
* CQRS
* Provider Pattern
* Result Pattern (no-throw error handling)

---

# 2. High Level Architecture

```text
Client
    ↓
API Layer
    ↓
Application Layer
    ↓
Domain Layer
    ↓
Infrastructure Layer
    ↓
Storage Provider

---------------------------------

IStorageProvider

    ├── S3StorageProvider
    ├── MinioStorageProvider
    └── LocalStorageProvider
```

---

# 3. Folder Structure

```text
src

Api

Application
    Commands
    Queries
    DTOs
    Interfaces

Domain
    Entities
    Enums

Infrastructure
    Persistence
    Storage

        S3

        MinIO

        Local

Shared
    Results
        ErrorType
        AppError
        Result
    Contracts
        ApiResponse
        ErrorResponse
```

---

# 4. Entity Design

## FileDocument

Represents one file metadata record.

Properties:

Id : Guid

Name : string

Extension : string

ContentType : string

Size : long

StorageKey : string

Provider : StorageProviderType

CreatedAt : DateTime

UploadedBy : Guid

Description : string

IsDeleted : bool

DeletedAt : DateTime?

## Project

Represents a tenant (project) with its own storage namespace.

Properties:

Id : Guid

Name : string

FolderName : string (ASCII only, unique, immutable via API)

ApiKey : string

Description : string?

IsActive : bool

CreatedAt : DateTime

**FolderName validation rules:**

- Required, must be non-empty
- Max 100 characters
- Must match regex `^[a-zA-Z0-9_-]+$` (ASCII letters, digits, hyphen, underscore only — no spaces, no Vietnamese diacritics, no special characters)
- Must be unique across all projects (enforced by `IX_Projects_FolderName` unique index)
- Set at creation time only; **cannot be updated via API** (admin edits directly in database if needed)
- Used as the top-level folder name in the storage key, e.g. `MyProject/report.pdf`

## AdminUser

Represents an administrator who can log in via JWT.

Properties:

Id : Guid

Username : string

PasswordHash : string

IsActive : bool

CreatedAt : DateTime

UpdatedAt : DateTime?

---

# 5. Enum

StorageProviderType

Values:

S3 = 1

MinIO = 2

Local = 3

---

# 6. Upload Flow

### Step 1

Client calls:

POST /api/files/init-upload

Request:

name

contentType

size

---

### Step 2

Application generates the storage key using the project's FolderName:

Format: `{FolderName}/{fileName}`

Example: `MyProject/report.pdf`

The FolderName is sanitized (replaces `/` and `\` with `_`) to ensure it forms a valid path segment.

**Duplicate check:** Before issuing a presigned URL, the system checks if a non-deleted file with the same name already exists in the project. If it does, returns `409 Conflict` (`FILE_NAME_EXISTS`) — no presigned URL is issued.

---

### Step 3

StorageProvider generates UploadInstruction.

---

### Step 4

Response:

fileId

uploadUrl

expiredAt

headers

---

### Step 5

Client uploads binary directly.

S3:

Client → S3

Local:

Client → API → Local Disk

---

### Step 6

Client calls:

POST /api/files/complete

System stores metadata into database.

---

# 7. Download Flow

Client

↓

GET /api/files/{id}

↓

Application

↓

Repository

↓

IStorageProvider

↓

Generate Download URL

↓

Return FileDto

---

# 8. Delete Flow (Soft Delete)

DELETE /api/files/{id}

Process:

1. Find metadata by id (scoped by project + user)
2. If not found, return `404 NOT_FOUND`
3. Call `document.SoftDelete()` — sets `IsDeleted = true`, `DeletedAt = now`
4. Save changes to database
5. **S3 object is intentionally kept** so the file can be restored later

The EF Core global query filter (`HasQueryFilter`) automatically excludes soft-deleted records from normal queries. The file disappears from search/list but remains in storage.

---

# 8.1. Trash Bin

Mỗi project có một trash bin riêng, chứa các file đã soft-delete.

**Listing trash:**

GET /api/files/trash

- Tenant scope: lists trash for the project identified by the API key
- Admin scope: lists trash for a specific project (`?projectId=...`)
- Supports keyword search, pagination, and sorting (same as search)
- Uses `IgnoreQueryFilters()` to bypass the soft-delete auto-filter

---

# 8.2. Restore Flow

POST /api/files/{id}/restore

Process:

1. Find soft-deleted file by id within project (uses `IgnoreQueryFilters`)
2. If not found in trash, return `404 NOT_FOUND`
3. Check if an active file with the same name already exists in the project (`ExistsByNameAsync`)
4. If name conflict, return `409 CONFLICT` (`FILE_NAME_EXISTS`)
5. Call `document.Restore()` — clears `IsDeleted` and `DeletedAt`
6. Save changes to database
7. File becomes visible again in normal search/list

---

# 8.3. Purge Flow (Permanent Delete)

DELETE /api/files/{id}/purge

Permanently removes a file from both storage and database. Only works on files already in trash.

Process:

1. Find soft-deleted file by id within project (uses `IgnoreQueryFilters`)
2. If not found in trash, return `404 NOT_FOUND`
3. **Reference count check (Option B safety):** Count how many OTHER rows (active or soft-deleted) reference the same `StorageKey`
   - `CountOtherReferencesByStorageKeyAsync(storageKey, excludeId)`
   - Uses `IgnoreQueryFilters()` + `AsNoTracking()` so it sees ALL rows including soft-deleted ones
4. If `refCount == 0` → delete the S3/storage object (`StorageProvider.DeleteAsync`)
5. If `refCount > 0` → **skip storage delete** (prevents data loss when another record shares the same storage key, e.g. a re-upload with the same name)
6. Hard-remove the DB row (`HardRemoveAsync` — uses `IgnoreQueryFilters` + `Remove`)
7. Save changes to database

**Why the reference check?** Because the storage key format is `{FolderName}/{fileName}`, two files with the same name in the same project share the same storage key. If file A is in trash and file B (same name) was uploaded after, purging A must not delete the S3 object that B still references.

---

# 9. Storage Abstraction

Interface:

IStorageProvider

Methods:

InitUploadAsync()

CompleteUploadAsync()

GetDownloadUrlAsync()

DeleteAsync()

ExistsAsync()

GetMetadataAsync()

---

# 10. UploadInstruction

Properties:

UploadUrl

Headers

ExpiredAt

---

# 11. FileDto

Properties:

Id

Name

Extension

ContentType

Size

DownloadUrl

CreatedAt

Description

---

# 12. Database Schema

## FileDocuments

Columns:

Id UUID PK

Name VARCHAR(255)

Extension VARCHAR(20)

ContentType VARCHAR(255)

Size BIGINT

StorageKey VARCHAR(500)

Provider INT

Description TEXT

UploadedBy UUID

CreatedAt TIMESTAMP

DeletedAt TIMESTAMP NULL

IsDeleted BOOLEAN

ProjectId UUID FK → Projects

Indexes:

IX_FileDocuments_StorageKey

IX_FileDocuments_UploadedBy

## Projects

Columns:

Id UUID PK

Name VARCHAR(200)

FolderName VARCHAR(100) NOT NULL

ApiKey VARCHAR(128)

Description TEXT

IsActive BOOLEAN

CreatedAt TIMESTAMP

Indexes:

IX_Projects_FolderName (UNIQUE)

IX_Projects_ApiKey (UNIQUE)

## AdminUsers

Columns:

Id UUID PK

Username VARCHAR(100) NOT NULL

PasswordHash TEXT NOT NULL

IsActive BOOLEAN

CreatedAt TIMESTAMP

UpdatedAt TIMESTAMP NULL

---

# 13. Commands

InitUploadCommand

CompleteUploadCommand

DeleteFileCommand (soft delete — keeps S3 object)

RestoreFileCommand (restore from trash — checks name conflict)

PurgeFileCommand (permanent delete — checks storage key reference count)

UpdateDescriptionCommand

---

# 14. Queries

GetFileByIdQuery

SearchFilesQuery

GetFilesByUserQuery

GetTrashQuery (lists soft-deleted files in a project, supports keyword/pagination/sorting)

---

# 15. API Contract

## Files

POST /api/files/init-upload

POST /api/files/complete

GET /api/files/{id}

DELETE /api/files/{id} (soft delete — moves to trash)

GET /api/files (search with keyword, pagination, sorting)

GET /api/files/user/{userId}

GET /api/files/trash (list trash bin; admin: ?projectId=...)

POST /api/files/{id}/restore (restore from trash)

DELETE /api/files/{id}/purge (permanent delete from trash)

## Auth

POST /api/auth/login (admin JWT login)

## Projects (admin only)

POST /api/projects

GET /api/projects

GET /api/projects/{id}

PUT /api/projects/{id}

PATCH /api/projects/{id}/active

POST /api/projects/{id}/regenerate-key

---

# 15.5. Response Format (ApiResponse Envelope)

Tất cả API response được bọc trong envelope `ApiResponse<T>`:

```json
{
  "success": true,
  "data": { ... },
  "message": "Request processed successfully.",
  "errors": [],
  "timestamp": "2026-06-24T12:00:00+00:00"
}
```

Thành phần:

| Field | Type | Mô tả |
|---|---|---|
| `success` | boolean | true nếu thành công, false nếu lỗi |
| `data` | T? | Dữ liệu trả về (null khi lỗi) |
| `message` | string | Thông báo mô tả kết quả |
| `errors` | ErrorResponse[] | Danh sách lỗi (rỗng khi thành công) |
| `timestamp` | DateTimeOffset | Thời điểm tạo response |

Error response:

```json
{
  "success": false,
  "data": null,
  "message": "File with id '...' was not found.",
  "errors": [
    {
      "code": "FILE_NOT_FOUND",
      "message": "File with id '...' was not found.",
      "detail": null,
      "target": null
    }
  ],
  "timestamp": "2026-06-24T12:00:00+00:00"
}
```

Result Pattern (Application layer):

- Handlers return `Result<T>` / `Result` thay vì throw exception
- `Result.IsSuccess` / `Result.IsFailure` / `Result.Value` / `Result.Errors`
- `AppError` với `ErrorType` enum xác định HTTP status code

ErrorType → HTTP Status:

| ErrorType | HTTP | Khi nào |
|---|---|---|
| Validation | 422 | Sai kiểu file, vượt size |
| NotFound | 404 | File/project không tồn tại |
| Conflict | 409 | Xung đột dữ liệu |
| Unauthorized | 401 | Sai credentials |
| Forbidden | 403 | Không có quyền |
| Failure | 400 | Lỗi business logic khác |

---

# 16. Search

Supports:

keyword

page

pageSize

sortBy

sortDirection

Response:

PagedResult<FileDto>

---

# 17. Validation

Maximum size:

100 MB

Allowed extensions:

pdf

docx

xlsx

png

jpg

jpeg

ContentType must be validated.

---

# 18. Configuration

StorageProviderType

S3

MinIO

Local

MaxUploadSize

UploadExpirationMinutes

DownloadExpirationMinutes

---

# 19. Logging

Log:

Init upload

Upload completed

Download request

Delete request

Provider exception

---

# 20. Exception Types

Domain exceptions vẫn tồn tại nhưng được catch trong handler và convert sang `AppError`:

| Domain Exception | Error Code | ErrorType | HTTP |
|---|---|---|---|
| FileNotFoundException | FILE_NOT_FOUND | NotFound | 404 |
| ProjectNotFoundException | PROJECT_NOT_FOUND | NotFound | 404 |
| InvalidFileTypeException | INVALID_FILE_TYPE | Validation | 422 |
| UploadExpiredException | UPLOAD_EXPIRED | Failure | 400 |
| PermissionDeniedException | PERMISSION_DENIED | Forbidden | 403 |
| InvalidCredentialsException | INVALID_CREDENTIALS | Unauthorized | 401 |
| StorageException | STORAGE_ERROR | Failure | 502 |

ExceptionHandlingMiddleware (fallback) catch các exception chưa được handler xử lý, trả về ApiResponse error format.

---

# 21. Security

Authorization required.

Users only access their own files.

Upload URL expiration:

5 minutes.

Download URL expiration:

1 minute.

No storage credentials exposed to clients.

---

# 22. Non Functional Requirements

Support:

1000000 files

Horizontal scaling

Provider replacement without changing business layer

Unit test coverage > 80%

All providers must implement IStorageProvider
