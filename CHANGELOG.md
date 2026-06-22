# Changelog

Tất cả thay đổi đáng chú ý của dự án DocumentStorageService.

Định dạng dựa trên [Keep a Changelog](https://keepachangelog.com/vi/1.1.0/).

---

## [0.2.0] — 2026-06-22

### Fixed
- **[Security]** Path traversal trong `LocalStorageProvider` — validate đường dẫn giải quyết nằm trong base directory
- **[Security]** Endpoint upload/download ẩn danh — yêu cầu xác thực `X-API-Key` trên tất cả endpoint
- **[Security]** Thông tin đăng nhập production hardcode trong `appsettings.json` — chuyển sang user secrets
- **[Security]** Thứ tự xóa file sai — soft-delete DB trước, xóa file vật lý sau khi commit
- **[Security]** Cache poisoning — negative cache rút xuống 30s, thêm `SizeLimit` cho `MemoryCache`
- **[Security]** Validate S3/MinIO options sai thời điểm — validate trước khi tạo client
- **[Security]** Exception middleware leak internal details — chỉ expose message cho domain exceptions

### Changed
- `GetFileByIdQuery` / `DeleteFileCommand` / `UpdateDescriptionCommand`: `UserId` thành nullable, cho phép admin truy cập file không cần user scope
- `FilesController`: thêm `[FromQuery] Guid? projectId` cho admin chỉ định project trên endpoint GetById/Delete/UpdateDescription
- `Project.Update`: throw `ArgumentException` khi `name` là chuỗi rỗng thay vì bỏ qua âm thầm
- `S3StorageProvider` / `MinioStorageProvider`: `ValidateOptions` chạy trước `CreateClient`
- `ExceptionHandlingMiddleware`: thêm `exposeMessage` flag, ẩn chi tiết exception không phải domain

### Added
- `AdminOnlyAttribute` — attribute `IAuthorizationFilter` tập trung hóa kiểm tra admin, thay thế 5 lần lặp `if(!IsAdmin)` trong `ProjectsController`
- `[Range(1, int.MaxValue)]` trên `page` và `[Range(1, 100)]` trên `pageSize` ở tất cả endpoint phân trang
- `.gitignore` entry cho `.claude/`
- User secrets initialization (`UserSecretsId: e11925e5-92b4-4136-a77c-ac118a35f85f`)
- `README.md`, `CHANGELOG.md`
- `Dockerfile`, `docker-compose.yml`, `.dockerignore`

---

## [0.1.0] — 2026-06-18

### Added
- Clean Architecture solution với 5 projects: Api, Application, Domain, Infrastructure, Shared
- CQRS pattern: Commands (InitUpload, CompleteUpload, DeleteFile, UpdateDescription) + Queries (GetFileById, SearchFiles, GetFilesByUser)
- Project management: Create, Update, SetActive, RegenerateApiKey, GetAll, GetById
- Storage providers: `S3StorageProvider`, `MinioStorageProvider`, `LocalStorageProvider` (chia sẻ `S3CompatibleStorageProvider` base)
- EF Core với SQL Server + PostgreSQL support, migration `InitialCreate` + `AddProjects`
- Soft delete qua `IsDeleted` / `DeletedAt` + `HasQueryFilter` tự động
- Upload 2 pha: init-upload (presigned URL) → complete (lưu metadata)
- Phân quyền: Admin (AdminKey) + Project User (API key `pk_...`)
- Caching: `ProjectCache` (5 phút sliding expiration cho positive, 30s cho negative)
- `ExceptionHandlingMiddleware` — RFC 7807 Problem Details
- `ProjectResolutionMiddleware` — resolve API key → project
- 80 unit tests (Domain.Tests + Application.Tests)
- Software Design Document (`DocumentStorageService-SDD.md`)
- Deployment Guide (`DEPLOYMENT-GUIDE.md`)
