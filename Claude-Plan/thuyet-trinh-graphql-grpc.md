# THUYẾT TRÌNH: GRAPHQL & gRPC TRONG DỰ ÁN MANGA MANAGEMENT SYSTEM

> **Dự án:** Manga Management System (MMS) — Hệ thống quản lý xuất bản manga đa vai trò  
> **Môn học:** PRN232  
> **Công nghệ trọng tâm:** GraphQL (HotChocolate) · gRPC (Protobuf) · .NET 8  
> **Ngày thuyết trình:** 2026-05-31

---

## MỤC LỤC

1. [Giới thiệu dự án & vì sao cần 4 giao thức](#1-giới-thiệu-dự-án)
2. [Phần 1 — GraphQL là gì?](#2-graphql-là-gì)
3. [Phần 2 — GraphQL trong dự án MMS](#3-graphql-trong-dự-án-mms)
4. [Phần 3 — gRPC là gì?](#4-grpc-là-gì)
5. [Phần 4 — gRPC trong dự án MMS](#5-grpc-trong-dự-án-mms)
6. [Phần 5 — GraphQL vs gRPC: Khi nào dùng cái nào?](#6-graphql-vs-grpc)
7. [Phần 6 — Demo luồng thực tế trong MMS](#7-demo-luồng-thực-tế)
8. [Phần 7 — Lợi ích đạt được & bài học](#8-lợi-ích--bài-học)
9. [Phần 8 — Câu hỏi thường gặp (Q&A)](#9-qa)

---

## 1. GIỚI THIỆU DỰ ÁN

### Manga Management System là gì?

MMS là **nền tảng quản lý toàn bộ vòng đời xuất bản manga** — từ khi Mangaka nộp bản thảo cho đến lúc tác phẩm được phê duyệt đăng tải. Hệ thống có **5 vai trò** nghiệp vụ phức tạp:

| Vai trò | Nhiệm vụ |
|---|---|
| **Mangaka** | Upload bản thảo, theo dõi tiến trình, nhận phản hồi |
| **Tantou Editor** | Review annotation, annotate trang, forward/reject |
| **Editorial Board** | Vote phê duyệt tập thể, quyết định cuối |
| **Assistant** | Hỗ trợ annotation, không có quyền vote |
| **Admin** | Quản lý hệ thống, phân quyền |

### Vì sao cần 4 giao thức song song?

Một hệ thống phức tạp KHÔNG thể dùng 1 giao thức duy nhất cho mọi trường hợp:

```
┌─────────────────────────────────────────────────────────────────┐
│                    Kiến trúc giao thức MMS                       │
│                                                                   │
│  Browser / Mobile App                                            │
│       │                                                           │
│       ├──── GraphQL ──── POST /graphql ──────► MMS.API          │
│       │      (CRUD nghiệp vụ, query linh hoạt)                  │
│       │                                                           │
│       ├──── SignalR ──── WebSocket ──────────► MMS.API          │
│       │      (Realtime: annotation live, vote update)            │
│       │                                                           │
│       └──── REST ────── POST /upload ──────► MMS.API            │
│              (Upload file binary - ảnh manga)                    │
│                                                                   │
│  MMS.API (Backend)                                               │
│       │                                                           │
│       └──── gRPC ────── HTTP/2 Protobuf ───► NotificationService│
│              (Server-to-server: gửi email, audit log)            │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

**Quy tắc vàng:**
- **GraphQL** → Browser/Mobile giao tiếp với Backend (CRUD, query)
- **gRPC** → Backend ↔ Backend nội bộ (notification, audit)
- **SignalR** → Push realtime từ Backend → Frontend
- **REST** → Upload file binary (ảnh, PDF)

> 💡 **Browser KHÔNG BAO GIỜ gọi gRPC trực tiếp.** gRPC dùng Protobuf binary, không chạy trên trình duyệt thuần túy.

---

## 2. GRAPHQL LÀ GÌ?

### Định nghĩa

**GraphQL** là ngôn ngữ truy vấn (query language) cho API + runtime để thực thi query đó. Ra đời 2015 bởi Facebook, trở thành chuẩn công nghiệp cho modern web API.

### So sánh REST vs GraphQL

| Khía cạnh | REST | GraphQL |
|---|---|---|
| **Số endpoint** | Nhiều (`/submissions`, `/submissions/123`, `/submissions/123/comments`) | **1 endpoint duy nhất** (`/graphql`) |
| **Format request** | URL + HTTP method (GET/POST/PUT/DELETE) | JSON `{ query, variables }` qua POST |
| **Format response** | BE tự quyết trả field nào | **FE chọn đúng field cần** |
| **Over-fetching** | Thường xuyên (30 field nhưng chỉ cần 3) | **Zero over-fetching** |
| **Under-fetching** | Phải gọi nhiều request | **1 request lấy đủ data** |
| **Schema discovery** | OpenAPI (viết tay) | **Introspection tự động** |
| **Versioning** | `/api/v1/`, `/api/v2/` | Schema evolve qua `@deprecated` |
| **Realtime** | Cần thêm WebSocket riêng | **Subscription built-in** |

### Ví dụ thực tế: Over-fetching với REST

**REST (cách cũ):**
```
GET /api/submissions/abc-123
→ Trả về toàn bộ 30 fields:
{
  "id": "abc-123",
  "status": "PENDING",
  "mangakaId": "...",
  "tantouId": "...",
  "manuscriptId": "...",
  "createdAt": "...",
  "updatedAt": "...",
  "comments": [...],     ← 10 comments đầy đủ
  "history": [...],      ← 50 transitions
  "votes": [...],        ← 5 votes
  "annotations": [...]   ← 100 annotations
  ...                    ← còn 20 field nữa
}
```
→ FE chỉ cần `id + status` để render danh sách, nhưng phải nhận 30 field. **Waste bandwidth + RAM**

**GraphQL (cách mới trong MMS):**
```graphql
query {
  getSubmissionById(id: "abc-123") {
    id
    status          # chỉ lấy 2 field cần thiết
  }
}
→ Response:
{
  "data": {
    "getSubmissionById": {
      "id": "abc-123",
      "status": "PENDING"
    }
  }
}
```
→ **Đúng 2 field cần, không hơn không kém.**

### Cấu trúc cơ bản của GraphQL

```
GraphQL Schema
├── Query     → Đọc dữ liệu (GET equivalent)
├── Mutation  → Thay đổi dữ liệu (POST/PUT/DELETE equivalent)
└── Subscription → Realtime push (WebSocket)
```

---

## 3. GRAPHQL TRONG DỰ ÁN MMS

### 3.1. Thư viện: HotChocolate

MMS dùng **HotChocolate** — thư viện GraphQL mạnh nhất cho .NET:

```csharp
// src/MMS.API/DependencyInjection.cs
services
    .AddGraphQLServer()
    .AddQueryType<UserQueries>()          // Query root
    .AddTypeExtension<SeriesQueries>()    // Extend Query
    .AddTypeExtension<SubmissionQueries>()
    .AddTypeExtension<TaskQueries>()
    .AddTypeExtension<AnnotationQueries>()
    .AddMutationType<AuthMutations>()     // Mutation root
    .AddTypeExtension<SeriesMutations>()
    .AddTypeExtension<SubmissionMutations>()
    .AddAuthorization()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .AddErrorFilter<GraphQLErrorFilter>();
```

### 3.2. Cấu trúc thư mục GraphQL

```
src/MMS.API/
└── GraphQL/
    ├── Queries/                     ← Đọc dữ liệu
    │   ├── UserQueries.cs           ← ROOT type (base)
    │   ├── SeriesQueries.cs         ← [ExtendObjectType]
    │   ├── SubmissionQueries.cs
    │   ├── RegionQueries.cs
    │   ├── TaskQueries.cs
    │   ├── AnnotationQueries.cs
    │   ├── ChapterQueries.cs
    │   ├── PageQueries.cs
    │   ├── ManuscriptQueries.cs
    │   └── CandidateApplicationQueries.cs
    │
    ├── Mutations/                   ← Thay đổi dữ liệu
    │   ├── AuthMutations.cs         ← Login/Register
    │   ├── SeriesMutations.cs
    │   ├── SubmissionMutations.cs
    │   ├── TaskMutations.cs
    │   ├── AnnotationMutations.cs
    │   └── ...
    │
    ├── GraphQLErrorFilter.cs        ← Bắt exception → chuẩn hóa lỗi
    └── GraphQLHelper.cs             ← Lấy userId từ JWT claims
```

### 3.3. Anatomy một Query

**File:** [src/MMS.API/GraphQL/Queries/SeriesQueries.cs](../src/MMS.API/GraphQL/Queries/SeriesQueries.cs)

```csharp
[ExtendObjectType(typeof(UserQueries))]   // ① Ghép vào Query root
public class SeriesQueries
{
    [Authorize(Roles = new[] { "Editorial Board", "Admin" })]  // ② Phân quyền theo role
    public async Task<IEnumerable<SeriesResponse>> GetPendingSeries(
        [Service] ISeriesService seriesService)   // ③ DI inject service
    {
        return await seriesService.GetPendingSeriesAsync();
    }

    [UseFiltering]    // ④ Tự động support filter: ?where=title_contains: "Dragon"
    public async Task<IEnumerable<SeriesResponse>> GetMySeries(
        [Service] ISeriesService seriesService,
        Guid mangakaId)
    {
        return await seriesService.GetByMangakaAsync(mangakaId);
    }

    public async Task<SeriesResponse?> GetSeriesById(
        [Service] ISeriesService seriesService,
        Guid id)   // ⑤ Parameter tự động binding từ GraphQL variables
    {
        return await seriesService.GetByIdAsync(id);
    }
}
```

**Giải thích từng annotation:**
1. `[ExtendObjectType]` → Pattern "type extension" để tổ chức code theo entity, không nhồi tất cả vào 1 class
2. `[Authorize(Roles = ...)]` → Role-based auth declarative tại resolver
3. `[Service]` → HotChocolate DI injection (khác `[FromServices]` của ASP.NET)
4. `[UseFiltering]` → HotChocolate tự sinh filtering logic, FE có thể gửi `{ getMySeries(mangakaId: "...", where: { title: { contains: "Dragon" }}) { id title } }`
5. Parameter tự động map từ GraphQL variables

**GraphQL query tương ứng mà FE gửi:**
```graphql
# Lấy danh sách series của mangaka
query GetMySeries($mangakaId: UUID!) {
  getMySeries(mangakaId: $mangakaId) {
    id
    title
    status
    createdAt
    # Không cần chapters, votes, annotations → không trả về
  }
}
```

### 3.4. Anatomy một Mutation

**File:** [src/MMS.API/GraphQL/Mutations/SubmissionMutations.cs](../src/MMS.API/GraphQL/Mutations/SubmissionMutations.cs)

```csharp
[ExtendObjectType(typeof(AuthMutations))]
public class SubmissionMutations
{
    // ① Tantou Editor forward submission lên Editorial Board
    [Authorize(Roles = new[] { "Tantou Editor", "Editorial Board" })]
    public async Task<SubmissionResponse> ForwardToBoard(
        [Service] ISubmissionService submissionService,
        ClaimsPrincipal claimsPrincipal,     // ② Lấy từ JWT context
        Guid submissionId,
        string comment)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);  // ③ Extract userId
        var result = await submissionService.ForwardToBoardAsync(
            submissionId, userId, comment);
        return result.Data!;   // ④ Trả entity đã update → FE cache update
    }

    // Mangaka nộp bản thảo
    [Authorize(Roles = new[] { "Mangaka" })]
    public async Task<SubmissionResponse> CreateSubmission(
        [Service] ISubmissionService submissionService,
        ClaimsPrincipal claimsPrincipal,
        CreateSubmissionInput input)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        var result = await submissionService.CreateAsync(input, userId);
        return result.Data!;
    }
}
```

**GraphQL mutation tương ứng:**
```graphql
mutation ForwardToBoard($submissionId: UUID!, $comment: String!) {
  forwardToBoard(submissionId: $submissionId, comment: $comment) {
    id
    status        # FE nhận lại status mới để update UI ngay
    updatedAt
  }
}
```

### 3.5. Error Handling với GraphQLErrorFilter

**File:** [src/MMS.API/GraphQL/GraphQLErrorFilter.cs](../src/MMS.API/GraphQL/GraphQLErrorFilter.cs)

```csharp
public class GraphQLErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        return error.Exception switch
        {
            // Domain exception → client error (không log stack trace)
            DomainException ex => error
                .WithMessage(ex.Message)
                .WithCode(ex.ErrorCode)
                .RemoveException(),

            // Unauthorized → chuẩn hóa
            UnauthorizedAccessException => error
                .WithMessage("Không có quyền thực hiện thao tác này")
                .WithCode("UNAUTHORIZED")
                .RemoveException(),

            // Mọi exception khác → ẩn chi tiết (bảo mật)
            _ => error
                .WithMessage("Đã xảy ra lỗi hệ thống")
                .WithCode("INTERNAL_ERROR")
                .RemoveException()
        };
    }
}
```

**Response lỗi chuẩn hóa mà FE nhận được:**
```json
{
  "errors": [{
    "message": "Không đủ quyền forward submission này",
    "extensions": {
      "code": "INSUFFICIENT_PERMISSION"
    }
  }],
  "data": null
}
```

### 3.6. Vòng đời 1 GraphQL Request trong MMS

```
1. FE gửi:
   POST /graphql
   { "query": "query { getSubmissionById(id: \"abc\") { id status } }" }

2. HotChocolate: Parse query → AST tree

3. HotChocolate: Validate schema (field tồn tại? type đúng? auth?)

4. HotChocolate: Execute resolvers
   ↓
   SubmissionQueries.GetSubmissionById()
   ↓
   [Service] ISubmissionService → DI resolve
   ↓
   submissionService.GetByIdAsync("abc")
   ↓
   SubmissionRepository → EF Core → SQL Server
   ↓
   Map Entity → SubmissionResponse DTO

5. HotChocolate: Pick ONLY requested fields từ DTO
   (id, status) → bỏ qua mọi field khác

6. Return JSON:
   { "data": { "getSubmissionById": { "id": "abc", "status": "PENDING" } } }
```

### 3.7. Playground & Introspection

HotChocolate tự sinh **Banana Cake Pop** playground tại `/graphql`:

```
https://localhost:7001/graphql
```

Tại đây có thể:
- Khám phá toàn bộ schema (tất cả queries, mutations, types)
- Chạy query trực tiếp với authentication
- Xem documentation tự sinh
- **Không cần viết Swagger/OpenAPI** — schema tự cập nhật theo code

---

## 4. gRPC LÀ GÌ?

### Định nghĩa

**gRPC** (gRPC Remote Procedure Calls) là framework RPC hiệu suất cao do Google phát triển, chạy trên HTTP/2. Dữ liệu được serialize bằng **Protocol Buffers (Protobuf)** — định dạng binary, nhỏ hơn JSON 3-5 lần.

### So sánh REST vs gRPC

| Khía cạnh | REST/JSON | gRPC/Protobuf |
|---|---|---|
| **Protocol** | HTTP/1.1 | **HTTP/2 (multiplexing)** |
| **Serialization** | JSON (text, ~100 bytes/msg) | **Protobuf binary (~20 bytes/msg)** |
| **Performance** | Trung bình | **5-10x nhanh hơn** |
| **Contract** | Optional (OpenAPI) | **Bắt buộc (.proto file)** |
| **Streaming** | Không | **4 kiểu streaming** |
| **Browser support** | Hoàn toàn | Cần gRPC-Web proxy |
| **Use case** | External API | **Internal microservices** |
| **Code generation** | Không | **Tự sinh client/server code** |

### 4 kiểu gRPC Calls

```
1. UNARY                          2. SERVER STREAMING
   Client ────request────► Server    Client ───request────► Server
   Client ◄───response─── Server    Client ◄──stream①─── Server
                                     Client ◄──stream②─── Server
                                     Client ◄──stream③─── Server
                                     Client ◄──[end]───── Server

3. CLIENT STREAMING               4. BIDIRECTIONAL STREAMING
   Client ───stream①────► Server    Client ◄──►──►──►─── Server
   Client ───stream②────► Server    (Cả 2 gửi/nhận đồng thời)
   Client ───stream③────► Server
   Client ◄──response─── Server
```

**Trong MMS:**
- `SendOne` → **Unary** (gửi 1 email)
- `FanOut` → **Server Streaming** (gửi nhiều email, stream trạng thái giao)
- `WriteStream` → **Client Streaming** (batch audit log entries)

---

## 5. gRPC TRONG DỰ ÁN MMS

### 5.1. Kiến trúc tổng quan

```
┌─────────────────────┐          HTTP/2 + Protobuf          ┌──────────────────────────┐
│     MMS.API         │                                      │   MMS.NotificationService │
│   (gRPC CLIENT)     │ ─────────────────────────────────► │     (gRPC SERVER)         │
│                     │                                      │                           │
│ IEmailService       │   SendNotificationRequest            │ NotificationGrpcService   │
│   ↓                 │   ─────────────────────────────►   │   ↓                       │
│ GrpcEmailService    │                                      │ SmtpEmailSender           │
│   ↓                 │   ◄─────────────────────────────   │   ↓                       │
│ gRPC Client Stub    │   SendNotificationResponse           │ MailKit → SMTP Server     │
└─────────────────────┘                                      └──────────────────────────┘
         │
         │ GrpcAuthClientInterceptor
         │ (Thêm JWT vào mọi call)
```

**Tại sao không dùng REST cho notification?**

| Tiêu chí | REST | gRPC |
|---|---|---|
| Hiệu suất | JSON overhead mỗi request | Binary, nhanh hơn 5x |
| Type safety | Runtime error | **Compile-time error nếu sai type** |
| Streaming | Cần implement riêng | **FanOut native streaming** |
| Contract | Dễ bị "drift" | **.proto file là source of truth** |
| Retry/Deadline | Tự implement | **Polly tích hợp sẵn** |

### 5.2. Proto Contract — Ngôn ngữ chung

**File:** [MMS.Protos/Protos/Notification/v1/notification.proto](../MMS.Protos/Protos/Notification/v1/notification.proto)

```protobuf
syntax = "proto3";

package notification.v1;

import "google/protobuf/timestamp.proto";
import "Common/v1/common.proto";

// ① SERVICE định nghĩa: đây là "interface" của gRPC server
service NotificationService {
  rpc SendOne(SendNotificationRequest) returns (SendNotificationResponse);
  rpc FanOut(FanOutRequest) returns (stream DeliveryStatus);  // ② SERVER STREAMING
}

// ③ MESSAGE = data structure (thay JSON)
message SendNotificationRequest {
  string recipient_user_id = 1;  // field number (1, 2, 3...) — dùng khi encode binary
  string title = 2;
  string body = 3;
  string channel = 4;            // "in_app" | "email" | "push"
  map<string, string> metadata = 5;  // ④ Flexible key-value
}

message SendNotificationResponse {
  string notification_id = 1;
  bool success = 2;
  optional string error_message = 3;  // ⑤ optional = có thể null
}

message FanOutRequest {
  repeated string recipient_user_ids = 1;  // ⑥ repeated = array
  string title = 2;
  string body = 3;
  string channel = 4;
}

message DeliveryStatus {
  string recipient_user_id = 1;
  string notification_id = 2;
  bool delivered = 3;
  google.protobuf.Timestamp delivered_at = 4;  // ⑦ Well-known types
}
```

**Proto file sinh ra code C# tự động:**
```
MMS.Protos/Protos/Notification/v1/notification.proto
         ↓ dotnet build (protoc compiler)
NotificationService.NotificationServiceClient   ← gRPC Client (dùng ở API)
NotificationService.NotificationServiceBase     ← Abstract class (implement ở NotificationService)
SendNotificationRequest                          ← C# class
SendNotificationResponse                         ← C# class
FanOutRequest                                    ← C# class
DeliveryStatus                                   ← C# class
```

### 5.3. Audit Log Proto — Client Streaming

**File:** [MMS.Protos/Protos/AuditLog/v1/audit_log.proto](../MMS.Protos/Protos/AuditLog/v1/audit_log.proto)

```protobuf
service AuditLogService {
  rpc WriteStream(stream AuditEntry) returns (WriteAck);  // CLIENT STREAMING
  rpc Query(AuditQueryRequest) returns (AuditQueryResponse);
}

message AuditEntry {
  string actor_user_id = 1;
  string entity_name = 2;   // "Submission", "Series", "Task"...
  string entity_id = 3;
  string action = 4;        // "CREATE", "UPDATE", "TRANSITION"...
  optional string old_value_json = 5;
  optional string new_value_json = 6;
  string correlation_id = 7;
  google.protobuf.Timestamp occurred_at = 8;
  optional string ip_address = 9;
}
```

> **Client Streaming** → API gom nhiều audit entry thành 1 batch rồi stream sang AuditLogService một lần. Hiệu quả hơn gọi 100 lần Unary cho 100 entries.

### 5.4. gRPC Client — GrpcEmailService (Adapter Pattern)

**File:** [src/MMS.Infrastructure/Services/GrpcEmailService.cs](../src/MMS.Infrastructure/Services/GrpcEmailService.cs)

```csharp
// ① Implement interface ở Application layer — Business logic không biết gRPC tồn tại
public class GrpcEmailService : IEmailService
{
    private readonly NotificationService.NotificationServiceClient _client;
    private readonly ILogger<GrpcEmailService> _logger;

    // ② Các method nghiệp vụ rõ ràng
    public async Task SendApplicationApprovedAsync(
        string email, string fullName, string seriesTitle)
    {
        await SendViaGrpcAsync(
            email,
            subject: $"[MMS] Chúc mừng! Tác phẩm '{seriesTitle}' đã được duyệt",
            body: $"Kính gửi {fullName}, tác phẩm của bạn đã được Editorial Board phê duyệt..."
        );
    }

    public async Task SendSubmissionRejectedAsync(
        string email, string fullName, string reason)
    {
        await SendViaGrpcAsync(
            email,
            subject: "[MMS] Submission bị từ chối",
            body: $"Kính gửi {fullName}, submission của bạn bị từ chối. Lý do: {reason}"
        );
    }

    // ③ Private method: chuyển đổi sang gRPC call
    private async Task SendViaGrpcAsync(string email, string subject, string body)
    {
        try
        {
            await _client.SendOneAsync(new SendNotificationRequest
            {
                Title = subject,
                Body = body,
                Channel = "email",
                Metadata =
                {
                    // ④ "direct_email" → NotificationService biết dùng email này thay userId
                    ["direct_email"] = email
                }
            });
        }
        catch (RpcException ex)
        {
            // ⑤ Fallback: log warning + simulate (không crash nghiệp vụ chính)
            _logger.LogWarning(ex, "gRPC unavailable. Falling back to simulation.");
            Console.WriteLine($"[Email Simulation] To: {email}\nSubject: {subject}\n{body}");
        }
    }
}
```

**Tại sao dùng Adapter Pattern?**
```
Application Layer              Infrastructure Layer
┌──────────────────┐           ┌──────────────────────────┐
│  IEmailService   │◄──────── │  GrpcEmailService        │
│  (interface)     │           │  (gRPC implementation)   │
│                  │           └──────────────────────────┘
│  SubmissionService           ┌──────────────────────────┐
│  gọi: _email.Send()│◄──────│  SmtpEmailService        │
│  → không quan tâm│           │  (SMTP implementation)   │
│    gRPC hay SMTP  │           └──────────────────────────┘
└──────────────────┘
```
→ Dễ dàng **swap implementation** mà không đụng Business Logic

### 5.5. gRPC Server Streaming — FanOut

**File:** [src/MMS.Infrastructure/Services/NotificationGrpcClientWrapper.cs](../src/MMS.Infrastructure/Services/NotificationGrpcClientWrapper.cs)

```csharp
// Dùng khi cần gửi notification cho nhiều user cùng lúc
// (VD: Editorial Board vote → thông báo tất cả stakeholders)
public async Task FanOutEmailAsync(
    IEnumerable<string> userIds, string title, string body)
{
    var request = new FanOutRequest
    {
        Title = title,
        Body = body,
        Channel = "email"
    };
    request.RecipientUserIds.AddRange(userIds);

    // ① Gọi gRPC server streaming
    using var call = _client.FanOut(request);

    // ② await foreach → nhận từng DeliveryStatus khi server stream về
    await foreach (var status in call.ResponseStream.ReadAllAsync())
    {
        // ③ Log từng kết quả delivery real-time
        _logger.LogInformation(
            "Email delivery: userId={UserId}, delivered={Delivered}, time={Time}",
            status.RecipientUserId,
            status.Delivered,
            status.DeliveredAt);
    }
}
```

**Luồng FanOut hoạt động:**
```
API ──── FanOutRequest([user1,user2,user3], "Kết quả vote") ────► NotificationService
                                                                         │
                                                          ┌──────────────┤
                                                          │ Loop users:   │
                                                          │ user1 → SMTP  │
API ◄── DeliveryStatus(user1, delivered=true) ──────────│◄─── stream①   │
                                                          │ user2 → SMTP  │
API ◄── DeliveryStatus(user2, delivered=true) ──────────│◄─── stream②   │
                                                          │ user3 → SMTP  │
API ◄── DeliveryStatus(user3, delivered=false) ─────────│◄─── stream③   │
                                                          └───────────────┘
```

### 5.6. JWT Auth Interceptors

#### Client Interceptor (API → gọi service)

**File:** [src/MMS.Infrastructure/gRPC/GrpcAuthClientInterceptor.cs](../src/MMS.Infrastructure/gRPC/GrpcAuthClientInterceptor.cs)

```csharp
public class GrpcAuthClientInterceptor : Interceptor
{
    // Tự động thêm JWT vào MỌI gRPC call
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var headers = context.Options.Headers ?? new Metadata();
        headers.Add("authorization", $"Bearer {GetServiceToken()}");

        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host,
            context.Options.WithHeaders(headers));

        return continuation(request, newContext);
    }
}
```

#### Server Interceptor (NotificationService → validate)

**File:** [src/MMS.Infrastructure/gRPC/GrpcAuthServerInterceptor.cs](../src/MMS.Infrastructure/gRPC/GrpcAuthServerInterceptor.cs)

```csharp
public class GrpcAuthServerInterceptor : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ValidateToken(context);   // Throw RpcException(Unauthenticated) nếu JWT invalid
        return await continuation(request, context);
    }

    private void ValidateToken(ServerCallContext context)
    {
        var token = context.RequestHeaders
            .FirstOrDefault(h => h.Key == "authorization")
            ?.Value?.Replace("Bearer ", "");

        if (string.IsNullOrEmpty(token))
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Missing token"));

        // Validate JWT signature + expiry
        _tokenValidator.Validate(token);   // throws nếu invalid
    }
}
```

### 5.7. Polly Retry Policy — Tự động retry khi lỗi

**File:** [src/MMS.Infrastructure/gRPC/GrpcClientRegistrationExtensions.cs](../src/MMS.Infrastructure/gRPC/GrpcClientRegistrationExtensions.cs)

```csharp
services.AddGrpcClient<NotificationService.NotificationServiceClient>(o =>
{
    o.Address = new Uri(options.NotificationEndpoint);  // https://localhost:5102
})
.AddInterceptor<GrpcAuthClientInterceptor>()           // ① Thêm JWT
.AddTransientHttpErrorPolicy(p =>
    p.WaitAndRetryAsync(
        retryCount: 3,                                  // ② Retry tối đa 3 lần
        sleepDurationProvider: attempt =>
            TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))  // ③ Exponential backoff
        // attempt=1: chờ 400ms
        // attempt=2: chờ 800ms
        // attempt=3: chờ 1600ms
    )
);
```

**Exponential Backoff:**
```
Lần 1 thất bại → chờ 400ms → thử lại
Lần 2 thất bại → chờ 800ms → thử lại
Lần 3 thất bại → chờ 1600ms → thử lại
Lần 4 thất bại → throw RpcException → GrpcEmailService bắt → fallback simulation
```

### 5.8. NotificationService — gRPC Server độc lập

**File:** [MMS.NotificationService/Program.cs](../MMS.NotificationService/Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<GrpcAuthServerInterceptor>();  // ① Validate mọi call
});

// ② DB chỉ đọc để lookup email từ userId
builder.Services.AddDbContext<MmsDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();     // ③ Gửi email thật
builder.Services.AddScoped<IUserEmailLookup, DbUserEmailLookup>(); // ④ Lookup email từ DB

var app = builder.Build();
app.MapGrpcService<NotificationGrpcService>();  // ⑤ Bind service
app.Run("https://0.0.0.0:5102");               // ⑥ Port riêng, độc lập với API
```

