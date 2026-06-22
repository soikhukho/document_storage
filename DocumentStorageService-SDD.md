# Document Storage Service - Software Design Document

## 1. Overview

### Purpose

Document Storage Service cung cấp khả năng:

* Upload file
* Download file
* Delete file
* Quản lý metadata
* Hỗ trợ nhiều storage provider

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
* PostgreSQL

Architecture:

* Clean Architecture
* CQRS
* Provider Pattern

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

Application generates:

object key

Example:

user/{userId}/2026/06/{guid}.pdf

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

# 8. Delete Flow

DELETE /api/files/{id}

Process:

1. Find metadata

2. Call StorageProvider.Delete()

3. Soft delete database record

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

Table:

FileDocuments

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

Indexes:

IX_FileDocuments_StorageKey

IX_FileDocuments_UploadedBy

---

# 13. Commands

InitUploadCommand

CompleteUploadCommand

DeleteFileCommand

UpdateDescriptionCommand

---

# 14. Queries

GetFileByIdQuery

SearchFilesQuery

GetFilesByUserQuery

---

# 15. API Contract

POST /api/files/init-upload

POST /api/files/complete

GET /api/files/{id}

DELETE /api/files/{id}

GET /api/files

GET /api/files/user/{userId}

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

FileNotFoundException

StorageException

InvalidFileTypeException

UploadExpiredException

PermissionDeniedException

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
