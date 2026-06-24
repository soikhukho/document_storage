# DocumentStorageService

Dịch vụ lưu trữ tài liệu đa tenant, xây dựng trên ASP.NET Core 9 Clean Architecture với CQRS. Hỗ trợ nhiều storage provider: **AWS S3**, **MinIO**, và **Local File System**.

## Tính năng

- **Upload 2 pha**: Client lấy presigned URL → upload trực tiếp lên storage → API lưu metadata
- **Multi-tenant**: Mỗi project có API key riêng, cô lập dữ liệu theo project
- **Soft delete**: File bị xóa khỏi storage nhưng metadata vẫn giữ trong DB
- **Đa storage provider**: Chuyển đổi giữa S3 / MinIO / Local chỉ bằng cấu hình
- **Phân quyền**: Admin (JWT, toàn quyền) và Project User (API key `pk_...`, scoped theo project)
- **Tìm kiếm & phân trang**: Keyword, sort, paging trên metadata
- **Response chuẩn**: Mọi API response bọc trong `ApiResponse<T>` envelope (`success`, `data`, `message`, `errors`, `timestamp`)

## Kiến trúc

```
┌─────────────────────────────────────────────┐
│              DocumentStorage.Api             │  Controllers, Middleware
│                  (webapi)                    │
├──────────────┬───────────────────────────────┤
│  Application │         Infrastructure        │
│   Commands   │     Persistence (EF Core)     │
│   Queries    │     Storage (S3/MinIO/Local)  │
│    DTOs      │     Auth, Caching             │
├──────────────┼───────────────────────────────┤
│    Domain    │           Shared              │
│  Entities    │        (no deps)              │
│  Exceptions  │                               │
└──────────────┴───────────────────────────────┘
```

**Hướng phụ thuộc (Clean Architecture):**
```
Shared ← Domain ← Application ← Infrastructure
                             ← Api → Infrastructure
```

## Cấu trúc dự án

```
src/
├── DocumentStorage.Api/              # Web API (controllers, middleware, attributes)
├── DocumentStorage.Application/      # CQRS handlers, DTOs, interfaces
├── DocumentStorage.Domain/           # Entities, enums, domain exceptions
├── DocumentStorage.Infrastructure/   # EF Core, storage providers, auth, caching
└── DocumentStorage.Shared/           # ApiResponse, ErrorResponse, Result, AppError, ErrorType

tests/
├── DocumentStorage.Domain.Tests/     # 31 tests
└── DocumentStorage.Application.Tests/# 54 tests
```

## Bắt đầu nhanh

### Yêu cầu

- .NET 9 SDK
- SQL Server 2019+ hoặc PostgreSQL 14+
- (Tùy chọn) MinIO hoặc AWS S3

### Cài đặt

```bash
# Clone
git clone <repo-url>
cd DocumentStorage

# Khôi phục packages
dotnet restore DocumentStorage.slnx

# Cấu hình secrets (Development)
cd src/DocumentStorage.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=localhost,1433;Database=Document_Storage;User Id=sa;Password=YourPass;TrustServerCertificate=True"
dotnet user-secrets set "Auth:AdminKey" "your-secure-admin-key"
cd ../..

# Tạo database & chạy migration
dotnet ef database update \
  --project src/DocumentStorage.Infrastructure \
  --startup-project src/DocumentStorage.Api

# Chạy
dotnet run --project src/DocumentStorage.Api
```

API chạy tại `http://localhost:5000`. Mở OpenAPI spec tại `/openapi/v1.json` (chế độ Development).

### Chạy với Docker

```bash
docker-compose up
```

Xem chi tiết tại [DEPLOYMENT-GUIDE.md](DEPLOYMENT-GUIDE.md).

## Công nghệ

| Thành phần | Phiên bản |
|---|---|
| .NET | 9.0 |
| EF Core | 9.0 |
| AWSSDK.S3 | 3.7.* |
| Database | SQL Server hoặc PostgreSQL |
| Testing | xUnit + NSubstitute |

## Tài liệu

- [SDD (Software Design Document)](DocumentStorageService-SDD.md) — Thiết kế chi tiết
- [Hướng dẫn triển khai](DEPLOYMENT-GUIDE.md) — Cấu hình, API reference, ví dụ client
- [Lịch sử thay đổi](CHANGELOG.md) — Các phiên bản và bản vá

## Bảo mật

- Xác thực: Admin = JWT (`/api/auth/login`), Project = API key (`X-API-Key`)
- Response envelope: `ApiResponse<T>` cho mọi endpoint, Result pattern trong handlers
- Path traversal protection trên Local storage
- Presigned URL có thời hạn cho upload/download
- Soft delete + EF Core query filter tự động
- ExceptionHandlingMiddleware fallback → ApiResponse error format

Xem chi tiết tại [DEPLOYMENT-GUIDE.md §7](DEPLOYMENT-GUIDE.md#7-bảo-mật).