**NotificationGrpcService.cs:**
```csharp
public class NotificationGrpcService : NotificationService.NotificationServiceBase
{
    // UNARY: Gửi 1 notification
    public override async Task<SendNotificationResponse> SendOne(
        SendNotificationRequest request, ServerCallContext context)
    {
        string? email;

        // ① Metadata trick: GrpcEmailService truyền email trực tiếp
        if (request.Metadata.TryGetValue("direct_email", out var directEmail))
        {
            email = directEmail;
        }
        else
        {
            // ② Truy DB lấy email từ userId
            email = await _userEmailLookup.GetEmailAsync(request.RecipientUserId);
        }

        var success = await _emailSender.SendAsync(email, "", request.Title, request.Body);

        return new SendNotificationResponse
        {
            NotificationId = Guid.NewGuid().ToString(),
            Success = success
        };
    }

    // SERVER STREAMING: Gửi cho nhiều user, stream kết quả về
    public override async Task FanOut(
        FanOutRequest request,
        IServerStreamWriter<DeliveryStatus> responseStream,
        ServerCallContext context)
    {
        foreach (var userId in request.RecipientUserIds)
        {
            context.CancellationToken.ThrowIfCancellationRequested();  // ③ Respect cancellation

            var email = await _userEmailLookup.GetEmailAsync(userId);
            var delivered = email != null
                && await _emailSender.SendAsync(email, "", request.Title, request.Body);

            // ④ Stream từng kết quả về client ngay khi xong (không đợi tất cả)
            await responseStream.WriteAsync(new DeliveryStatus
            {
                RecipientUserId = userId,
                NotificationId = Guid.NewGuid().ToString(),
                Delivered = delivered,
                DeliveredAt = Timestamp.FromDateTime(DateTime.UtcNow)
            });
        }
    }
}
```

