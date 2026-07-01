# Chi Tiết gRPC Trong Dự Án MMS (Manga Management System)

## Tổng Quan Kiến Trúc

Dự án MMS sử dụng gRPC cho **giao tiếp nội bộ giữa các service** (service-to-service), không expose trực tiếp ra ngoài cho client web/mobile.

```
┌──────────────────────────────────────────────────────────────────┐
│                        Client (Browser / App)                    │
└────────────────────────────┬─────────────────────────────────────┘
                             │ REST / HTTP/1.1
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│                    MMS.API  (Port 5000)                          │
│  - REST Controllers                                              │
│  - GrpcEmailService    →  gRPC Client                           │
│  - NotificationGrpcClientWrapper  →  gRPC Client               │
└────────────┬─────────────────────────────────────────────────────┘
             │ gRPC / HTTP/2
             ▼
┌────────────────────────────────────────────────────────────────┐
│         MMS.NotificationService  (Port 5102)                   │
│  - NotificationGrpcService  (gRPC Server)                     │
│  - SmtpEmailSender  (gửi email thực qua SMTP/MailKit)         │
│  - DbUserEmailLookup  (tra cứu email từ DB theo userId)       │
└────────────────────────────────────────────────────────────────┘
```

### Các Service gRPC trong dự án

| Service | Trạng thái | Port | Vai trò |
|---------|-----------|------|---------|
| `NotificationService` | **Hoạt động** | 5102 | Gửi email thông báo |
| `ImageProcessor` | **Được đăng ký, chưa implement server** | 5101 | Xử lý/optimize ảnh manga |
| `AuditLogService` | **Proto có sẵn, chưa implement** | 5103 | Ghi audit log |

---

## Cấu Trúc File Quan Trọng

```
MMS.Protos/                                  ← Thư viện dùng chung (shared)
├── MMS.Protos.csproj
└── Protos/
    ├── AuditLog/v1/audit_log.proto
    ├── Common/v1/common.proto
    ├── ImageProcessing/v1/image_processing.proto
    └── Notification/v1/notification.proto

MMS.NotificationService/                     ← gRPC Server (service độc lập)
├── Program.cs
└── Services/
    ├── NotificationGrpcService.cs           ← Implementation gRPC server
    ├── SmtpEmailSender.cs                   ← Gửi email thực
    └── DbUserEmailLookup.cs                 ← Tra cứu email từ DB

src/MMS.Infrastructure/
├── gRPC/
│   ├── GrpcClientOptions.cs                 ← Config endpoint, timeout, retry
│   ├── GrpcClientRegistrationExtensions.cs  ← Đăng ký gRPC clients vào DI
│   ├── GrpcAuthClientInterceptor.cs         ← Inject JWT token vào outgoing call
│   └── GrpcAuthServerInterceptor.cs         ← Validate JWT token trên server
├── Services/
│   ├── GrpcEmailService.cs                  ← IEmailService → gọi NotificationService qua gRPC
│   └── NotificationGrpcClientWrapper.cs     ← INotificationGrpcClient → gọi gRPC trực tiếp
└── DependencyInjection.cs                   ← Kết nối tất cả

src/MMS.Application/
├── Interfaces/Services/
│   └── INotificationGrpcClient.cs           ← Interface abstraction
└── Services/
    └── SubmissionService.cs                 ← Gọi gRPC khi duyệt bản thảo
```

---

## Định Nghĩa Proto Files

### 1. `notification.proto` — Thông báo người dùng

```proto
service NotificationService {
  rpc SendOne(SendNotificationRequest) returns (SendNotificationResponse);  // Unary
  rpc FanOut(FanOutRequest) returns (stream DeliveryStatus);               // Server Streaming
}
```

| RPC | Kiểu | Mô tả |
|-----|------|-------|
| `SendOne` | Unary | Gửi một thông báo cho một người dùng |
| `FanOut` | Server Streaming | Gửi broadcast cho nhiều người, server trả từng kết quả |

**Messages quan trọng:**
- `SendNotificationRequest`: `recipient_user_id`, `title`, `body`, `channel` ("email"/"in_app"/"push"), `metadata` (key-value cho direct_email)
- `FanOutRequest`: `recipient_user_ids[]`, `title`, `body`, `channel`
- `DeliveryStatus`: trạng thái gửi cho từng user (streaming response)

### 2. `image_processing.proto` — Xử lý ảnh manga

