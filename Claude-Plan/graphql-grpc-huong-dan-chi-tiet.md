# HƯỚNG DẪN CHI TIẾT GRAPHQL & GRPC TRONG DỰ ÁN MMS

> **Tài liệu kèm theo:** [lo-trinh-trien-khai-chi-tiet.md](./lo-trinh-trien-khai-chi-tiet.md)
> **Mục đích:** Giải thích **từ con số 0** cho người lần đầu tiếp xúc GraphQL & gRPC — vì sao dự án dùng chúng, chúng vận hành ra sao trong codebase MMS hiện tại, cấu trúc thư mục đã setup đã tốt chưa, và lộ trình mở rộng đúng cách.
> **Đối tượng đọc:** Dev backend MMS chưa quen GraphQL/gRPC, cần "bản đồ tư duy" trước khi đụng code Phase 4+.
> **Ngày lập:** 2026-05-21 · **Phiên bản:** 1.0

---

## MỤC LỤC

1. [Tóm tắt 60 giây — Vì sao MMS có cả 4 giao thức](#1-tóm-tắt-60-giây)
2. [Phần A — GraphQL từ A đến Z](#phần-a--graphql-từ-a-đến-z)
   - A.1. GraphQL là gì? So sánh với REST
   - A.2. Vòng đời 1 request GraphQL trong MMS
   - A.3. Cấu trúc thư mục `MMS.API/GraphQL/` hiện tại — bóc tách
   - A.4. Anatomy 1 Query, 1 Mutation, 1 Subscription
   - A.5. Authorization, Error filter, DataLoader
   - A.6. Cách thêm 1 feature GraphQL mới (workflow 7 bước)
   - A.7. Đánh giá cấu trúc GraphQL hiện tại
3. [Phần B — gRPC từ A đến Z](#phần-b--grpc-từ-a-đến-z)
   - B.1. gRPC là gì? So sánh với REST và GraphQL
   - B.2. 4 kiểu RPC: Unary, Server-Stream, Client-Stream, Bidirectional
   - B.3. Vòng đời 1 gRPC call trong kiến trúc MMS
   - B.4. Cấu trúc project `MMS.Protos/` hiện tại — bóc tách
   - B.5. Cấu trúc thư mục `MMS.Infrastructure/gRPC/` — bóc tách
   - B.6. Anatomy 1 file `.proto` + code generated
   - B.7. Auth interceptor, retry policy, deadline
   - B.8. Cách thêm 1 gRPC service mới (workflow 8 bước)
   - B.9. Đánh giá cấu trúc gRPC hiện tại
4. [Phần C — Bản đồ giao thức theo Phase](#phần-c--bản-đồ-giao-thức-theo-phase)
5. [Phần D — Khi nào dùng GraphQL, khi nào dùng gRPC, khi nào dùng REST/SignalR](#phần-d--quy-tắc-chọn-giao-thức)
6. [Phần E — Anti-patterns thường gặp khi mới học](#phần-e--anti-patterns)
7. [Phần F — Checklist dev hằng ngày](#phần-f--checklist-dev-hằng-ngày)
8. [Phần G — Tài liệu tham khảo & lệnh CLI hữu ích](#phần-g--tài-liệu-tham-khảo)

---

## 1. Tóm tắt 60 giây

MMS là **hệ thống quản lý xuất bản manga đa vai trò** (Mangaka, Tantou, Editorial Board, Assistant). Vì là nghiệp vụ phức tạp + nhiều luồng realtime, ta dùng **4 giao thức song song**, mỗi cái cho 1 mục đích:

| Giao thức | Dùng cho | Client | Server |
|---|---|---|---|
| **GraphQL** | Mọi thao tác CRUD/Query nghiệp vụ từ FE (web/mobile) → BE. FE chọn field nào cần, BE không over-fetch. | Apollo Client (FE) | HotChocolate (BE) |
| **gRPC** | Server-to-server nội bộ: API gọi NotificationService, AuditLogService. Performance cao + contract chặt (proto). | API (gRPC client) | Các service worker (gRPC server) |
| **SignalR** | Realtime push từ BE → FE: ai vừa annotate, vote count update live. | Browser WebSocket | API hub |
| **REST** | Upload file binary (manga page → Cloudinary). GraphQL không hợp binary. | Browser `<input type="file">` | API multipart endpoint |

> 💡 **Quy tắc vàng:**
> - Browser/mobile **KHÔNG BAO GIỜ** gọi gRPC trực tiếp.
> - gRPC **CHỈ** dùng giữa các service backend nội bộ.
> - Binary lớn (ảnh, file) **KHÔNG** nhồi vào GraphQL — dùng REST multipart.

---

## Phần A — GraphQL từ A đến Z

### A.1. GraphQL là gì? So sánh với REST

**GraphQL** là **query language** cho API + **runtime** để thực thi query đó. Khác với REST:

| Khía cạnh | REST | GraphQL |
|---|---|---|
| Endpoint | Nhiều endpoint (`/users`, `/users/123/posts`, ...) | **1 endpoint duy nhất** (`/graphql`) |
| Format request | URL + method (GET/POST/...) | JSON `{ query, variables }` qua POST |
| Format response | BE tự quyết trả field nào | **FE chọn field cần** (over-fetching = 0) |
| Versioning | `/api/v1/`, `/api/v2/` | Schema evolve qua deprecation, không version URL |
| N+1 problem | Manual join | DataLoader pattern (batch tự động) |
| Realtime | Cần SSE/WebSocket riêng | **Subscription** built-in qua WebSocket |
| Schema discovery | OpenAPI (manual) | Introspection — tự sinh từ code |

**Ví dụ thực tế trong MMS:**

REST cách cũ (giả định):
```
GET /api/submissions/abc-123
→ trả về toàn bộ field của Submission: id, status, manuscriptId, tantouId, comments[], history[], votes[], ...
```
→ FE chỉ cần `id + status` để render list, nhưng BE trả 30 field. **Over-fetch.**

GraphQL:
```graphql
query {
  submissionById(id: "abc-123") {
    id
    status
  }
}
```
→ Server CHỈ tính + trả 2 field này. Khi cần thêm, FE thêm field, BE không đổi.

> ⚠️ Đừng nhầm: GraphQL **không phải database query** — nó query **schema** (mà schema do BE define + map vào service/DB).

---

### A.2. Vòng đời 1 request GraphQL trong MMS

```
┌──────────┐  POST /graphql                        ┌──────────────────────┐
│ FE       │  body: { query: "...", variables }   │ MMS.API              │
│ (Apollo) │─────────────────────────────────────▶│                      │
└──────────┘                                       │  1. HTTP middleware  │
                                                   │     (JWT auth)       │
                                                   │  2. HotChocolate     │
                                                   │     - parse query    │
                                                   │     - validate schema│
                                                   │     - check @Authorize│
                                                   │  3. Resolver chạy    │
                                                   │     → call Service   │
                                                   │  4. Service          │
                                                   │     → Repository     │
                                                   │     → EF Core → DB   │
                                                   │  5. Trả về JSON      │
                                                   │     CHỈ field FE hỏi │
                                                   └─────────┬────────────┘
                                                             │
┌──────────┐  JSON response                                 │
│ FE       │◀────────────────────────────────────────────────┘
└──────────┘
```

**Các thành phần trong MMS đang dùng:**

- **HotChocolate** (NuGet `HotChocolate.AspNetCore`) — GraphQL server lib chính thức cho .NET. Đã cài sẵn ở `MMS.API.csproj`.
- **Resolver** = method C# được HotChocolate gọi khi field xuất hiện trong query. Mỗi field 1 resolver (hoặc derive từ POCO).
- **[Service]** attribute — dependency injection inline trong tham số resolver. Đây là cách HotChocolate inject scoped service mà không cần constructor.

---

### A.3. Cấu trúc thư mục `MMS.API/GraphQL/` hiện tại — bóc tách

Cấu trúc hiện tại của bạn:

```
src/MMS.API/GraphQL/
├── GraphQLHelper.cs              ← Extension method DI cho HotChocolate
├── GraphQLErrorFilter.cs         ← Bắt exception và format error
├── Mutations/                    ← Mọi thao tác THAY ĐỔI state
│   ├── AnnotationMutations.cs
│   ├── AuthMutations.cs
│   ├── ChapterMutations.cs
│   ├── PageMutations.cs
│   ├── RegionMutations.cs
│   ├── SeriesMutations.cs
│   ├── SubmissionMutations.cs
│   ├── TaskMutations.cs
│   └── UserMutations.cs
└── Queries/                      ← Mọi thao tác CHỈ ĐỌC
    ├── AnnotationQueries.cs
    ├── ChapterQueries.cs
    ├── PageQueries.cs
    ├── RegionQueries.cs
    ├── SeriesQueries.cs
    ├── SubmissionQueries.cs
    ├── TaskQueries.cs
    └── UserQueries.cs
```

**Phân tích từng phần:**

#### A.3.1. `Queries/` — đọc dữ liệu

Mỗi domain entity (User, Series, Chapter, ...) có 1 file `*Queries.cs`. Mục đích: **gom các method query liên quan 1 entity vào 1 chỗ** → dễ tìm, dễ review.

HotChocolate cần đăng ký 1 **root Query type** duy nhất. Có 2 cách quản lý nhiều Query class:

1. **Cách bạn đang dùng — `[ExtendObjectType(typeof(UserQueries))]`:**
   - `UserQueries` là root.
   - `SubmissionQueries`, `SeriesQueries`, ... gắn `[ExtendObjectType(typeof(UserQueries))]` → các method trong đó được "merge" vào root.
   - Ưu: 1 namespace global, FE thấy như tất cả query nằm cùng cấp.
   - Nhược: phải nhớ extend đúng root, dễ quên.

2. **Cách "Type Extension" với root class trống** (alternative):
   ```csharp
   public class Query { } // empty root
   public class UserQueries { /* methods */ }
   // Đăng ký: .AddQueryType<Query>().AddTypeExtension<UserQueries>()
   ```

→ **Đánh giá:** cách hiện tại OK cho MVP. Nếu sau này có >15 file Queries, nên chuyển sang cách 2 vì rõ ràng hơn.

#### A.3.2. `Mutations/` — thay đổi state

Tương tự Queries. Mỗi entity 1 file. Mutation **luôn return entity sau khi thay đổi** (để FE update cache không phải refetch).

> 💡 Quy ước đặt tên mutation (industry-wide):
> - `createSubmission`, `updateSubmission`, `deleteSubmission` — CRUD chuẩn.
> - `submitManuscript`, `forwardToBoard`, `castVote` — **action-style** cho luồng workflow phức tạp (recommended cho MMS, vì chuyển trạng thái có nghĩa hơn "update").

#### A.3.3. `GraphQLHelper.cs` — DI registration

Đây là nơi bạn đăng ký tất cả Query/Mutation/Subscription với HotChocolate ở `Program.cs`. Ví dụ điển hình:

```csharp
public static IServiceCollection AddMmsGraphQL(this IServiceCollection services)
{
    services
        .AddGraphQLServer()
        .AddQueryType<UserQueries>()                      // root
        .AddTypeExtension<SubmissionQueries>()            // extend root
        .AddTypeExtension<SeriesQueries>()
        // ... mutations
        .AddMutationType<UserMutations>()
        .AddTypeExtension<SubmissionMutations>()
        // ...
        .AddAuthorization()                                // policy-based
        .AddErrorFilter<GraphQLErrorFilter>()              // catch exception
        .AddProjections().AddFiltering().AddSorting();     // OData-like
    return services;
}
```

#### A.3.4. `GraphQLErrorFilter.cs` — chuẩn hoá error

Lý do tồn tại file này: **GraphQL exception KHÔNG đi qua `ExceptionMiddleware` của ASP.NET Core**. Phải có filter riêng để catch + format `{ errorCode, message, traceId }` cho client. Tham chiếu Phase 0.3 trong lộ trình.

> ⚠️ Khi thêm exception type mới ở `MMS.Domain/Exceptions/`, nhớ extend `GraphQLErrorFilter` để map. Nếu không, FE sẽ thấy `Internal Server Error` chung chung.

---

### A.4. Anatomy 1 Query, 1 Mutation, 1 Subscription

#### A.4.1. Query mẫu (đã có trong code)

`SubmissionQueries.cs`:
```csharp
[ExtendObjectType(typeof(UserQueries))]
public class SubmissionQueries
{
    /// <summary>Xem danh sách submission của một Mangaka</summary>
    public async Task<IEnumerable<SubmissionResponse>> GetMySubmissions(
        [Service] ISubmissionService submissionService,
        Guid mangakaId)
    {
        return await submissionService.GetByMangakaAsync(mangakaId);
    }
}
```

**Bóc tách:**
- `[ExtendObjectType(typeof(UserQueries))]` — gắn vào root Query.
- `[Service] ISubmissionService` — inject service vào tham số (KHÔNG dùng constructor).
- `Guid mangakaId` — tham số nhận từ GraphQL variable.
- HotChocolate auto-convert tên: `GetMySubmissions` → field GraphQL `mySubmissions`.

FE query:
```graphql
query {
  mySubmissions(mangakaId: "abc-123") {
    id
    status
    submittedAt
  }
}
```

#### A.4.2. Mutation mẫu (template cho Phase 5)

```csharp
[ExtendObjectType(typeof(UserMutations))]
public class SubmissionMutations
{
    [Authorize(Policy = "CanSubmitManuscript")]  // policy từ Phase 1
    public async Task<SubmissionResponse> SubmitManuscript(
        [Service] ISubmissionService service,
        [GlobalState("UserId")] Guid userId,
        SubmitManuscriptInput input)
    {
        var result = await service.SubmitForReviewAsync(input, userId);
        if (result.IsFailure)
            throw new DomainException(result.ErrorCode!, result.Error!);
        return result.Value!;
    }
}

public record SubmitManuscriptInput(Guid ManuscriptId, string? CoverNote);
```

**Lưu ý quan trọng:**
- `[Authorize(Policy = ...)]` — chỉ user qualify policy mới gọi được. **KHÔNG check role trong service.**
- `[GlobalState("UserId")]` — lấy current user từ context (set sẵn ở middleware JWT).
- Return luôn `SubmissionResponse` (DTO) — KHÔNG trả entity EF Core trực tiếp (leak internal).
- Throw domain exception → `GraphQLErrorFilter` catch + map.

#### A.4.3. Subscription mẫu (Phase 8)

```csharp
public class AnnotationSubscriptions
{
    [Subscribe]
    [Topic("annotation_chapter_{chapterId}")]
    public AnnotationResponse OnAnnotationAdded(
        Guid chapterId,
        [EventMessage] AnnotationResponse annotation) => annotation;
}
```

FE subscribe:
```graphql
subscription {
  onAnnotationAdded(chapterId: "...") { id content authorName }
}
```

→ Khi service publish event qua `ITopicEventSender.SendAsync("annotation_chapter_xyz", annot)`, mọi FE đang subscribe sẽ nhận push.

---

### A.5. Authorization, Error filter, DataLoader

#### A.5.1. Authorization (Phase 1)

3 cấp:

1. **Schema-level** — `[Authorize]` trên class hoặc method → user phải authenticated.
2. **Role-level** — `[Authorize(Roles = "Tantou")]`.
3. **Policy-level** — `[Authorize(Policy = "CanReviewSubmission")]` — quan trọng nhất, check **quan hệ user ↔ resource**.

> 🔥 **Anti-pattern:** Đừng viết `if (user.Role != "Tantou") return Result.Failure(...)` trong service. Đẩy lên policy.

#### A.5.2. Error Filter

Đã có `GraphQLErrorFilter.cs`. Mở rộng để map exception → error code:

```csharp
public class GraphQLErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        if (error.Exception is DomainException de)
            return error.WithMessage(de.Message)
                        .WithCode(de.ErrorCode)
                        .RemoveException();   // không leak stack trace
        // ...
        return error;
    }
}
```

#### A.5.3. DataLoader (Phase 5, 6, 12)

Vấn đề **N+1**: query 100 submission, mỗi submission lookup tantou → 1 + 100 query DB.

Giải: DataLoader **batch** các id trong 1 tick → 1 query:

```csharp
public class UserByIdDataLoader : BatchDataLoader<Guid, User>
{
    private readonly IUserRepository _repo;
    public UserByIdDataLoader(IUserRepository repo, IBatchScheduler s) 
        : base(s) { _repo = repo; }

    protected override async Task<IReadOnlyDictionary<Guid, User>> LoadBatchAsync(
        IReadOnlyList<Guid> keys, CancellationToken ct)
        => await _repo.GetByIdsAsync(keys).ToDictionaryAsync(u => u.Id);
}
```

Resolver dùng:
```csharp
public Task<User> GetTantou(
    [Parent] SubmissionResponse sub,
    UserByIdDataLoader loader)
    => loader.LoadAsync(sub.TantouId);
```

→ 100 submission cùng gọi `loader.LoadAsync(id)` → HotChocolate gom thành 1 batch → 1 query.

> ⚠️ **Bắt buộc dùng DataLoader** cho mọi field cross-entity ở Phase 12 (dashboard) — không có sẽ chết DB.

---

### A.6. Cách thêm 1 feature GraphQL mới (workflow 7 bước)

Ví dụ: thêm mutation `forwardToBoard` (Phase 5).

1. **Định nghĩa DTO** ở `MMS.Application/DTOs/Submission/` — input + response.
2. **Thêm method ở Service interface** `ISubmissionService.ForwardToBoardAsync(...)`.
3. **Implement service** ở `MMS.Application/Services/` — gọi repo, state machine, return `Result<T>`.
4. **Thêm policy** ở `MMS.Infrastructure/Authorization/` nếu chưa có (`CanForwardToBoard`).
5. **Đăng ký policy** trong `Program.cs` qua `AddAuthorization(o => o.AddPolicy(...))`.
6. **Thêm resolver** ở `SubmissionMutations.cs`:
   ```csharp
   [Authorize(Policy = "CanForwardToBoard")]
   public async Task<SubmissionResponse> ForwardToBoard(
       [Service] ISubmissionService svc,
       [GlobalState("UserId")] Guid userId,
       ForwardToBoardInput input)
   {
       var r = await svc.ForwardToBoardAsync(input.SubmissionId, userId, input.Comment);
       return r.IsSuccess ? r.Value! : throw new DomainException(r.ErrorCode!, r.Error!);
   }
   ```
7. **Test qua Banana Cake Pop** (UI có sẵn ở `/graphql`) — gửi mutation thử, check response + DB.

---

### A.7. Đánh giá cấu trúc GraphQL hiện tại

| Tiêu chí | Đánh giá | Ghi chú |
|---|---|---|
| Tách Queries vs Mutations | ✅ Tốt | Đúng convention industry. |
| Mỗi entity 1 file | ✅ Tốt | Dễ navigate. |
| Có `GraphQLErrorFilter` | ✅ Tốt | Sẵn sàng map exception. |
| Có `GraphQLHelper` extension | ✅ Tốt | DI gọn ở Program.cs. |
| Thiếu `Subscriptions/` folder | ⚠️ Chưa có | Tạo khi vào Phase 8. |
| Thiếu `DataLoaders/` folder | ⚠️ Chưa có | Tạo khi vào Phase 5 (nếu query bắt đầu N+1). |
| Thiếu `Inputs/` folder | ⚠️ Chưa có | Input record type nên gom 1 chỗ. Khuyến nghị tạo. |
| Có `[Authorize]` trên resolver | ❓ Chưa rõ | Phải verify khi vào Phase 1. |

**Đề xuất cấu trúc target sau Phase 1+8:**

```
src/MMS.API/GraphQL/
├── GraphQLHelper.cs
├── GraphQLErrorFilter.cs
├── Queries/
│   └── *.cs
├── Mutations/
│   └── *.cs
├── Subscriptions/                  ← thêm ở Phase 8
│   ├── AnnotationSubscriptions.cs
│   ├── BoardVotingSubscriptions.cs
│   └── NotificationSubscriptions.cs
├── Inputs/                         ← thêm khi có nhiều input record
│   ├── SubmitManuscriptInput.cs
│   └── ...
├── DataLoaders/                    ← thêm khi gặp N+1
│   ├── UserByIdDataLoader.cs
│   └── SeriesByIdDataLoader.cs
└── Types/                          ← optional, custom ObjectType khi cần
    └── SubmissionType.cs
```

**Kết luận GraphQL phần A:** cấu trúc hiện tại **đủ tốt cho MVP Phase 0-4**. Khi vào Phase 5+ cần bổ sung 3 folder: `Subscriptions`, `Inputs`, `DataLoaders`. Không cần refactor lớn.

---

## Phần B — gRPC từ A đến Z

### B.1. gRPC là gì? So sánh với REST và GraphQL

**gRPC** = **g**oogle **R**emote **P**rocedure **C**all. Tinh thần: **gọi function trên server từ xa như gọi function local**. Không nghĩ theo URL/resource, mà nghĩ theo **method name + arguments**.

**Đặc trưng:**
1. **Contract-first**: định nghĩa service + message trong file `.proto` → tool sinh code C# (cả client lẫn server stub) tự động.
2. **HTTP/2**: multiplexing nhiều call cùng 1 TCP connection, header compression.
3. **Protobuf binary**: nhỏ hơn JSON ~5-10 lần, parse nhanh hơn.
4. **Streaming**: 4 kiểu (xem B.2).
5. **Strongly-typed**: lỗi typo phát hiện tại compile time, không runtime.

| Khía cạnh | REST | GraphQL | gRPC |
|---|---|---|---|
| Format wire | JSON text | JSON text | Protobuf binary |
| Schema | OpenAPI (manual) | SDL (auto) | `.proto` (manual) |
| Code gen | Optional | Optional | **Bắt buộc** (Grpc.Tools) |
| Browser-friendly | ✅ | ✅ | ❌ (cần gRPC-Web proxy) |
| Streaming | SSE/WebSocket | Subscription | **Native 4 modes** |
| Performance | Trung bình | Trung bình | **Cao** |
| Phù hợp | Public API | Client app fetch data | **Service-to-service nội bộ** |

**Vì sao MMS dùng gRPC chỉ cho server-to-server?**
- Browser không hiểu Protobuf binary native → cần gRPC-Web (thêm phức tạp).
- Service-to-service: throughput cao quan trọng (notification fanout 1000 user, audit batch 100 event/sec).

---

### B.2. 4 kiểu RPC

| Kiểu | Mô tả | Use case trong MMS |
|---|---|---|
| **Unary** | 1 request → 1 response (giống REST POST) | `SendOne(SendNotificationRequest) → DeliveryStatus` |
| **Server-Stream** | 1 request → N response (server đẩy nhiều phần) | `FanOut(FanOutRequest) → stream DeliveryStatus` (notify 100 user, stream từng status về) |
| **Client-Stream** | N request → 1 response (client gửi nhiều phần, server ack cuối) | `WriteStream(stream AuditEvent) → WriteAck` (API stream audit event lên service) |
| **Bidirectional** | N request ↔ N response (real-time chat) | `SuggestAnnotation(stream Region) ↔ stream Suggestion` (Phase 13.3 AI) |

**Diagram trực quan:**

```
Unary:           Client ──REQ──▶ Server
                 Client ◀──RES── Server

Server-Stream:   Client ──REQ──▶ Server
                 Client ◀──RES1── Server
                 Client ◀──RES2── Server
                 Client ◀──RES3── Server

Client-Stream:   Client ──REQ1──▶ Server
                 Client ──REQ2──▶ Server
                 Client ──REQ3──▶ Server
                 Client ◀──ACK── Server

Bidirectional:   Client ──REQ1──▶ Server
                 Client ◀──RES1── Server
                 Client ──REQ2──▶ Server
                 Client ◀──RES2── Server
```

---

### B.3. Vòng đời 1 gRPC call trong kiến trúc MMS

Ví dụ: API muốn gửi notification cho 50 board member khi submission được forward.

```
┌──────────────────┐                 ┌────────────────────────┐
│ MMS.API          │                 │ MMS.NotificationService│
│ (gRPC client)    │                 │ (gRPC server)          │
│                  │                 │                        │
│ 1. SubmissionSvc │                 │                        │
│    raise event   │                 │                        │
│ 2. EventHandler  │                 │                        │
│ 3. INotification │  HTTP/2         │                        │
│    Client.FanOut │──gRPC unary────▶│ 4. NotificationGrpcSvc │
│    (50 user_ids) │                 │    .FanOut() handler   │
│                  │                 │ 5. Render template     │
│                  │                 │ 6. Channels.Send(email │
│                  │                 │    + in_app push)      │
│                  │  Stream of      │                        │
│ 9. UI gets       │◀─DeliveryStatus─│ 7. yield return status │
│    realtime push │  (50 messages)  │    per user            │
│    via SignalR   │                 │ 8. Completed           │
└──────────────────┘                 └────────────────────────┘
```

**Trong code MMS:**
- Bước 3: `INotificationClient` ở `MMS.Application/Interfaces/`, impl `GrpcNotificationClient` ở `MMS.Infrastructure/gRPC/Clients/`.
- Bước 4: `NotificationService` (project riêng) — chưa tạo, sẽ tạo ở Phase 9.

---

### B.4. Cấu trúc project `MMS.Protos/` hiện tại — bóc tách

Hiện tại bạn có:

```
MMS.Protos/
├── MMS.Protos.csproj
└── Protos/
    ├── Common/v1/common.proto                  ← shared message (Timestamp wrap, UserRef, ...)
    ├── ImageProcessing/v1/image_processing.proto  ← (sẽ DROP — Phase 3 chuyển Cloudinary)
    ├── Notification/v1/notification.proto       ← Phase 9
    └── AuditLog/v1/audit_log.proto              ← Phase 10
```

**Đánh giá:**

✅ **Tốt:**
- Folder convention `{Service}/v{N}/` — đúng best practice (Phase 2.2 trong lộ trình).
- Tách project riêng (`MMS.Protos`) — đúng. Cả API lẫn các Service đều reference project này → không lệch contract.
- Đặt namespace C#: `MMS.Protos.Notification.V1` qua `option csharp_namespace`.

⚠️ **Cần dọn:**
- `image_processing.proto` đã được lộ trình quyết định **bỏ** (Phase 3 dùng Cloudinary REST). File này còn nằm trong project + đang được reference ở `GrpcClientRegistrationExtensions.cs` line 3: `using MMS.Protos.ImageProcessing.V1;` và line 19: `services.AddGrpcClient<ImageProcessor.ImageProcessorClient>(...)`.
  - **Action:** Xoá `Protos/ImageProcessing/` + remove dòng đăng ký client. Hoặc giữ file nhưng comment dòng đăng ký nếu muốn để tham khảo.

✅ **Cấu hình `MMS.Protos.csproj` đúng:**
Khi mở file, bạn sẽ thấy entries kiểu:
```xml
<ItemGroup>
  <Protobuf Include="Protos\**\*.proto" GrpcServices="Both" />
</ItemGroup>
<ItemGroup>
  <PackageReference Include="Grpc.Tools" Version="..." PrivateAssets="All" />
  <PackageReference Include="Google.Protobuf" Version="..." />
  <PackageReference Include="Grpc.Net.ClientFactory" Version="..." />  
</ItemGroup>
```

- `GrpcServices="Both"` — sinh **cả** client stub (cho API) **và** server abstract class (cho NotificationService project).
- `Grpc.Tools` **PrivateAssets="All"** — tool build-time, không bị transitive vào project consume.
- ⚠️ KHÔNG add `Grpc.AspNetCore` vào `MMS.Protos.csproj` — chỉ là class lib pure, server-side dependency để service project tự add.

---

### B.5. Cấu trúc thư mục `MMS.Infrastructure/gRPC/` — bóc tách

```
src/MMS.Infrastructure/gRPC/
├── GrpcClientOptions.cs              ← appsettings binding: endpoint, retry count, max msg size
├── GrpcClientRegistrationExtensions.cs ← DI: AddMmsGrpcClients()
├── GrpcAuthClientInterceptor.cs      ← inject JWT vào metadata trước mỗi call
└── GrpcAuthServerInterceptor.cs      ← verify JWT từ metadata khi service nhận call
```

**Đánh giá:**

✅ **Tốt:**
- Đặt interceptor ở Infrastructure layer — đúng (chỉ liên quan transport).
- Tách `Options` (POCO bind từ config) khỏi `RegistrationExtensions` (logic DI).
- Có cả Client + Server interceptor — sẵn sàng cho Phase 1.4.

⚠️ **Hiện tại:**
- File `GrpcClientRegistrationExtensions.cs` chỉ đăng ký 1 client `ImageProcessor` (sắp xoá). Phase 9 + 10 sẽ thêm `NotificationClient` và `AuditLogClient`.
- File `GrpcAuthServerInterceptor.cs` nằm ở `MMS.Infrastructure` — sẽ được reuse khi tạo project `MMS.NotificationService` và `MMS.AuditLogService`. Tốt.

**Đề xuất bổ sung khi vào Phase 9/10:**

```
src/MMS.Infrastructure/gRPC/
├── GrpcClientOptions.cs
├── GrpcClientRegistrationExtensions.cs
├── Interceptors/
│   ├── GrpcAuthClientInterceptor.cs
│   ├── GrpcAuthServerInterceptor.cs
│   └── GrpcLoggingInterceptor.cs   ← thêm: log latency/correlation
├── Clients/                          ← wrapper layer, không gọi stub trực tiếp
│   ├── INotificationClient.cs       ← (move sang Application/Interfaces)
│   ├── GrpcNotificationClient.cs
│   ├── IAuditLogClient.cs
│   └── GrpcAuditLogClient.cs
└── Resilience/
    └── GrpcRetryPolicies.cs         ← Polly policy reuse
```

Đặt **`I*Client` interface ở `MMS.Application/Interfaces/Grpc/`** — Application không phụ thuộc Infrastructure. Chỉ impl `Grpc*Client` ở Infrastructure.

---

### B.6. Anatomy 1 file `.proto` + code generated

File `notification.proto` hiện tại của bạn:

```protobuf
syntax = "proto3";
package mms.notification.v1;
option csharp_namespace = "MMS.Protos.Notification.V1";

import "google/protobuf/timestamp.proto";

service NotificationService {
  rpc SendOne(SendNotificationRequest) returns (SendNotificationResponse);
  rpc FanOut(FanOutRequest) returns (stream DeliveryStatus);
}

message SendNotificationRequest {
  string recipient_user_id = 1;
  string title = 2;
  string body = 3;
  string channel = 4;
  map<string, string> metadata = 5;
}
// ... DeliveryStatus, FanOutRequest, ...
```

**Bóc tách từng dòng:**

| Dòng | Ý nghĩa |
|---|---|
| `syntax = "proto3";` | Phiên bản — luôn dùng proto3 (proto2 lỗi thời). |
| `package mms.notification.v1;` | Namespace logical, dùng cho proto reference. |
| `option csharp_namespace = ...` | Namespace C# generated — **phải khớp** với cấu trúc folder `MMS.Protos.Notification.V1`. |
| `import "google/protobuf/timestamp.proto";` | Import well-known type. Cần `Grpc.Tools` để resolve. |
| `service NotificationService { ... }` | Định nghĩa "interface" — Grpc.Tools sinh ra `NotificationService.NotificationServiceBase` (abstract cho server) + `NotificationService.NotificationServiceClient` (client stub). |
| `rpc SendOne(Req) returns (Res);` | Unary RPC. |
| `rpc FanOut(Req) returns (stream Res);` | Server-stream RPC. |
| `string recipient_user_id = 1;` | Field number `1` — **wire format identity**. ⚠️ KHÔNG đổi sau khi deploy. |
| `map<string, string> metadata = 5;` | Map type — generated thành `IDictionary<string, string>`. |

**Code C# được sinh ra (xem trong `obj/Debug/.../*.cs` sau khi build):**

Phía **client** (dùng ở API):
```csharp
var client = new NotificationService.NotificationServiceClient(channel);
var response = await client.SendOneAsync(new SendNotificationRequest {
    RecipientUserId = userId.ToString(),
    Title = "...",
    Body = "...",
    Channel = "email"
});
```

Phía **server** (sẽ implement ở `MMS.NotificationService` project, Phase 9):
```csharp
public class NotificationGrpcService : NotificationService.NotificationServiceBase
{
    public override async Task<SendNotificationResponse> SendOne(
        SendNotificationRequest request, ServerCallContext context)
    {
        // ... logic gửi
        return new SendNotificationResponse { NotificationId = ..., Success = true };
    }

    public override async Task FanOut(
        FanOutRequest request,
        IServerStreamWriter<DeliveryStatus> responseStream,
        ServerCallContext context)
    {
        foreach (var userId in request.RecipientUserIds)
        {
            var status = await SendToUser(userId, request);
            await responseStream.WriteAsync(status);  // stream từng status
        }
    }
}
```

> 💡 **Quy tắc đặt tên field:** `snake_case` trong `.proto`, Grpc.Tools tự convert sang `PascalCase` C#.

> ⚠️ **Field number rules:**
> - Số 1-15: tiết kiệm wire (1 byte) — dùng cho field hay xuất hiện.
> - Số 16+: 2 byte.
> - **Không đổi**, **không reuse** field number của field đã xoá (mark `reserved 3;`).

---

### B.7. Auth interceptor, retry policy, deadline

#### B.7.1. Client interceptor — inject JWT

File `GrpcAuthClientInterceptor.cs` (đã có) — concept:

```csharp
public class GrpcAuthClientInterceptor : Interceptor
{
    private readonly ITokenProvider _tokens;
    
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var headers = new Metadata
        {
            { "Authorization", $"Bearer {_tokens.GetServiceToken()}" }
        };
        var newOptions = context.Options.WithHeaders(headers);
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, newOptions);
        return continuation(request, newContext);
    }
    // ... override các kiểu stream khác tương tự
}
```

#### B.7.2. Server interceptor — verify JWT

`GrpcAuthServerInterceptor.cs` (đã có) — concept:

```csharp
public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
    TRequest request, ServerCallContext context,
    UnaryServerMethod<TRequest, TResponse> continuation)
{
    var auth = context.RequestHeaders.GetValue("authorization");
    var userId = _jwt.Validate(auth);
    context.UserState["UserId"] = userId;
    return await continuation(request, context);
}
```

#### B.7.3. Retry + Deadline

Trong `GrpcClientRegistrationExtensions.cs` đã có Polly retry:
```csharp
.AddTransientHttpErrorPolicy(p =>
    p.WaitAndRetryAsync(options.RetryCount,
        attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))));
```

⚠️ **Deadline (timeout per call) — bạn chưa setup. Bắt buộc thêm khi vào Phase 9.** gRPC call không có deadline mặc định → hang vô hạn → exhaust thread pool. Cách set:

```csharp
var deadline = DateTime.UtcNow.AddSeconds(30);
var response = await client.SendOneAsync(req, deadline: deadline);
```

Hoặc set default trên channel:
```csharp
.ConfigureChannel(ch => 
{
    ch.MaxReceiveMessageSize = ...;
    // ch.UnsafeUseInsecureChannelCallCredentials = ...;
});
```

---

### B.8. Cách thêm 1 gRPC service mới (workflow 8 bước)

Ví dụ: thêm `NotificationService` (Phase 9).

1. **Định nghĩa `.proto`** ở `MMS.Protos/Protos/Notification/v1/notification.proto` (đã có).
2. **Build `MMS.Protos`** → Grpc.Tools sinh client + server stub vào `obj/`.
3. **Tạo project mới `MMS.NotificationService`** (ASP.NET Core Empty hoặc Worker):
   ```
   dotnet new web -n MMS.NotificationService
   dotnet add MMS.NotificationService reference MMS.Protos
   dotnet add MMS.NotificationService package Grpc.AspNetCore
   ```
4. **Implement gRPC service** extends `NotificationService.NotificationServiceBase` ở `MMS.NotificationService/Services/NotificationGrpcService.cs`.
5. **Register service** trong `Program.cs` của project:
   ```csharp
   builder.Services.AddGrpc();
   app.MapGrpcService<NotificationGrpcService>();
   ```
6. **Tạo client wrapper** ở `MMS.Application/Interfaces/Grpc/INotificationClient.cs` (interface) + `MMS.Infrastructure/gRPC/Clients/GrpcNotificationClient.cs` (impl gọi stub).
7. **Đăng ký client** vào DI ở `GrpcClientRegistrationExtensions.cs`:
   ```csharp
   services.AddGrpcClient<NotificationService.NotificationServiceClient>(o =>
       o.Address = new Uri(options.NotificationEndpoint))
       .AddInterceptor<GrpcAuthClientInterceptor>()
       .AddTransientHttpErrorPolicy(...);
   services.AddScoped<INotificationClient, GrpcNotificationClient>();
   ```
8. **Wire EventHandler** ở `MMS.Application/EventHandlers/` — handle `SubmissionCreatedDomainEvent` → call `INotificationClient.FanOutAsync(...)`.

---

### B.9. Đánh giá cấu trúc gRPC hiện tại

| Tiêu chí | Đánh giá | Ghi chú |
|---|---|---|
| Tách project `MMS.Protos` riêng | ✅ Tốt | Best practice, đúng Phase 2.1. |
| Folder `Service/v1/*.proto` | ✅ Tốt | Versioning convention chuẩn. |
| Set `option csharp_namespace` | ✅ Tốt | Verify ở `notification.proto`. |
| Import `google/protobuf/timestamp.proto` | ✅ Tốt | Dùng well-known types. |
| Interceptor có sẵn (auth client + server) | ✅ Tốt | Sẵn sàng cho Phase 9+. |
| `GrpcClientOptions` + DI extension | ✅ Tốt | Đúng pattern. |
| Polly retry policy | ✅ Tốt | Đã có exponential backoff. |
| Deadline per call | ❌ Thiếu | Bắt buộc bổ sung Phase 9. |
| `image_processing.proto` còn nằm trong | ⚠️ Stale | Xoá khi vào Phase 3. |
| Folder `Clients/` wrapper | ❌ Chưa có | Tạo Phase 9. |
| Folder `Interceptors/` (gom interceptor) | ⚠️ Có thể tổ chức lại | Khi >2 interceptor thì gom vào subfolder. |

**Kết luận gRPC phần B:** cấu trúc hiện tại **đúng hướng và tốt cho Phase 0-2**. Phải bổ sung khi vào Phase 9/10:
1. Xoá `image_processing.proto` + reference (Phase 3 dùng Cloudinary).
2. Set deadline per call.
3. Tạo `Clients/` wrapper layer.
4. Tạo project `MMS.NotificationService` + `MMS.AuditLogService` (mỗi project 1 process gRPC server riêng).

---

## Phần C — Bản đồ giao thức theo Phase

(Tham chiếu chéo với [lo-trinh-trien-khai-chi-tiet.md](./lo-trinh-trien-khai-chi-tiet.md#2-bản-đồ-giao-thức))

| Phase | GraphQL | gRPC | SignalR | REST |
|---|---|---|---|---|
| 0 (Foundation) | — | — | — | — |
| 1 (RBAC) | Q/M User-Role | — | — | — |
| 2 (gRPC bootstrap) | — | (chưa expose RPC) | — | — |
| 3 (Cloudinary upload) | — | — | — | `POST /api/upload` |
| 4 (Manuscript version) | Q manuscript/page history | — | — | (kế thừa Phase 3) |
| 5 (Submission workflow) | M submitManuscript, forwardToBoard · Q submissionInbox | — | — | — |
| 6 (Annotation) | M createAnnotation, resolveAnnotation · Q annotationsByPage | — | (broadcast P8) | — |
| 7 (Board voting) | M castVote, openSession · Q boardSessions | — | (live count P8) | — |
| 8 (SignalR foundation) | Subscription onAnnotationAdded, ... | — | **4 Hubs** | — |
| 9 (gRPC Notification) | — | **NotificationService.FanOut/SendOne** | (forward DeliveryStatus) | — |
| 10 (gRPC AuditLog) | Q auditLog | **AuditLog.WriteStream** | — | — |
| 11 (Ranking/Survey) | M submitSurvey · Q rankingTrend, upcomingReleases | — | (notify P9) | — |
| 12 (Analytics) | Q tantouDashboard, boardOverview | (optional) AnalyticsAggregation | — | — |
| 13 (Optimization) | — | (optional) AiAssist bidirectional | — | — |

---

## Phần D — Quy tắc chọn giao thức

Khi gặp 1 use case mới, dùng cây quyết định sau:

```
Câu hỏi 1: Client là gì?
│
├── Browser/mobile (FE)
│   │
│   ├── Câu hỏi 2: Có streaming / push từ server?
│   │   ├── CÓ ──▶ SignalR Hub  HOẶC  GraphQL Subscription
│   │   │         (chọn 1 — đừng làm cả 2 cho cùng event)
│   │   └── KHÔNG ──▶ Câu hỏi 3
│   │
│   └── Câu hỏi 3: Upload/download binary lớn (>1MB)?
│       ├── CÓ ──▶ REST multipart endpoint
│       └── KHÔNG ──▶ GraphQL (default)
│
└── Server khác / background service (server-to-server)
    │
    ├── Câu hỏi 4: Cross-language? (vd Python AI service)
    │   ├── CÓ ──▶ gRPC (proto là lingua franca)
    │   └── KHÔNG ──▶ Câu hỏi 5
    │
    └── Câu hỏi 5: Có streaming hoặc throughput cao?
        ├── CÓ ──▶ gRPC (vì stream native)
        └── KHÔNG ──▶ gRPC unary HOẶC REST nội bộ
                     (gRPC nếu đã có infra, REST nếu chỉ 1 call rare)
```

**Ví dụ áp dụng:**

| Use case | Giao thức | Lý do |
|---|---|---|
| Mangaka submit manuscript | GraphQL Mutation | FE → BE, không binary, không stream. |
| Mangaka upload page (15MB ảnh) | REST multipart | Binary lớn, GraphQL không phù hợp. |
| Tantou xem live vote count | SignalR | Push từ BE, low-latency, hub group. |
| API → NotificationService gửi email | gRPC unary | Server-to-server, contract chặt. |
| API stream audit event → AuditLogService | gRPC client-stream | Throughput cao, batch insert. |
| Board dashboard query | GraphQL Query (+ DataLoader) | Nhiều field nested, FE chọn. |
| FE nhận thông báo new submission | SignalR + GraphQL Subscription | Tuỳ — khuyến nghị SignalR cho user notification (đơn giản). |

---

## Phần E — Anti-patterns

### GraphQL

🔥 **1. Trả entity EF Core trực tiếp từ resolver.**
   - Sai: `return await _db.Submissions.FindAsync(id);`
   - Đúng: `return _mapper.Map<SubmissionResponse>(entity);`
   - Lý do: Entity có lazy navigation → schema phình to với field không muốn expose; circular reference; security leak (vd field `PasswordHash` trên User entity).

🔥 **2. Check authorization trong service body.**
   - Sai: `if (user.Role != "Tantou") return Result.Failure(...)` trong service.
   - Đúng: `[Authorize(Policy = "CanReviewSubmission")]` trên resolver.

🔥 **3. Bỏ qua DataLoader, query tuần tự.**
   - Sai: resolver `Submission.Tantou` gọi `_userSvc.GetById(sub.TantouId)` từng cái.
   - Đúng: `UserByIdDataLoader.LoadAsync(sub.TantouId)`.

🔥 **4. Mutation trả `Boolean`.**
   - Sai: `Task<bool> DeleteSubmission(Guid id)`.
   - Đúng: `Task<SubmissionResponse> DeleteSubmission(...)` (return entity trước khi delete) hoặc payload type `{ success, deletedId, errors[] }`.

🔥 **5. Subscription không authorize.**
   - Subscription chạy qua WebSocket — phải verify JWT khi connect + verify quyền join topic.

### gRPC

🔥 **6. Đổi field number trong `.proto`.**
   - Sai: `string title = 2;` đổi thành `string title = 5;` → break tất cả client đã deploy.
   - Đúng: `reserved 2;` nếu xoá field; thêm field mới với number chưa dùng.

🔥 **7. Gọi gRPC từ browser.**
   - Browser không hiểu Protobuf binary. Nếu thực sự cần → setup gRPC-Web (Envoy proxy) — phức tạp, MVS không nên.

🔥 **8. Không set deadline.**
   - `await client.FanOutAsync(req)` không có deadline → service đang slow → hang → thread pool exhausted → API down.
   - Đúng: `await client.FanOutAsync(req, deadline: DateTime.UtcNow.AddSeconds(30))`.

🔥 **9. gRPC fail = throw lên service business.**
   - Sai: NotificationService down → mutation `submitManuscript` cũng fail.
   - Đúng: catch `RpcException` → log + queue retry → mutation vẫn return success cho user.

🔥 **10. Thêm Grpc.AspNetCore vào `MMS.Protos`.**
   - `MMS.Protos` là **class library pure** — chỉ chứa contract. Server dependency để project service tự add.

🔥 **11. Tạo gRPC service cho mọi thứ.**
   - Over-engineering. Chỉ tách khi có lý do thật: CPU intensive, throughput cao, cross-language. Inbox notification 100/ngày → REST internal là đủ, không cần gRPC.

---

## Phần F — Checklist dev hằng ngày

Khi bạn implement 1 task có liên quan GraphQL hoặc gRPC, chạy qua checklist này:

### Cho 1 GraphQL feature mới

- [ ] Có DTO request/response ở `MMS.Application/DTOs/`? (KHÔNG trả entity EF Core)
- [ ] Service trả `Result<T>` không phải throw cho lỗi nghiệp vụ?
- [ ] Resolver gắn `[Authorize(Policy = ...)]`?
- [ ] Mutation return entity (để FE update cache)?
- [ ] N+1 check: field nested có DataLoader chưa?
- [ ] Test qua **Banana Cake Pop** (UI HotChocolate ở `/graphql`)?
- [ ] Exception map đúng qua `GraphQLErrorFilter`?
- [ ] Schema introspection (`{ __schema { types { name } } }`) hiển thị field mới?

### Cho 1 gRPC service mới

- [ ] `.proto` đặt đúng folder `Protos/{Service}/v1/`?
- [ ] `option csharp_namespace` match folder?
- [ ] Field number duy nhất, không reuse?
- [ ] Build `MMS.Protos` thành công → stub sinh ra ở `obj/`?
- [ ] Project service mới reference `MMS.Protos`, add `Grpc.AspNetCore`?
- [ ] Server interceptor verify JWT?
- [ ] Client wrapper interface ở `MMS.Application/Interfaces/Grpc/`?
- [ ] Client gọi có **deadline**?
- [ ] Polly retry policy enabled?
- [ ] Test qua **grpcurl** hoặc **Postman gRPC** trước khi wire vào EventHandler?
- [ ] Catch `RpcException` để KHÔNG block business?

---

## Phần G — Tài liệu tham khảo

### Lệnh CLI hữu ích

```powershell
# Build protos để regenerate code
dotnet build MMS.Protos/MMS.Protos.csproj

# Restore tools
dotnet restore

# Test GraphQL local (sau khi run API)
# Mở browser: https://localhost:5001/graphql  (Banana Cake Pop UI)

# Test gRPC local với grpcurl (cài: choco install grpcurl)
grpcurl -plaintext localhost:5005 list
grpcurl -plaintext -d '{"recipient_user_id":"abc","title":"hi","body":"test","channel":"email"}' `
    localhost:5005 mms.notification.v1.NotificationService/SendOne

# Linter proto (cài Buf optional)
buf lint MMS.Protos/Protos
buf breaking MMS.Protos/Protos --against '.git#branch=main'
```

### Link đọc thêm

- **HotChocolate (GraphQL):** https://chillicream.com/docs/hotchocolate — official docs.
- **Protobuf style guide:** https://protobuf.dev/programming-guides/style/
- **gRPC .NET docs:** https://learn.microsoft.com/aspnet/core/grpc
- **SignalR .NET:** https://learn.microsoft.com/aspnet/core/signalr/introduction

### File liên quan trong repo MMS

- Cấu trúc DI tổng: [src/MMS.Infrastructure/DependencyInjection.cs](../src/MMS.Infrastructure/DependencyInjection.cs)
- Cấu hình appsettings: [src/MMS.API/appsettings.json](../src/MMS.API/appsettings.json)
- gRPC client options: [src/MMS.Infrastructure/gRPC/GrpcClientOptions.cs](../src/MMS.Infrastructure/gRPC/GrpcClientOptions.cs)
- Lộ trình triển khai: [Claude-Plan/lo-trinh-trien-khai-chi-tiet.md](./lo-trinh-trien-khai-chi-tiet.md)
- Kế hoạch tổng quan: [Claude-Plan/ke-hoach-trien-khai.md](./ke-hoach-trien-khai.md)

---

## TÓM LẠI — Đáp 3 câu hỏi cốt lõi

### Hỏi 1: GraphQL được dùng trong MMS như thế nào?

GraphQL là **giao tiếp chính giữa FE web và BE API**. Mọi CRUD user/series/chapter/submission/annotation/vote/ranking/dashboard đều qua 1 endpoint duy nhất `/graphql`. FE viết query/mutation chọn đúng field cần, BE (HotChocolate) parse, validate authorization, gọi service, trả JSON.

**Cấu trúc hiện tại tốt** (Queries/Mutations tách theo entity, có ErrorFilter, có Helper DI). Phase 8 sẽ bổ sung `Subscriptions/`, Phase 5+ bổ sung `DataLoaders/`.

### Hỏi 2: gRPC được dùng trong MMS như thế nào?

gRPC chỉ dùng **server-to-server nội bộ**, KHÔNG cho browser:
- **Phase 9:** API → NotificationService (unary `SendOne`, server-stream `FanOut`) gửi email/push.
- **Phase 10:** API → AuditLogService (client-stream `WriteStream`) batch insert audit log.
- **Phase 13 (optional):** API ↔ AiAssistService Python (bidirectional) suggest annotation.

**Cấu trúc hiện tại tốt:** project `MMS.Protos` tách riêng, `.proto` versioning chuẩn (`v1/`), có interceptor auth, có Polly retry. **Phải dọn:** xoá `image_processing.proto` (Cloudinary thay thế), thêm deadline per call, tạo `Clients/` wrapper khi Phase 9.

### Hỏi 3: Cấu trúc dự án có tốt không?

**Đánh giá tổng quan: 8/10** — đi đúng hướng best practice industry.

✅ Điểm tốt:
- Clean Architecture (Domain ← Application ← Infrastructure ← API).
- Tách `MMS.Protos` thành class lib riêng → không bị lệch contract giữa client/server.
- GraphQL tổ chức theo entity, có error filter.
- gRPC interceptor sẵn cho auth, Polly retry sẵn.

⚠️ Cần fix khi vào phase tương ứng:
1. Xoá `image_processing.proto` (Phase 3).
2. Set deadline per gRPC call (Phase 9).
3. Tạo `Subscriptions/`, `DataLoaders/`, `Inputs/` trong GraphQL (Phase 5-8).
4. Tạo `Clients/` wrapper layer cho gRPC (Phase 9).
5. Audit `[Authorize(Policy=...)]` đầy đủ trên resolver (Phase 1).

**Tóm gọn 1 dòng:** *Cấu trúc đã đúng — chỉ cần đi tiếp theo lộ trình, đừng cố refactor lớn lúc này.*

---

> **Đọc xong tài liệu này, bạn nên có khả năng:**
> 1. Phân biệt khi nào dùng GraphQL, gRPC, REST, SignalR cho 1 use case mới.
> 2. Thêm 1 GraphQL Query/Mutation/Subscription mà không hỏi mentor.
> 3. Tạo 1 `.proto` file và 1 gRPC client wrapper end-to-end.
> 4. Biết file nào trong codebase tương ứng với khái niệm nào.
> 5. Tránh được 11 anti-patterns thường gặp.
>
> **Khi bí:** quay lại [lo-trinh-trien-khai-chi-tiet.md](./lo-trinh-trien-khai-chi-tiet.md) tìm phase chứa task → đọc 5 khối A/B/C/D/E của task đó.