### 5.9. Configuration

**appsettings.json:**
```json
{
  "Grpc": {
    "ServiceToken": "eyJ...",
    "ServiceTokenSecret": "min32chars_secret_key_production",
    "NotificationEndpoint": "https://localhost:5102",
    "AuditLogEndpoint": "https://localhost:5103",
    "MaxReceiveMessageSizeMb": 64,
    "RetryCount": 3,
    "DefaultDeadlineSeconds": 30
  }
}
```

---

## 6. GRAPHQL VS gRPC — KHI NÀO DÙNG CÁI NÀO?

### Ma trận quyết định

```
                    Client là Browser/Mobile?
                    ┌────YES────┬────NO────┐
                    │           │          │
              Cần realtime?  Server-to-    │
              ┌─YES─┬─NO─┐   server?      │
              │     │    │   ┌─YES─┬─NO─┐  │
           SignalR  │    │   │    │    │  │
                    │    │  gRPC  │  REST│
                Upload file? (internal)  │
                ┌─YES─┬─NO─┐              │
               REST  GraphQL
```

### Decision Tree đơn giản

| Câu hỏi | Nếu YES → Dùng |
|---|---|
| Browser/Mobile cần đọc/ghi dữ liệu nghiệp vụ? | **GraphQL** |
| Cần push realtime lên FE? | **SignalR** |
| Upload file/ảnh binary? | **REST multipart** |
| Backend service A gọi Backend service B? | **gRPC** |
| Cần streaming data (nhiều result)? | **gRPC streaming** |