```proto
service ImageProcessor {
  rpc ProcessPage(stream PageChunk) returns (stream ProcessProgress);  // Bidirectional Streaming
  rpc GenerateThumbnail(GenerateThumbnailRequest) returns (ThumbnailResult);  // Unary
}
```

| RPC | Kiểu | Mô tả |
|-----|------|-------|
| `ProcessPage` | Bidirectional Streaming | Upload ảnh theo chunks, nhận tiến trình xử lý realtime |
| `GenerateThumbnail` | Unary | Sinh thumbnail từ key ảnh đã có |

**ProcessStage enum:** `RECEIVED` → `RESIZING` → `OPTIMIZING` → `UPLOADED` → `DONE` / `FAILED`

### 3. `audit_log.proto` — Ghi nhật ký hệ thống

```proto
service AuditLogService {
  rpc WriteStream(stream AuditEntry) returns (WriteAck);   // Client Streaming
  rpc Query(AuditQueryRequest) returns (AuditQueryResponse);  // Unary
}
```

| RPC | Kiểu | Mô tả |
|-----|------|-------|
| `WriteStream` | Client Streaming | Ghi nhiều audit entries theo batch |
| `Query` | Unary | Truy vấn audit log với phân trang |

### 4. `common.proto` — Shared types

Chứa các message dùng chung: `UserRef`, `PaginationRequest`, `ErrorDetail`.

---

## Quy Trình Chi Tiết: Gửi Email Thông Báo (NotificationService)

### Luồng 1: `GrpcEmailService` — Gửi email theo địa chỉ trực tiếp

**Khi nào dùng:** Các tình huống mà code đã biết sẵn email (gửi lời mời, cảnh báo hiệu suất)

```
SubmissionController / InvitationController
        │
        │ inject IEmailService
        ▼
GrpcEmailService (src/MMS.Infrastructure/Services/)
        │
        │ gọi _client.SendOneAsync(SendNotificationRequest {
        │     RecipientUserId = "",          ← bỏ trống
        │     Title = subject,
        │     Body = body,
        │     Channel = "email",
        │     Metadata["direct_email"] = email  ← email trực tiếp trong metadata
        │ })
        │
        │ HTTP/2 + gRPC (qua GrpcAuthClientInterceptor)
        │ + Header: Authorization: Bearer <ServiceToken>
        ▼
NotificationGrpcService (MMS.NotificationService/)
        │
        │ SendOne() handler kiểm tra:
        │   if Metadata["direct_email"] exists → dùng email đó
        │   else → DbUserEmailLookup.GetEmailAsync(userId)
        ▼
SmtpEmailSender.SendAsync(email, name, subject, body)
        │
        │ MailKit → SMTP server
        ▼
       Email đến hộp thư người dùng
```

### Luồng 2: `NotificationGrpcClientWrapper` — Gửi email theo userId

**Khi nào dùng:** Khi chỉ biết userId (ví dụ: sau khi duyệt bản thảo Submission)

```
SubmissionService.FinalizeDecisionAsync()
        │
        │ inject INotificationGrpcClient
        ▼
NotificationGrpcClientWrapper (src/MMS.Infrastructure/Services/)
        │
        │ SendEmailAsync(userId, title, body) →
        │   _client.SendOneAsync(SendNotificationRequest {
        │       RecipientUserId = userId,   ← userId để server tra cứu
        │       Title = title,
        │       Body = body,
        │       Channel = "email"
        │   })
        │
        │ HTTP/2 + gRPC (qua GrpcAuthClientInterceptor)
        ▼
NotificationGrpcService.SendOne()
        │
        │ Không có direct_email trong Metadata
        │ → DbUserEmailLookup.GetEmailAsync(userId)
        │       └─ SELECT Email FROM Users WHERE Id = @userId
        ▼
SmtpEmailSender.SendAsync(email, ...)
```

### Luồng 3: `FanOut` — Gửi broadcast nhiều người

```
NotificationGrpcClientWrapper.FanOutEmailAsync(userIds[], title, body)
        │
        │ FanOutRequest { RecipientUserIds = userIds[], Channel = "email" }
        │ → _client.FanOut(request)  ← trả về IAsyncStreamReader
        ▼
NotificationGrpcService.FanOut()
        │
        │ foreach userId in request.RecipientUserIds:
        │   1. DbUserEmailLookup.GetEmailAsync(userId)
        │   2. SmtpEmailSender.SendAsync(email, ...)
        │   3. await responseStream.WriteAsync(DeliveryStatus { ... })
        │                                   ↑ streaming từng kết quả về client
        ▼
Client nhận DeliveryStatus từng cái qua:
  await foreach (var status in stream.ResponseStream.ReadAllAsync())
```

