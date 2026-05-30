# Kiến trúc SignalR (Phase 8) & gRPC Email Service (Phase 9)
## Hướng dẫn đọc hiểu cho người mới vào dự án

> **Mục đích:** Giải thích đầy đủ luồng đi, lý do tồn tại, và cách sử dụng của SignalR và gRPC Email Service. Không cần biết gì về dự án trước, chỉ cần đọc từ trên xuống dưới.

---

## MỤC LỤC

1. [Bức tranh tổng thể](#1-bức-tranh-tổng-thể)
2. [Phase 8 — SignalR Realtime](#2-phase-8--signalr-realtime)
   - [Vấn đề cần giải quyết](#21-vấn-đề-cần-giải-quyết)
   - [Kiến trúc tổng quan](#22-kiến-trúc-tổng-quan)
   - [Luồng từ đầu đến cuối](#23-luồng-từ-đầu-đến-cuối)
   - [Các thành phần chi tiết](#24-các-thành-phần-chi-tiết)
   - [4 Hub và tác dụng từng cái](#25-4-hub-và-tác-dụng-từng-cái)
   - [Group naming convention](#26-group-naming-convention)
3. [Phase 9 — gRPC Email Service](#3-phase-9--grpc-email-service)
   - [Vấn đề cần giải quyết](#31-vấn-đề-cần-giải-quyết)
   - [Kiến trúc tổng quan](#32-kiến-trúc-tổng-quan)
   - [Luồng gửi email từ đầu đến cuối](#33-luồng-gửi-email-từ-đầu-đến-cuối)
   - [Các thành phần chi tiết](#34-các-thành-phần-chi-tiết)
4. [Khi nào dùng cái nào?](#4-khi-nào-dùng-cái-nào)
5. [Cách test thủ công](#5-cách-test-thủ-công)
6. [Map file theo chức năng](#6-map-file-theo-chức-năng)

---

## 1. Bức tranh tổng thể

Dự án **Manga Management System (MMS)** quản lý quy trình xuất bản manga: từ tác giả (Mangaka) nộp bản thảo → Tantou Editor kiểm duyệt → Editorial Board bỏ phiếu → publish.

Trong quy trình này có 2 nhu cầu kỹ thuật:

| Nhu cầu | Giải pháp |
|---|---|
| Người dùng cần thấy cập nhật **ngay lập tức** khi ai đó action (tạo annotation, bỏ phiếu, duyệt bản thảo) mà không cần F5 trang | **SignalR** — đẩy dữ liệu từ server xuống browser qua WebSocket |
| Gửi **email thông báo** khi có sự kiện quan trọng, nhưng logic gửi email không được chạy trực tiếp trong API (chậm, khó scale, fail làm ảnh hưởng cả request) | **gRPC NotificationService** — tách gửi email thành microservice riêng |

---

## 2. Phase 8 — SignalR Realtime

### 2.1 Vấn đề cần giải quyết

Không có SignalR, browser chỉ nhận dữ liệu khi **user chủ động gọi API** (HTTP pull). Kết quả:
- Tantou đang xem chapter review, Mangaka vừa tạo annotation mới → Tantou phải F5 mới thấy.
- Board member đang xem voting screen, đồng nghiệp vừa vote → không biết gì cho đến khi reload.

SignalR giải quyết bằng cách giữ một **kết nối WebSocket** liên tục giữa browser và server. Server có thể push dữ liệu xuống **bất kỳ lúc nào**.

### 2.2 Kiến trúc tổng quan

```
[Browser Client]
     │
     │  WebSocket (wss://)
     │  JWT token qua query string: ?access_token=xxx
     ▼
[SignalR Hub]  ←─── [IHubContext<THub>]  ←─── [EventHandler]
                                                      ▲
                                                      │ INotificationHandler<TEvent>
                                               [MediatR.Publish()]
                                                      ▲
                                               [IDomainEventPublisher]
                                                      ▲
                                         [Service Layer: PublishAsync(event)]
                                                      ▲
                                         [DB: SaveChanges() hoàn thành]
```

**Nguyên tắc bất di bất dịch:** Event chỉ được publish SAU KHI `SaveChanges()` thành công. Không bao giờ push realtime rồi rollback DB — gây ra phantom data trên browser.

### 2.3 Luồng từ đầu đến cuối

Ví dụ: **Mangaka tạo annotation mới**

```
1. Mangaka gọi API POST /api/annotations
         │
         ▼
2. AnnotationService.CreateAsync()
   - validate quyền
   - tạo entity Annotation
   - _uow.SaveChangesAsync()  ← ĐÃ VÀO DB
         │
         ▼ (sau SaveChanges)
3. _eventPublisher.PublishAsync(new AnnotationCreatedEvent(
       annotation.Id, page.Id, chapter.Id, userId,
       category, x, y, width, height
   ))
         │
         ▼
4. DomainEventPublisher.PublishAsync()
   - gọi _mediator.Publish(event)
         │
         ▼
5. MediatR tìm INotificationHandler<AnnotationCreatedEvent>
   → tìm thấy AnnotationCreatedToHubHandler
         │
         ▼
6. AnnotationCreatedToHubHandler.Handle()
   - gọi _hubContext.Clients
       .Group("chapter:{chapterId}")
       .SendAsync("annotationAdded", payload)
         │
         ▼ (qua WebSocket)
7. TẤT CẢ browser đang trong group "chapter:{chapterId}"
   nhận được event "annotationAdded" với payload:
   {
     annotationId, pageId, category,
     x, y, width, height, createdBy
   }
         │
         ▼
8. Browser cập nhật UI ngay lập tức (không cần F5)
```

### 2.4 Các thành phần chi tiết

#### Domain Events — `src/MMS.Domain/Events/`

Event là "điều đã xảy ra". Dùng `record` vì bất biến (immutable) sau khi tạo.

**Base class:**
```
src/MMS.Domain/Events/DomainEvent.cs
```
```csharp
public abstract record DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
// Implement INotification từ MediatR để có thể dùng mediator.Publish()
```

**5 loại event hiện có:**

| File | Event | Khi nào fire |
|---|---|---|
| `Events/Annotations/AnnotationCreatedEvent.cs` | `AnnotationCreatedEvent` | Sau khi annotation được lưu vào DB |
| `Events/Annotations/AnnotationStatusChangedEvent.cs` | `AnnotationStatusChangedEvent` | Khi annotation chuyển sang Resolved/Rejected |
| `Events/BoardVoting/BoardVoteCastEvent.cs` | `BoardVoteCastEvent` | Sau mỗi lần board member vote submission |
| `Events/BoardVoting/ApplicationVoteCastEvent.cs` | `ApplicationVoteCastEvent` | Sau mỗi lần board vote đơn ứng tuyển |
| `Events/Submissions/SubmissionStatusChangedEvent.cs` | `SubmissionStatusChangedEvent` | Khi submission đổi trạng thái workflow |

---

#### IDomainEventPublisher — `src/MMS.Application/Interfaces/IDomainEventPublisher.cs`

Interface này là **cầu nối** giữa Application layer và SignalR/MediatR.

```csharp
public interface IDomainEventPublisher
{
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : DomainEvent;
}
```

**Lý do tồn tại:** Application Services (AnnotationService, SubmissionService...) cần publish event, nhưng **Application layer không được biết** về MediatR hay SignalR (đó là chi tiết kỹ thuật của API layer). Interface này cho phép Application chỉ biết "tôi cần publish event" — không biết ai xử lý.

**Implementation:** `src/MMS.API/Services/DomainEventPublisher.cs` — bên trong chỉ gọi `_mediator.Publish(event)`.

---

#### EventHandlers — `src/MMS.API/EventHandlers/`

Mỗi Handler nhận một loại event và push xuống đúng group SignalR.

| File | Nhận event | Push tới group | Tên event gửi xuống browser |
|---|---|---|---|
| `AnnotationCreatedToHubHandler.cs` | `AnnotationCreatedEvent` | `chapter:{chapterId}` | `"annotationAdded"` |
| `AnnotationStatusChangedToHubHandler.cs` | `AnnotationStatusChangedEvent` | `chapter:{chapterId}` | `"annotationStatusChanged"` |
| `BoardVoteCastToHubHandler.cs` | `BoardVoteCastEvent` | `submission:{submissionId}` | `"voteCountUpdated"` |
| `ApplicationVoteCastToHubHandler.cs` | `ApplicationVoteCastEvent` | `application:{applicationId}` | `"applicationVoteCountUpdated"` |
| `SubmissionStatusChangedToHubHandler.cs` | `SubmissionStatusChangedEvent` | `user:{notifyUserId}` | `"notificationReceived"` |

**Quy tắc bắt buộc cho mọi Handler:** Luôn wrap trong `try/catch` và chỉ `LogWarning` — KHÔNG rethrow. Nếu SignalR fail, annotation vẫn đã lưu vào DB rồi — không được rollback business operation vì lý do broadcast.

```csharp
// Đúng:
try {
    await _hubContext.Clients.Group(...).SendAsync(...);
} catch (Exception ex) {
    _logger.LogWarning(ex, "Broadcast failed — business operation is NOT affected");
}

// SAI — KHÔNG BAO GIỜ làm:
await _hubContext.Clients.Group(...).SendAsync(...);  // throw = rollback request
```

---

#### JWT Authentication cho SignalR

WebSocket không thể gửi `Authorization: Bearer xxx` header như HTTP thông thường. Giải pháp: gửi JWT qua **query string**.

**Config trong `src/MMS.Infrastructure/DependencyInjection.cs`** (AddJwtAuthentication):
```csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        var token = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;
        if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hub"))
            context.Token = token;
        return Task.CompletedTask;
    }
};
```

**Kết quả:** Khi browser kết nối SignalR, URL sẽ là:
```
wss://localhost:5001/hub/annotations?access_token=eyJhbGciOi...
```

---

### 2.5 4 Hub và tác dụng từng cái

Tất cả Hub nằm ở `src/MMS.API/Hubs/`. Tất cả đều kế thừa `BaseAuthHub` — tự động:
1. Yêu cầu `[Authorize]` — không có JWT hợp lệ → không connect được.
2. Khi connect: join group `user:{userId}` → để server push notification cá nhân.
3. Khi disconnect: rời group `user:{userId}`.

---

**`NotificationHub.cs`** — Nhận thông báo cá nhân

```
Client kết nối → auto join group "user:{userId}"
Không có method nào client cần gọi — chỉ lắng nghe
Server push xuống khi cần: "notificationReceived"
```

Use case: Mangaka nhận thông báo "Bản thảo của bạn đã được duyệt" mà không cần polling.

---

**`AnnotationHub.cs`** — Cộng tác realtime khi review chapter

```
Client kết nối
Client gọi: JoinChapter(chapterId)
  → Server validate: user phải là AuthorId hoặc TantouEditorId của series chứa chapter này
  → Nếu hợp lệ: join group "chapter:{chapterId}"
  
Client gọi: LeaveChapter(chapterId)
  → rời group "chapter:{chapterId}"

Server push xuống group khi ai đó:
  - Tạo annotation mới:     "annotationAdded"
  - Thay đổi status annotation: "annotationStatusChanged"
```

Use case: Tantou và Mangaka cùng mở trang review chapter, annotation của một người hiện ngay ở màn hình người kia.

---

**`BoardVotingHub.cs`** — Phiên họp bỏ phiếu realtime

```
Client gọi: JoinSubmission(submissionId)
  → Validate: user phải là "Editorial Board" hoặc "Tantou Editor"
  → Nếu hợp lệ: join group "submission:{submissionId}"

Client gọi: JoinApplication(applicationId)
  → Validate: user phải là "Editorial Board"
  → join group "application:{applicationId}"

Server push xuống group khi ai đó vote:
  - Vote submission:   "voteCountUpdated"   { approveCount, rejectCount, totalVotes }
  - Vote application:  "applicationVoteCountUpdated"
  
⚠️ Không bao giờ broadcast AI vote gì — chỉ số đếm tổng.
```

Use case: 5 board member cùng mở trang voting, mỗi lần ai vote thì màn hình tất cả cập nhật số phiếu.

---

**`ChapterProgressHub.cs`** — Theo dõi tiến độ chương

```
Client gọi: JoinChapter(chapterId)
  → join group "progress:chapter:{chapterId}"
  
Client gọi: LeaveChapter(chapterId)
  → rời group

Server push: "pageStatusChanged", "chapterProgressUpdated"
```

Use case: Tantou xem dashboard tiến độ, thấy các page chuyển trạng thái Draft → Inked → Done theo thời gian thực.

---

### 2.6 Group naming convention

| Group name | Ý nghĩa | Dùng ở Hub |
|---|---|---|
| `user:{userId}` | Notification riêng cho 1 user | NotificationHub (auto join) |
| `chapter:{chapterId}` | Tất cả người đang review chapter này | AnnotationHub |
| `submission:{submissionId}` | Tất cả board đang vote submission này | BoardVotingHub |
| `application:{applicationId}` | Tất cả board đang vote application này | BoardVotingHub |
| `progress:chapter:{chapterId}` | Dashboard tiến độ chapter | ChapterProgressHub |

---

## 3. Phase 9 — gRPC Email Service

### 3.1 Vấn đề cần giải quyết

Trước Phase 9, API gửi email **trực tiếp** qua SMTP trong cùng request. Vấn đề:
- SMTP call mất 500ms–3s → toàn bộ API request bị block chờ.
- Không thể scale riêng phần gửi email.
- Logic gửi email nằm lẫn vào business logic.

Giải pháp: tạo **microservice riêng** (`MMS.NotificationService`) chuyên gửi email. API giao tiếp với service này qua **gRPC** (nhanh, type-safe, binary protocol).

### 3.2 Kiến trúc tổng quan

```
MMS.API (gRPC CLIENT)
│
│  Code gọi: IEmailService.SendApplicationApprovedAsync(email, name, title)
│
│  DI resolve: IEmailService → GrpcEmailService (Adapter)
│
│  GrpcEmailService.SendViaGrpcAsync()
│       │
│       │  gRPC call: NotificationServiceClient.SendOneAsync(request)
│       │  + JWT token tự động đính kèm (GrpcAuthClientInterceptor)
│       │  + Retry tự động nếu lỗi thoáng qua (Polly)
│       │
│  ─────│──── gRPC over HTTP/2 (port 5102) ──────────────►
│       │
│       ▼
MMS.NotificationService (gRPC SERVER - process riêng)
│
│  NotificationGrpcService.SendOne()
│       │
│       ├── Nếu request có metadata["direct_email"]: dùng email đó luôn
│       └── Nếu không: IUserEmailLookup.GetEmailAsync(userId) → query DB
│
│  SmtpEmailSender.SendAsync(toEmail, subject, body)
│       │
│       └── MailKit → SMTP Server (Gmail hoặc SMTP bất kỳ)
│               │
│  ◄────────── SendNotificationResponse { success, notificationId }
```

### 3.3 Luồng gửi email từ đầu đến cuối

Ví dụ: **Board approve submission, gửi email báo Mangaka**

```
1. SubmissionService.FinalizeBoardDecisionAsync(submissionId, Approved, comment)
   - validate, update status, SaveChanges()
         │
         ▼
2. Lấy thông tin mangaka:
   var mangaka = await _uow.Users.GetByIdAsync(submission.MangakaId.Value);
         │
         ▼
3. Gọi gRPC wrapper:
   await _notificationClient.SendEmailAsync(
       mangaka.Id.ToString(),
       "Kết quả duyệt bản thảo",
       "Chúc mừng! Bản thảo 'xxx' đã được duyệt xuất bản."
   );
         │
         ▼
4. NotificationGrpcClientWrapper.SendEmailAsync()
   gọi: _client.SendOneAsync(new SendNotificationRequest {
       RecipientUserId = mangakaId,
       Title = "Kết quả duyệt bản thảo",
       Body = "Chúc mừng! ...",
       Channel = "email"
   })
         │ (HTTP/2, binary protobuf)
         ▼
5. MMS.NotificationService nhận request
   NotificationGrpcService.SendOne():
   - Kiểm tra metadata["direct_email"]: không có
   - Gọi DbUserEmailLookup.GetEmailAsync(mangakaId)
     → query DB: SELECT Email FROM Users WHERE Id = @id
     → kết quả: "mangaka@example.com"
         │
         ▼
6. SmtpEmailSender.SendAsync("mangaka@example.com", "", subject, body)
   - Tạo MimeMessage với HTML body
   - Connect SMTP (Gmail:587, StartTLS)
   - Authenticate, Send, Disconnect
         │
         ▼
7. Trả về SendNotificationResponse { Success = true, NotificationId = "..." }
         │
         ▼ (response về API)
8. NotificationGrpcClientWrapper log: "SendOne to {userId}: success=True"
         │
         ▼
9. API request hoàn thành — Mangaka đã nhận email
```

**Nếu MMS.NotificationService không chạy:**
```
Bước 4 gRPC call → lỗi connection refused
→ GrpcEmailService.SendViaGrpcAsync() catch exception
→ LogWarning("Failed to send email via gRPC ... Falling back to console log.")
→ Console.WriteLine("[Email Simulation - gRPC unavailable] To: ...")
→ API tiếp tục bình thường — KHÔNG throw, KHÔNG fail request
```

### 3.4 Các thành phần chi tiết

#### Proto contract — `MMS.Protos/Protos/Notification/v1/notification.proto`

Proto là "hợp đồng" giữa client và server. Cả hai bên đều build từ file này.

```protobuf
service NotificationService {
  rpc SendOne(SendNotificationRequest) returns (SendNotificationResponse);
  rpc FanOut(FanOutRequest) returns (stream DeliveryStatus);
}
```

- **`SendOne`**: Unary — gửi 1 request, nhận 1 response.
- **`FanOut`**: Server-streaming — gửi 1 request với danh sách userId, nhận về **stream** từng `DeliveryStatus` khi service gửi xong cho từng người.

---

#### IEmailService — `src/MMS.Application/Interfaces/Services/IEmailService.cs`

Interface **không thay đổi** từ trước Phase 9. Mọi code gọi email vẫn giữ nguyên:

```csharp
public interface IEmailService
{
    Task SendInvitationAsync(string email, string token, string roleName);
    Task SendUnderperformanceAlertAsync(string email, string authorName, string seriesTitle, int currentRanking);
    Task SendApplicationApprovedAsync(string email, string fullName, string seriesTitle);
    Task SendApplicationRejectedAsync(string email, string fullName, string seriesTitle, string? reason);
}
```

---

#### GrpcEmailService — `src/MMS.Infrastructure/Services/GrpcEmailService.cs`

**Đây là Adapter Pattern.** Implement `IEmailService` nhưng bên trong gọi gRPC thay vì SMTP trực tiếp.

```
IEmailService
     ▲
     │ implements
GrpcEmailService
     │ dùng
NotificationService.NotificationServiceClient (gRPC stub tự sinh từ proto)
```

Điểm đặc biệt: Gửi email bằng **địa chỉ email trực tiếp** (không qua lookup userId), dùng metadata:
```csharp
Metadata = { ["direct_email"] = email }
```
Phía server nhận được metadata này và dùng email đó luôn, bỏ qua bước lookup DB.

---

#### INotificationGrpcClient — `src/MMS.Application/Interfaces/Services/INotificationGrpcClient.cs`

Wrapper cho gRPC khi cần **gửi theo userId** (không có email trực tiếp):

```csharp
public interface INotificationGrpcClient
{
    Task SendEmailAsync(string userId, string title, string body);
    Task FanOutEmailAsync(IEnumerable<string> userIds, string title, string body);
}
```

**Implement:** `src/MMS.Infrastructure/Services/NotificationGrpcClientWrapper.cs`

`FanOutEmailAsync` dùng **server-streaming**: gửi 1 request → nhận về từng `DeliveryStatus` qua `await foreach`:
```csharp
using var stream = _client.FanOut(request);
await foreach (var status in stream.ResponseStream.ReadAllAsync())
{
    _logger.LogInformation("FanOut delivery: userId={UserId}, delivered={Delivered}", ...);
}
```

---

#### MMS.NotificationService — `MMS.NotificationService/`

Đây là **process riêng**, chạy song song với MMS.API trên port 5102.

**Cấu trúc thư mục:**
```
MMS.NotificationService/
├── Program.cs                        ← setup gRPC server + DI
├── appsettings.json                  ← port 5102, SMTP config, connection string
└── Services/
    ├── NotificationGrpcService.cs    ← implement proto: SendOne + FanOut
    ├── SmtpEmailSender.cs            ← gửi email thực qua MailKit
    ├── IEmailSender.cs               ← interface cho email sender
    ├── IUserEmailLookup.cs           ← interface lookup email từ userId
    ├── DbUserEmailLookup.cs          ← implement: query MmsDbContext.Users
    └── SmtpOptions.cs                ← config class: Host, Port, Username...
```

**`NotificationGrpcService.SendOne()` — logic xử lý:**
```
Nhận SendNotificationRequest
│
├── Có metadata["direct_email"]?
│     → Dùng email đó luôn (không query DB)
│
└── Không có:
      → DbUserEmailLookup.GetEmailAsync(userId)
      → query: SELECT Email FROM Users WHERE Id = @userId
      → Không tìm thấy? Return Success=false
│
▼
SmtpEmailSender.SendAsync(email, subject, body)
│
├── SMTP config có đủ (Host, Username, Password)?
│     → MailKit: Connect → Auth → Send → Disconnect
│
└── SMTP config trống?
      → Console.WriteLine("[Email Simulation]...") (development mode)
│
▼
Return SendNotificationResponse { Success, NotificationId }
```

---

#### gRPC Authentication — `src/MMS.Infrastructure/gRPC/`

Tất cả gRPC call giữa MMS.API và MMS.NotificationService đều được authenticate:

```
MMS.API gửi gRPC call
│
GrpcAuthClientInterceptor (client-side)
→ Đọc config "Grpc:ServiceToken"
→ Thêm vào metadata: Authorization: Bearer {serviceToken}
│
──── gRPC call qua mạng ────►
│
GrpcAuthServerInterceptor (server-side)
→ Đọc metadata: Authorization: Bearer {serviceToken}
→ Validate bằng "Grpc:ServiceTokenSecret"
→ Token hết hạn hoặc sai? → Return Unauthenticated
→ Hợp lệ? → Tiếp tục xử lý
```

---

#### Dependency Injection — `src/MMS.Infrastructure/DependencyInjection.cs`

```csharp
// Email → đi qua gRPC NotificationService
services.AddScoped<IEmailService, GrpcEmailService>();

// Wrapper cho gRPC khi gửi theo userId
services.AddScoped<INotificationGrpcClient, NotificationGrpcClientWrapper>();

// Đăng ký gRPC clients (tự động có retry + JWT interceptor)
services.AddMmsGrpcClients(configuration);
```

Một dòng `services.AddScoped<IEmailService, GrpcEmailService>()` là đủ để **toàn bộ code cũ** đang gọi `IEmailService` tự động đi qua gRPC mà không cần sửa gì.

---

## 4. Khi nào dùng cái nào?

### Muốn push realtime xuống browser → dùng SignalR

Scenario và cách làm:

```csharp
// 1. Inject IDomainEventPublisher vào service
public MyService(IUnitOfWork uow, IDomainEventPublisher eventPublisher) { ... }

// 2. Tạo domain event mới nếu chưa có (trong MMS.Domain/Events/)
public record MyNewEvent(Guid EntityId, string SomeData) : DomainEvent;

// 3. Publish SAU SaveChanges
await _uow.SaveChangesAsync();
await _eventPublisher.PublishAsync(new MyNewEvent(entity.Id, data));

// 4. Tạo EventHandler trong MMS.API/EventHandlers/
public class MyNewEventHandler : INotificationHandler<MyNewEvent>
{
    public async Task Handle(MyNewEvent evt, CancellationToken ct)
    {
        try {
            await _hubContext.Clients.Group("someGroup").SendAsync("eventName", payload, ct);
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Broadcast failed");
        }
    }
}
```

### Muốn gửi email → dùng IEmailService hoặc INotificationGrpcClient

```csharp
// Option A: Gửi đến email address trực tiếp (dùng IEmailService)
// Inject: IEmailService _emailService
await _emailService.SendApplicationApprovedAsync(email, fullName, seriesTitle);
// → Tự động qua GrpcEmailService → gRPC → NotificationService → SMTP

// Option B: Gửi đến userId (dùng INotificationGrpcClient)
// Inject: INotificationGrpcClient _notificationClient
await _notificationClient.SendEmailAsync(userId.ToString(), subject, body);
// → NotificationService lookup email trong DB rồi gửi

// Option C: Gửi cho nhiều người (FanOut)
var userIds = boardMembers.Select(m => m.Id.ToString());
await _notificationClient.FanOutEmailAsync(userIds, subject, body);
// → Server-streaming: gửi lần lượt, nhận DeliveryStatus từng người
```

---

## 5. Cách test thủ công

### Test SignalR

File `signalr-test.html` ở thư mục gốc dự án, mở bằng browser bất kỳ:

```html
<!-- Chỉnh sửa 2 thứ: -->
const token = "PASTE_JWT_TOKEN_HERE";  // lấy từ POST /api/auth/login
const hubUrl = "https://localhost:5001/hub/notifications";  // hoặc /hub/annotations, /hub/boardvoting
```

Sau khi kết nối thành công ("Connected!" xuất hiện trong console), trigger một action từ Swagger/Postman và xem event hiện ra trong trang HTML.

### Test gRPC Email

1. Chạy `MMS.NotificationService` (project riêng, port 5102).
2. Chạy `MMS.API` bình thường.
3. Thực hiện action trigger email (vd: finalize submission approval).
4. Xem log của `MMS.NotificationService`:
   - Có SMTP config: xem email thực trong hộp thư.
   - Không có SMTP config: xem `[Email Simulation]` trong console.
5. Nếu `MMS.NotificationService` không chạy: `MMS.API` vẫn hoạt động, chỉ log Warning.

---

## 6. Map file theo chức năng

### SignalR (Phase 8)

```
MMS.Domain/
└── Events/
    ├── DomainEvent.cs                        ← Base class (abstract record)
    ├── Annotations/
    │   ├── AnnotationCreatedEvent.cs
    │   └── AnnotationStatusChangedEvent.cs
    ├── BoardVoting/
    │   ├── BoardVoteCastEvent.cs
    │   └── ApplicationVoteCastEvent.cs
    └── Submissions/
        └── SubmissionStatusChangedEvent.cs

MMS.Application/
└── Interfaces/
    └── IDomainEventPublisher.cs              ← Interface publish event

MMS.API/
├── Services/
│   └── DomainEventPublisher.cs              ← Implement bằng MediatR
├── Hubs/
│   ├── BaseAuthHub.cs                       ← Base: Authorize + user group
│   ├── NotificationHub.cs                   ← Personal notifications
│   ├── AnnotationHub.cs                     ← Chapter review realtime
│   ├── BoardVotingHub.cs                    ← Voting session realtime
│   └── ChapterProgressHub.cs                ← Chapter progress tracking
├── EventHandlers/
│   ├── AnnotationCreatedToHubHandler.cs
│   ├── AnnotationStatusChangedToHubHandler.cs
│   ├── BoardVoteCastToHubHandler.cs
│   ├── ApplicationVoteCastToHubHandler.cs
│   └── SubmissionStatusChangedToHubHandler.cs
└── Program.cs                               ← AddSignalR, AddMediatR, MapHub<T>

MMS.Infrastructure/
└── DependencyInjection.cs                   ← JWT OnMessageReceived cho /hub/*
```

### gRPC Email Service (Phase 9)

```
MMS.Protos/
└── Protos/Notification/v1/
    └── notification.proto                   ← Contract: SendOne, FanOut

MMS.Application/
└── Interfaces/Services/
    ├── IEmailService.cs                     ← Interface gửi email (không đổi)
    └── INotificationGrpcClient.cs           ← Interface cho SendEmailAsync, FanOut

MMS.Infrastructure/
├── Services/
│   ├── GrpcEmailService.cs                  ← Adapter: IEmailService → gRPC
│   └── NotificationGrpcClientWrapper.cs     ← Wrapper: INotificationGrpcClient
├── gRPC/
│   ├── GrpcClientRegistrationExtensions.cs  ← Đăng ký gRPC clients + Polly
│   ├── GrpcAuthClientInterceptor.cs         ← Gắn JWT vào mọi gRPC call
│   └── GrpcAuthServerInterceptor.cs         ← Validate JWT ở server gRPC
└── DependencyInjection.cs                   ← IEmailService → GrpcEmailService

MMS.NotificationService/  (PROJECT RIÊNG — chạy port 5102)
├── Program.cs                               ← AddGrpc, MapGrpcService
├── appsettings.json                         ← port, SMTP, connection string
└── Services/
    ├── NotificationGrpcService.cs           ← Implement SendOne + FanOut
    ├── SmtpEmailSender.cs                   ← MailKit SMTP sender
    ├── DbUserEmailLookup.cs                 ← Query email từ userId
    ├── IEmailSender.cs
    ├── IUserEmailLookup.cs
    └── SmtpOptions.cs
```

---

> **Tóm tắt một câu:**
> - SignalR: Service layer publish event sau SaveChanges → MediatR route event → EventHandler push xuống đúng group SignalR → browser nhận realtime.
> - gRPC Email: Code gọi `IEmailService` → DI resolve sang `GrpcEmailService` → gọi gRPC tới `MMS.NotificationService` đang chạy riêng → service lookup email + gửi SMTP.