### Ví dụ trong MMS

```
Mangaka upload bản thảo:
  1. Ảnh trang → REST POST /upload/page (binary, multipart)
  2. Tạo submission → GraphQL mutation createSubmission
  3. Thông báo Tantou có submission mới → gRPC SendOne(tantouId, "Có submission mới")

Tantou annotate:
  1. Thêm annotation → GraphQL mutation addAnnotation
  2. Push realtime cho Mangaka thấy annotation live → SignalR

Editorial Board vote:
  1. Cast vote → GraphQL mutation castVote
  2. Vote count update live → SignalR
  3. Khi đủ vote → gRPC FanOut([mangakaId, tantouId, ...], "Kết quả vote: APPROVED")
```

---

## 7. DEMO LUỒNG THỰC TẾ

### Demo 1: GraphQL Query — Mangaka xem danh sách series

```graphql
# FE gửi query này
query GetMySeries {
  getMySeries(mangakaId: "user-123") {
    id
    title
    status
    chapters {
      id
      chapterNumber
    }
  }
}
```

```
Bước 1: POST https://localhost:7001/graphql
        Body: { "query": "query GetMySeries { ... }" }

Bước 2: HotChocolate parse → validate:
        - Field "getMySeries" có trong schema? ✓
        - Field "chapters" trong SeriesResponse? ✓
        - Type "chapters.chapterNumber" là Int? ✓

Bước 3: Execute SeriesQueries.GetMySeries()
        → ISeriesService.GetByMangakaAsync("user-123")
        → SELECT * FROM Series WHERE MangakaId = 'user-123'

Bước 4: HotChocolate projection:
        - Entity có 20 field
        - Query chỉ cần: id, title, status, chapters.id, chapters.chapterNumber
        → Trả đúng 5 field

Bước 5: Response:
{
  "data": {
    "getMySeries": [
      {
        "id": "series-456",
        "title": "Dragon Quest",
        "status": "ACTIVE",
        "chapters": [
          { "id": "ch-1", "chapterNumber": 1 },
          { "id": "ch-2", "chapterNumber": 2 }
        ]
      }
    ]
  }
}
```