---

## Cơ Chế Bảo Mật gRPC (Interceptors)

### Client Interceptor — `GrpcAuthClientInterceptor`

Tự động inject **Service Token** (JWT tĩnh, long-lived) vào header của **mọi outgoing gRPC call** từ MMS.API.

```csharp
// Tự động thêm vào trước mỗi gRPC call:
headers.Add("authorization", $"Bearer {GetServiceToken()}");

// Token lấy từ config:
_config["Grpc:ServiceToken"]
```

**Lưu ý:** Đây là Service-to-Service token, khác với User JWT token. Dùng để xác thực rằng request đến từ MMS.API chứ không phải client bên ngoài.

### Server Interceptor — `GrpcAuthServerInterceptor`

Validate JWT token trên phía server (NotificationService hoặc ImageService) trước khi xử lý request.

```
Mọi gRPC call đến NotificationService
        │
        ▼
GrpcAuthServerInterceptor.UnaryServerHandler()
        │
        │ Đọc header "authorization"
        │ Validate JWT với Grpc:ServiceTokenSecret
        │ Nếu hợp lệ → context.UserState["Authenticated"] = true
        │ Nếu không hợp lệ → throw RpcException(StatusCode.Unauthenticated)
        ▼
NotificationGrpcService.SendOne() / FanOut()
```

---

## Đăng Ký Dependency Injection

### Phía Server (MMS.NotificationService/Program.cs)

```csharp
// 1. Đăng ký gRPC server
builder.Services.AddGrpc();

// 2. Map service implementation
app.MapGrpcService<NotificationGrpcService>();
```

### Phía Client (src/MMS.Infrastructure/DependencyInjection.cs)

```csharp
// 1. Đăng ký wrapper implementations
services.AddScoped<IEmailService, GrpcEmailService>();
services.AddScoped<INotificationGrpcClient, NotificationGrpcClientWrapper>();

// 2. Đăng ký gRPC typed clients với Polly retry
services.AddMmsGrpcClients(configuration);
```

### Chi tiết `AddMmsGrpcClients` (GrpcClientRegistrationExtensions.cs)

```csharp
// Đăng ký client interceptor
services.AddTransient<GrpcAuthClientInterceptor>();

// NotificationService client
services.AddGrpcClient<NotificationService.NotificationServiceClient>(o => {
    o.Address = new Uri(options.NotificationEndpoint);  // "https://localhost:5102"
})
.ConfigureChannel(ch => { ch.MaxReceiveMessageSize = 64 * 1024 * 1024; })  // 64MB
.AddInterceptor<GrpcAuthClientInterceptor>()  // Tự inject Service Token
.AddTransientHttpErrorPolicy(p =>
    p.WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * 2^attempt)));
// Retry policy: 400ms → 800ms → 1600ms
```

---

## Cấu Hình (appsettings.json)

### MMS.API

```json
{
  "Grpc": {
    "ImageProcessorEndpoint": "https://localhost:5101",
    "NotificationEndpoint": "https://localhost:5102",
    "AuditLogEndpoint": "https://localhost:5103",
    "MaxReceiveMessageSizeMb": 64,
    "RetryCount": 3,
    "DefaultDeadlineSeconds": 30,
    "ServiceToken": "<JWT token để gửi đi>",
    "ServiceTokenSecret": "<secret để validate token đến>"
  }
}
```

