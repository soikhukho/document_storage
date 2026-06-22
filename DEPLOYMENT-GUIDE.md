# DocumentStorageService — Hướng Dẫn Triển Khai

## Mục Lục

1. [Yêu cầu hệ thống](#1-yêu-cầu-hệ-thống)
2. [Cấu hình](#2-cấu-hình)
3. [Triển khai](#3-triển-khai)
4. [Quản lý Project & API Key](#4-quản-lý-project--api-key)
5. [API Reference](#5-api-reference)
6. [Ví dụ tích hợp (Client)](#6-ví-dụ-tích-hợp-client)
7. [Bảo mật](#7-bảo-mật)
8. [Xử lý sự cố](#8-xử-lý-sự-cố)

---

## 1. Yêu cầu hệ thống

| Thành phần | Yêu cầu |
|---|---|
| .NET SDK | 9.0+ |
| Database | SQL Server 2019+ hoặc PostgreSQL 14+ |
| Storage | Local disk / AWS S3 / MinIO |
| OS | Windows / Linux / macOS |

---

## 2. Cấu hình

Cấu hình nằm trong `appsettings.json`. **Không lưu credentials (connection string, admin key) trong `appsettings.json`** — dùng một trong các cách sau:

| Môi trường | Cách cấu hình |
|---|---|
| **Development** | User Secrets (`dotnet user-secrets`) |
| **Production** | Environment variables hoặc Docker env |

```bash
# Development — User Secrets
cd src/DocumentStorage.Api
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=...;Password=..."
dotnet user-secrets set "Auth:AdminKey" "your-secure-admin-key"

# Production — Environment variables
export ConnectionStrings__SqlServer="Server=...;Password=..."
export Auth__AdminKey="your-secure-admin-key"
```

> **Lưu ý:** .NET dùng `__` (double underscore) cho hierarchy trong env vars, tương đương `:` trong JSON.

### 2.1. Database

```json
{
  "Database": {
    "Provider": "SqlServer"          // hoặc "PostgreSQL"
  },
  "ConnectionStrings": {
    "SqlServer": "",                 // ← set via user secrets / env var
    "PostgreSQL": "Host=localhost;Database=document_storage;Username=postgres;Password=postgres"
  }
}
```

Đổi `Provider` sang `"PostgreSQL"` để dùng PostgreSQL. Connection string tương ứng sẽ được sử dụng.

### 2.2. Auth (Admin Key)

```json
{
  "Auth": {
    "AdminKey": ""                   // ← set via user secrets / env var
  }
}
```

**Bắt buộc cấu hình** `AdminKey` trước khi chạy. Đây là key quản trị dùng để:
- Tạo/sửa/xóa project
- Xem file tất cả projects
- Deactivate/regenerate API key

### 2.3. Storage Provider

#### Option A — Local (mặc định)

```json
{
  "Storage": {
    "Provider": "Local",
    "MaxUploadSizeBytes": 104857600,      // 100 MB
    "UploadExpirationMinutes": 5,
    "DownloadExpirationMinutes": 1,
    "AllowedExtensions": ["pdf", "docx", "xlsx", "png", "jpg", "jpeg"]
  },
  "Local": {
    "BaseDirectory": "uploads",            // thư mục lưu file
    "PublicBaseUrl": "https://your-domain.com"  // URL công khai để client upload/download
  }
}
```

Khi dùng Local, client upload/download qua API trực tiếp (không presigned URL).

#### Option B — AWS S3

```json
{
  "Storage": {
    "Provider": "S3",
    "MaxUploadSizeBytes": 104857600,
    "UploadExpirationMinutes": 5,
    "DownloadExpirationMinutes": 1,
    "AllowedExtensions": ["pdf", "docx", "xlsx", "png", "jpg", "jpeg"]
  },
  "S3": {
    "AccessKey": "AKIAIOSFODNN7EXAMPLE",
    "SecretKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    "BucketName": "my-document-bucket",
    "Region": "ap-southeast-1",
    "ServiceUrl": ""
  }
}
```

- `AccessKey`/`SecretKey` để trống nếu chạy trên EC2 có IAM Role.
- `ServiceUrl` để trống cho AWS S3 thật. Điền URL nếu dùng LocalStack.

#### Option C — MinIO

```json
{
  "Storage": {
    "Provider": "MinIO"
  },
  "MinIO": {
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "BucketName": "documents",
    "Endpoint": "http://minio-server:9000",
    "Region": "us-east-1"
  }
}
```

---

## 3. Triển khai

### 3.1. Build & Publish

```bash
dotnet publish src/DocumentStorage.Api/DocumentStorage.Api.csproj \
  -c Release \
  -o ./publish
```

### 3.2. Tạo database & chạy migration

```bash
# Tạo database trước (SQL Server)
CREATE DATABASE Document_Storage;

# Chạy migration
dotnet ef database update \
  --project src/DocumentStorage.Infrastructure \
  --startup-project src/DocumentStorage.Api
```

Database sẽ có 2 bảng:
- **Projects** — thông tin project + API key
- **FileDocuments** — metadata file (scoped theo ProjectId)

### 3.3. Chạy ứng dụng

```bash
cd publish
dotnet DocumentStorage.Api.dll
```

Mặc định chạy ở `http://localhost:5000`.

### 3.4. Triển khai với Docker

Dự án có sẵn `Dockerfile` (multi-stage build) và `docker-compose.yml`:

```bash
# Chạy API + SQL Server
docker-compose up

# Build thủ công
docker build -t document-storage .
docker run -p 8080:8080 \
  -e ConnectionStrings__SqlServer="Server=...;Password=..." \
  -e Auth__AdminKey="your-admin-key" \
  document-storage
```

`docker-compose.yml` bao gồm:
- **api** — ASP.NET Core API (port 8080)
- **db** — SQL Server 2022 với healthcheck
- Volumes cho `uploads/` và database

---

## 4. Quản lý Project & API Key

### 4.1. Tạo project

```http
POST /api/projects
X-API-Key: admin-secret-key-change-in-production
Content-Type: application/json

{
  "name": "App Mobile",
  "description": "Files cho ứng dụng mobile"
}
```

**Response:**
```json
{
  "id": "a1b2c3d4-...",
  "name": "App Mobile",
  "description": "Files cho ứng dụng mobile",
  "apiKey": "pk_3f8a2b1c4d5e6f7g8h9i0j...",
  "isActive": true,
  "createdAt": "2026-06-19T08:00:00Z"
}
```

→ Phân phối `apiKey` cho client ứng dụng.

### 4.2. Tạo lại API key (khi bị lộ)

```http
POST /api/projects/{projectId}/regenerate-key
X-API-Key: admin-secret-key-change-in-production
```

→ API key cũ vô hiệu ngay lập tức. Cache bị xóa.

### 4.3. Deactivate project (tạm khóa)

```http
PATCH /api/projects/{projectId}/active
X-API-Key: admin-secret-key-change-in-production
Content-Type: application/json

false
```

→ Tất cả request dùng API key của project này bị từ chối ngay.

### 4.4. Danh sách projects

```http
GET /api/projects?page=1&pageSize=20
X-API-Key: admin-secret-key-change-in-production
```

---

## 5. API Reference

### Headers chung

| Header | Bắt buộc | Mô tả |
|---|---|---|
| `X-API-Key` | Có | API key của project (`pk_...`) hoặc admin key |
| `X-User-Id` | Tùy chọn | UUID định danh người dùng (cho file ownership) |

### 5.1. Upload file (2 pha)

#### Phase 1 — Init Upload

```http
POST /api/files/init-upload
X-API-Key: pk_3f8a2b1c...
X-User-Id: 550e8400-e29b-41d4-a716-446655440000
Content-Type: application/json

{
  "name": "report.pdf",
  "contentType": "application/pdf",
  "size": 102400
}
```

**Response:**
```json
{
  "fileId": "f1e2d3c4-...",
  "uploadUrl": "https://s3.amazonaws.com/bucket/projects/.../report.pdf?...",
  "headers": {
    "Content-Type": "application/pdf"
  },
  "expiredAt": "2026-06-19T08:05:00Z"
}
```

> **Lưu ý:** Nếu dùng Local storage, `uploadUrl` sẽ trỏ về endpoint `PUT /api/files/local-upload/{storageKey}` của API.

#### Phase 2 — Upload file lên storage

**S3/MinIO:** PUT file binary trực tiếp lên `uploadUrl` (presigned URL).

**Local:** PUT file binary lên API:
```http
PUT /api/files/local-upload/projects/{projectId}/users/{userId}/2026/06/{fileId}.pdf
X-API-Key: pk_3f8a2b1c...
Content-Type: application/pdf

<binary data>
```

> **Lưu ý:** Endpoint local-upload và local-download yêu cầu header `X-API-Key`.

#### Phase 3 — Complete Upload

```http
POST /api/files/complete
X-API-Key: pk_3f8a2b1c...
X-User-Id: 550e8400-e29b-41d4-a716-446655440000
Content-Type: application/json

{
  "fileId": "f1e2d3c4-...",
  "name": "report.pdf",
  "contentType": "application/pdf",
  "size": 102400,
  "description": "Báo cáo tháng 6"
}
```

**Response:**
```json
{
  "id": "f1e2d3c4-...",
  "projectId": "a1b2c3d4-...",
  "name": "report.pdf",
  "extension": "pdf",
  "contentType": "application/pdf",
  "size": 102400,
  "downloadUrl": "https://s3.amazonaws.com/bucket/...?...",
  "createdAt": "2026-06-19T08:00:00Z",
  "description": "Báo cáo tháng 6"
}
```

### 5.2. Download file

```http
GET /api/files/{fileId}
X-API-Key: pk_3f8a2b1c...
```

**Admin (chỉ định project):**
```http
GET /api/files/{fileId}?projectId={projectId}
X-API-Key: admin-key
```

**Response:** Trả về metadata + `downloadUrl` mới (presigned URL có thời hạn theo `DownloadExpirationMinutes`).

### 5.3. Danh sách file

**Theo project của user:**
```http
GET /api/files?page=1&pageSize=20&keyword=report&sortBy=createdAt&sortDirection=desc
X-API-Key: pk_3f8a2b1c...
```

**Admin xem tất cả:**
```http
GET /api/files?page=1&pageSize=20
X-API-Key: admin-secret-key-change-in-production
```

**Admin xem theo project cụ thể:**
```http
GET /api/projects/{projectId}/files?page=1&pageSize=20
X-API-Key: admin-secret-key-change-in-production
```

**Theo user cụ thể:**
```http
GET /api/files/user/{userId}?page=1&pageSize=20
X-API-Key: pk_3f8a2b1c...
```

**Tham số query:**

| Tham số | Mặc định | Mô tả |
|---|---|---|
| `keyword` | null | Tìm theo tên/mô tả |
| `page` | 1 | Trang hiện tại |
| `pageSize` | 20 | Số file/trang (max 100) |
| `sortBy` | createdAt | `name`, `size`, `createdAt` |
| `sortDirection` | asc | `asc` hoặc `desc` |

### 5.4. Cập nhật mô tả

```http
PATCH /api/files/{fileId}/description
X-API-Key: pk_3f8a2b1c...
Content-Type: application/json

{
  "description": "Mô tả mới"
}
```

**Admin:** Thêm `?projectId={projectId}` vào URL.

### 5.5. Xóa file (soft delete)

```http
DELETE /api/files/{fileId}
X-API-Key: pk_3f8a2b1c...
```

**Admin:** Thêm `?projectId={projectId}` vào URL.

→ File bị soft-delete trong DB trước, sau đó xóa khỏi storage.

### 5.6. Quản lý Project (Admin only)

| Method | Endpoint | Mô tả |
|---|---|---|
| POST | `/api/projects` | Tạo project |
| GET | `/api/projects` | Danh sách projects |
| GET | `/api/projects/{id}` | Chi tiết project |
| PUT | `/api/projects/{id}` | Cập nhật name/description |
| PATCH | `/api/projects/{id}/active` | Kích hoạt/vô hiệu |
| POST | `/api/projects/{id}/regenerate-key` | Tạo lại API key |

---

## 6. Ví dụ tích hợp (Client)

### 6.1. C# / HttpClient

```csharp
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("X-API-Key", "pk_3f8a2b1c...");
httpClient.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());

// Phase 1: Init upload
var initResponse = await httpClient.PostAsJsonAsync(
    "https://api.example.com/api/files/init-upload",
    new { Name = "report.pdf", ContentType = "application/pdf", Size = 102400 });
var init = await initResponse.Content.ReadFromJsonAsync<InitUploadResponse>();

// Phase 2: Upload file to storage (S3/MinIO)
using var fileStream = File.OpenRead("report.pdf");
var uploadResponse = await httpClient.PutAsync(init.UploadUrl, new StreamContent(fileStream));

// Phase 3: Complete upload
var completeResponse = await httpClient.PostAsJsonAsync(
    "https://api.example.com/api/files/complete",
    new {
        FileId = init.FileId,
        Name = "report.pdf",
        ContentType = "application/pdf",
        Size = 102400,
        Description = "Báo cáo tháng 6"
    });
var file = await completeResponse.Content.ReadFromJsonAsync<FileDto>();
```

### 6.2. JavaScript / Fetch

```javascript
const API = 'https://api.example.com';
const headers = {
  'X-API-Key': 'pk_3f8a2b1c...',
  'X-User-Id': '550e8400-e29b-41d4-a716-446655440000'
};

// Phase 1: Init upload
const initRes = await fetch(`${API}/api/files/init-upload`, {
  method: 'POST',
  headers: { ...headers, 'Content-Type': 'application/json' },
  body: JSON.stringify({ name: 'report.pdf', contentType: 'application/pdf', size: 102400 })
});
const { fileId, uploadUrl } = await initRes.json();

// Phase 2: Upload to S3
await fetch(uploadUrl, {
  method: 'PUT',
  headers: { 'Content-Type': 'application/pdf' },
  body: fileBuffer
});

// Phase 3: Complete
const completeRes = await fetch(`${API}/api/files/complete`, {
  method: 'POST',
  headers: { ...headers, 'Content-Type': 'application/json' },
  body: JSON.stringify({
    fileId, name: 'report.pdf', contentType: 'application/pdf',
    size: 102400, description: 'Báo cáo tháng 6'
  })
});
const fileDto = await completeRes.json();
```

---

## 7. Bảo mật

### 7.1. Phân quyền

| Role | Cách xác định | Quyền |
|---|---|---|
| **Admin** | `X-API-Key` = AdminKey (từ config) | Tất cả: tạo/xóa project, xem mọi file |
| **Project User** | `X-API-Key` = `pk_...` (project API key) | Chỉ file trong project của mình |

### 7.2. Thu hồi quyền

| Tình huống | Cách thực hiện | Hiệu lực |
|---|---|---|
| Khóa 1 project | `PATCH /api/projects/{id}/active` → `false` | **Ngay lập tức** |
| Đổi API key | `POST /api/projects/{id}/regenerate-key` | **Ngay lập tức** (key cũ vô hiệu) |
| Khóa tất cả | Đổi `Auth:AdminKey` trong config + restart | Mất quyền admin |

### 7.3. Khuyến nghị production

- **Không lưu secrets trong `appsettings.json`** — dùng environment variables hoặc user secrets
- Đổi `AdminKey` thành giá trị mạnh, ngẫu nhiên
- Dùng HTTPS
- Giới hạn `AllowedExtensions` theo nhu cầu
- Cấu hình `MaxUploadSizeBytes` phù hợp
- Backup database định kỳ
- Monitor storage disk (nếu dùng Local)

---

## 8. Xử lý sự cố

### Lỗi 401 Unauthorized

```
 Nguyên nhân: X-API-Key không hợp lệ hoặc project bị deactivate
 Khắc phục: Kiểm tra API key, kiểm tra project IsActive
```

### Lỗi 403 Forbidden

```
 Nguyên nhân: User thường truy cập endpoint admin-only
 Khắc phục: Dùng AdminKey cho các thao tác quản lý
```

### Lỗi 400 Invalid File Type

```
 Nguyên nhân: Extension không nằm trong AllowedExtensions
 Khắc phục: Thêm extension vào config hoặc đổi file
```

### Upload thành công nhưng Complete báo File Not Found

```
 Nguyên nhân: File chưa upload lên storage, hoặc upload hết hạn
 Khắc phục: Upload lại trong thời hạn UploadExpirationMinutes (mặc định 5 phút)
```

### Migration lỗi

```
 Nguyên nhân: Database chưa tạo hoặc connection string sai
 Khắc phục:
   1. Tạo database thủ công
   2. Kiểm tra connection string
   3. Chạy: dotnet ef database update
```