### Demo 2: gRPC — Submission được duyệt, gửi email

```
Bước 1: Editorial Board vote APPROVED
        GraphQL mutation: castVote(submissionId, vote: APPROVE)

Bước 2: SubmissionService.CastVoteAsync() xử lý:
        - Đủ 3/5 votes → Status = APPROVED
        - Gọi: _emailService.SendApplicationApprovedAsync(email, fullName, title)

Bước 3: GrpcEmailService.SendApplicationApprovedAsync():
        - Tạo SendNotificationRequest {
            title: "Chúc mừng! 'Dragon Quest' đã được duyệt",
            body: "Kính gửi Long, ...",
            channel: "email",
            metadata: { "direct_email": "long@example.com" }
          }

Bước 4: GrpcAuthClientInterceptor:
        - Thêm header: "authorization: Bearer eyJ..."

Bước 5: HTTP/2 Protobuf binary → NotificationService:5102
        ~20 bytes vs JSON ~200 bytes

Bước 6: GrpcAuthServerInterceptor validates JWT

Bước 7: NotificationGrpcService.SendOne():
        - Metadata["direct_email"] = "long@example.com"
        - SmtpEmailSender.SendAsync("long@example.com", subject, body)
        - MailKit → SMTP → Email gửi đến Mangaka

Bước 8: Return SendNotificationResponse { success: true }

Bước 9: GrpcEmailService nhận response → log success
```