### MMS.NotificationService

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "...",
    "Password": "...",
    "FromEmail": "...",
    "FromName": "MMS System"
  }
}
```

---

## Code Generation — Cách Proto Sinh Ra C# Code

File `MMS.Protos.csproj` cấu hình **Grpc.Tools** để tự động generate:

```xml
<PackageReference Include="Grpc.Tools" Version="2.72.0" PrivateAssets="All" />
<Protobuf Include="Protos\**\*.proto" GrpcServices="Both" />
<!-- GrpcServices="Both" = sinh cả client stub + server base class -->
```

**Kết quả generate cho mỗi .proto:**

| Proto | C# class được sinh |
|-------|-------------------|
| `notification.proto` | `NotificationService.NotificationServiceBase` (server base) |
| | `NotificationService.NotificationServiceClient` (client stub) |
| `image_processing.proto` | `ImageProcessor.ImageProcessorBase` (server base) |
| | `ImageProcessor.ImageProcessorClient` (client stub) |
| `audit_log.proto` | `AuditLogService.AuditLogServiceBase` |
| | `AuditLogService.AuditLogServiceClient` |

---

## Các Loại gRPC Communication Pattern Trong Dự Án

| Pattern | Proto Method | Hiện trạng |
|---------|-------------|-----------|
| **Unary** | `NotificationService.SendOne` | Hoạt động |
| **Unary** | `NotificationService.GenerateThumbnail` | Client đăng ký, server chưa impl |
| **Unary** | `AuditLogService.Query` | Proto only |
| **Server Streaming** | `NotificationService.FanOut` | Hoạt động |
| **Server Streaming** | `ImageProcessor.ProcessPage` | Client đăng ký, server chưa impl |
| **Client Streaming** | `AuditLogService.WriteStream` | Proto only |

---

## Fallback & Error Handling

### GrpcEmailService — Fallback khi gRPC fail

```csharp
catch (Exception ex)
{
    // gRPC fail → fallback sang console log (development mode)
    _logger.LogWarning(ex, "Failed to send email via gRPC...");
    Console.WriteLine($"[Email Simulation - gRPC unavailable]\nTo: {email}\n...");
}
```

### SmtpEmailSender — Fallback khi SMTP không cấu hình

```csharp
if (string.IsNullOrWhiteSpace(_options.Host) || ...)
{
    // Không có SMTP thực → log ra console thay vì crash
    _logger.LogInformation("[EMAIL SIMULATION] ...");
    return true;
}
```

### Retry Policy (Polly)

Tự động retry khi gặp lỗi HTTP tạm thời:
- Lần 1: chờ 400ms
- Lần 2: chờ 800ms
- Lần 3: chờ 1600ms (exponential backoff)

---

## Sequence Diagram — Duyệt Bản Thảo Kích Hoạt gRPC Email

```
Reviewer            MMS.API              SubmissionService     NotificationGrpcClientWrapper     NotificationGrpcService     SmtpEmailSender
   │                   │                        │                          │                              │                       │
   │ POST /finalize     │                        │                          │                              │                       │
   │─────────────────→ │                        │                          │                              │                       │
   │                   │ FinalizeDecisionAsync() │                          │                              │                       │
   │                   │──────────────────────→ │                          │                              │                       │
   │                   │                        │ Cập nhật DB Submission   │                              │                       │
   │                   │                        │──→ SaveChangesAsync()    │                              │                       │
   │                   │                        │                          │                              │                       │
   │                   │                        │ SendEmailAsync(userId,   │                              │                       │
   │                   │                        │  title, msg)             │                              │                       │
   │                   │                        │─────────────────────────→│                              │                       │
   │                   │                        │                          │ gRPC SendOne (HTTP/2)        │                       │
   │                   │                        │                          │ + Bearer ServiceToken        │                       │
   │                   │                        │                          │─────────────────────────────→│                       │
   │                   │                        │                          │                              │ ValidateToken()        │
   │                   │                        │                          │                              │ DbUserEmailLookup()   │
   │                   │                        │                          │                              │ (SELECT email FROM DB)│
   │                   │                        │                          │                              │──────────────────────→│
   │                   │                        │                          │                              │ SmtpEmailSender()     │
   │                   │                        │                          │                              │← SendNotificationResponse
   │                   │                        │                          │← response {success=true}    │                       │
   │                   │                        │← SubmissionResponse      │                              │                       │
   │←── 200 OK ────── │                        │                          │                              │                       │
```

---

## Ghi Chú Thêm

1. **Shared Proto Library**: `MMS.Protos` là project riêng, cả `MMS.API`, `MMS.Infrastructure` và `MMS.NotificationService` đều tham chiếu đến nó. Đảm bảo contract nhất quán giữa client và server.

2. **GrpcServices="Both"**: Mỗi `.proto` sinh ra cả `*Base` (server) lẫn `*Client` (client stub). Không cần viết tay serialization/deserialization.

3. **HTTP/2 required**: gRPC chạy trên HTTP/2. Trong development với localhost có thể cần `AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true)` nếu dùng HTTP (không phải HTTPS).

4. **Transport**: Dự án dùng `Grpc.AspNetCore` (server) và `Grpc.Net.Client` thông qua `AddGrpcClient` (client) — đây là thư viện chính thức của Microsoft/Google cho .NET, không phải thư viện cũ `Grpc.Core`.