### Demo 3: gRPC FanOut Streaming — Board vote xong, thông báo tất cả

```
FanOut([mangakaId, tantouId, assistantId], "Submission #123 đã được phê duyệt")
                │
                │ HTTP/2 Stream
                ▼
NotificationGrpcService.FanOut():
  Loop:
    user=mangakaId → lookup email → SMTP → stream DeliveryStatus(delivered=true)
    user=tantouId → lookup email → SMTP → stream DeliveryStatus(delivered=true)
    user=assistantId → email not found → stream DeliveryStatus(delivered=false)

API nhận stream theo thời gian thực:
  [300ms] Log: "mangaka: delivered=true"
  [520ms] Log: "tantou: delivered=true"
  [680ms] Log: "assistant: delivered=false (no email)"
```

---

## 8. LỢI ÍCH ĐẠT ĐƯỢC & BÀI HỌC

### 8.1. Lợi ích của GraphQL trong MMS

| Vấn đề trước đây | Giải pháp GraphQL |
|---|---|
| FE cần nhiều API calls để lấy đủ data | 1 query lấy mọi thứ |
| BE trả dư field, tốn bandwidth | FE chọn đúng field cần |
| Viết OpenAPI docs mất thời gian | Schema tự sinh, Banana Cake Pop |
| Phân quyền scattered khắp nơi | `[Authorize]` tập trung tại resolver |
| Error format không nhất quán | `GraphQLErrorFilter` chuẩn hóa |

**Số lượng API endpoints giảm từ ~40+ REST endpoints xuống còn 1 endpoint `/graphql`** với ~33 queries và ~30 mutations được tổ chức rõ ràng theo entity.

### 8.2. Lợi ích của gRPC trong MMS

| Vấn đề trước đây | Giải pháp gRPC |
|---|---|
| Email gửi trực tiếp trong API (blocking) | Offload sang NotificationService riêng biệt |
| JSON overhead cho internal calls | Protobuf binary 5x nhỏ hơn |
| Không có contract cho internal APIs | .proto file là source of truth |
| Service scale phải scale cả API | NotificationService scale độc lập |
| Gửi bulk email chậm (sequential) | FanOut streaming, không block |
| Retry logic phải tự viết | Polly tích hợp sẵn |

### 8.3. Tổng kết kiến trúc

```
✅ GraphQL: 1 endpoint thay 40+ REST endpoints
✅ GraphQL: Zero over-fetching (FE chọn field)
✅ GraphQL: Authorization declarative [Authorize]
✅ GraphQL: Error handling chuẩn hóa
✅ gRPC: NotificationService tách biệt, scale độc lập
✅ gRPC: Protobuf binary 5x nhỏ hơn JSON
✅ gRPC: Compile-time type safety qua .proto
✅ gRPC: Server streaming cho bulk operations
✅ gRPC: JWT interceptors tự động (không viết tay mỗi call)
✅ gRPC: Exponential backoff retry tự động
```

### 8.4. Những điểm cần cải thiện tiếp theo

```
⚠️ GraphQL:
   - DataLoader pattern (chưa có) → N+1 query khi fetch nested entities
   - GraphQL Subscriptions (chưa có) → hiện dùng SignalR riêng
   - Input type validation (chưa có) → đang validate ở service layer

⚠️ gRPC:
   - Deadline per call (chưa set) → call có thể hang mãi
   - AuditLogService chưa implement hoàn chỉnh
   - ImageProcessingService đã mark remove nhưng proto còn đó
```

---

## 9. Q&A — CÂU HỎI THƯỜNG GẶP

**Q: Tại sao không dùng REST cho mọi thứ?**
> REST đơn giản nhưng dẫn đến over-fetching, under-fetching, và N+1 problem khi FE cần data từ nhiều entity liên quan. GraphQL giải quyết cả 3 vấn đề này.

**Q: Tại sao không dùng GraphQL cho internal service communication?**
> Browser không thể gọi gRPC trực tiếp (cần gRPC-Web proxy). Nhưng cho server-to-server, gRPC nhanh hơn 5x, có native streaming, và contract `.proto` là "nguồn chân lý duy nhất" không thể sai lệch.

**Q: Làm sao biết request GraphQL của user có quyền hay không?**
> JWT được gửi kèm trong header `Authorization: Bearer {token}`. HotChocolate đọc claims từ JWT và check `[Authorize(Roles = ...)]` tại từng resolver trước khi execute.

**Q: Nếu NotificationService bị crash, API có crash theo không?**
> Không. `GrpcEmailService` có try-catch: nếu gRPC thất bại sau 3 lần retry, chỉ log warning và in simulation ra console. Nghiệp vụ chính (vote, submission state) không bị ảnh hưởng.

**Q: Tại sao FanOut dùng server streaming thay vì gửi N request riêng?**
> N request riêng = N round-trips + N connection setup. Server streaming = 1 connection, server push kết quả liên tục. Với 100 user thì streaming nhanh hơn đáng kể.

**Q: GraphQL có bị lỗi N+1 không?**
> Có, nếu không dùng DataLoader. VD: query 100 series mà mỗi series fetch chapters riêng = 101 SQL queries. Hiện tại MMS chưa implement DataLoader — đây là điểm cần cải thiện.

**Q: Làm sao debug gRPC? Không có JSON để đọc.**
> Dùng gRPC health check endpoint + structured logging. Trong development, enable gRPC reflection để dùng tool như `grpcurl` hoặc Postman để inspect.

---

## PHỤ LỤC: File & Thư mục Quan trọng

### GraphQL
| File | Mô tả |
|---|---|
| [src/MMS.API/DependencyInjection.cs](../src/MMS.API/DependencyInjection.cs) | Đăng ký toàn bộ GraphQL |
| [src/MMS.API/GraphQL/Queries/](../src/MMS.API/GraphQL/Queries/) | 10 Query classes |
| [src/MMS.API/GraphQL/Mutations/](../src/MMS.API/GraphQL/Mutations/) | 10 Mutation classes |
| [src/MMS.API/GraphQL/GraphQLErrorFilter.cs](../src/MMS.API/GraphQL/GraphQLErrorFilter.cs) | Error handling |
| [src/MMS.API/GraphQL/GraphQLHelper.cs](../src/MMS.API/GraphQL/GraphQLHelper.cs) | JWT helper |

### gRPC
| File | Mô tả |
|---|---|
| [MMS.Protos/Protos/](../MMS.Protos/Protos/) | Toàn bộ .proto contracts |
| [MMS.Protos/Protos/Notification/v1/notification.proto](../MMS.Protos/Protos/Notification/v1/notification.proto) | Notification service contract |
| [MMS.Protos/Protos/AuditLog/v1/audit_log.proto](../MMS.Protos/Protos/AuditLog/v1/audit_log.proto) | Audit log contract |
| [src/MMS.Infrastructure/gRPC/GrpcAuthClientInterceptor.cs](../src/MMS.Infrastructure/gRPC/GrpcAuthClientInterceptor.cs) | Client JWT interceptor |
| [src/MMS.Infrastructure/gRPC/GrpcAuthServerInterceptor.cs](../src/MMS.Infrastructure/gRPC/GrpcAuthServerInterceptor.cs) | Server JWT interceptor |
| [src/MMS.Infrastructure/gRPC/GrpcClientRegistrationExtensions.cs](../src/MMS.Infrastructure/gRPC/GrpcClientRegistrationExtensions.cs) | DI registration + Polly |
| [src/MMS.Infrastructure/Services/GrpcEmailService.cs](../src/MMS.Infrastructure/Services/GrpcEmailService.cs) | Email adapter (gRPC) |
| [src/MMS.Infrastructure/Services/NotificationGrpcClientWrapper.cs](../src/MMS.Infrastructure/Services/NotificationGrpcClientWrapper.cs) | FanOut client |
| [MMS.NotificationService/Services/NotificationGrpcService.cs](../MMS.NotificationService/Services/NotificationGrpcService.cs) | gRPC server impl |
| [MMS.NotificationService/Program.cs](../MMS.NotificationService/Program.cs) | NotificationService startup |

---

*Tài liệu này tóm tắt toàn bộ thiết kế GraphQL & gRPC trong MMS. Tham khảo thêm: [graphql-grpc-huong-dan-chi-tiet.md](./graphql-grpc-huong-dan-chi-tiet.md) và [signalr-grpc-email-architecture.md](./signalr-grpc-email-architecture.md).*
