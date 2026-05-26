# Hướng Dẫn Chạy Test GraphQL (Manga Management System)

Tài liệu này chứa toàn bộ các câu lệnh GraphQL (Queries & Mutations) đã được tối ưu hóa dạng **inline** và cập nhật theo cơ chế phân quyền mới nhất, bao gồm cả các API của Phase 5 & 6.

---

## 1. KHỞI TẠO TÀI KHOẢN (Public API)

### Tạo tài khoản Mangaka:
```graphql
mutation {
  createUser(
    request: {
      username: "mangaka_test"
      email: "mangaka@example.com"
      password: "Password123!"
      roles: ["Mangaka"]
      mangakaId: null
    }
  ) {
    succeeded
    message
    data {
      id
      username
      email
      roles
    }
  }
}
```

### Tạo tài khoản Assistant (Liên kết với Mangaka):
*(Thay thế ID Mangaka vừa tạo vào trường `mangakaId`)*
```graphql
mutation {
  createUser(
    request: {
      username: "assistant_test"
      email: "assistant@example.com"
      password: "Password123!"
      roles: ["Assistant"]
      mangakaId: "ĐIỀN_ID_MANGAKA_VÀO_ĐÂY"
    }
  ) {
    succeeded
    message
    data {
      id
      username
      email
      mangakaId
    }
  }
}
```

---

## 2. ĐĂNG NHẬP ĐỂ LẤY TOKEN (Public API)

### Đăng nhập Mangaka:
```graphql
mutation {
  login(request: { emailOrUsername: "mangaka_test", password: "Password123!" }) {
    succeeded
    message
    data {
      accessToken
      roles
    }
  }
}
```
*(Copy `accessToken` đính kèm vào Header `Authorization: Bearer <TOKEN_MANGAKA>` khi gọi các lệnh ở Phần 3)*

### Đăng nhập Assistant:
```graphql
mutation {
  login(request: { emailOrUsername: "assistant_test", password: "Password123!" }) {
    succeeded
    message
    data {
      accessToken
      roles
    }
  }
}
```
*(Copy `accessToken` đính kèm vào Header `Authorization: Bearer <TOKEN_ASSISTANT>` khi gọi các lệnh ở Phần 4)*

---

## 3. CÁC HOẠT ĐỘNG CỦA MANGAKA (Token Mangaka)

*Đính kèm Header:* `Authorization: Bearer <TOKEN_MANGAKA>`

### Thiết lập lương cứng cho Trợ lý:
```graphql
mutation {
  setAssistantBaseSalary(
    assistantId: "ĐIỀN_ID_ASSISTANT_VÀO_ĐÂY"
    baseSalary: 1500.0
  ) {
    succeeded
    message
    errors
    data {
      id
      username
      baseSalary
    }
  }
}
```

### Giao việc cho Trợ lý (Assign Task):
```graphql
mutation {
  assignTask(
    input: {
      title: "Vẽ line-art vùng mắt nhân vật chính"
      description: "Yêu cầu vẽ sắc nét, đúng theo phong cách gothic."
      assistantId: "ĐIỀN_ID_ASSISTANT_VÀO_ĐÂY"
      regionId: "ĐIỀN_ID_REGION_VÀO_ĐÂY"
      priority: "High"
      dueDate: "2026-05-25T17:00:00Z"
      resourceUrl: "https://storage.manga.com/resources/raw_page1.png"
    }
  ) {
    id
    title
    description
    status
    priority
    assignedUserName
  }
}
```
*(Lưu lại `id` của Task vừa tạo)*

### Tạo ghi chú lỗi cần sửa trên trang truyện (Annotation - Đã cập nhật Phase 6):
```graphql
mutation {
  createAnnotation(
    input: {
      pageId: "ĐIỀN_ID_PAGE_VÀO_ĐÂY"
      content: "Chỉnh lại mắt bên phải hơi lệch 5 độ"
      category: ARTWORK
      shape: RECTANGLE
      x: 45.5
      y: 60.2
      width: 10.0
      height: 10.0
    }
  ) {
    id
    content
    status
    category
    shape
    x
    y
    width
    height
    createdAt
  }
}
```

### Yêu cầu Trợ lý làm lại (NeedsRevision):
```graphql
mutation {
  updateTaskStatus(
    taskId: "ĐIỀN_ID_TASK_VÀO_ĐÂY"
    input: {
      newStatus: NEEDS_REVISION
      resultNote: "Sửa lại nét vẽ mắt như ghi chú anh vừa ghi trên trang nhé."
    }
  ) {
    id
    title
    status
    resultNote
  }
}
```

### Duyệt hoàn thành Task (Done):
```graphql
mutation {
  updateTaskStatus(
    taskId: "ĐIỀN_ID_TASK_VÀO_ĐÂY"
    input: { newStatus: DONE }
  ) {
    id
    title
    status
    completedAt
  }
}
```

### Nộp bản thảo truyện lên ban biên tập (Create Submission):
```graphql
mutation {
  createSubmission(
    input: {
      seriesId: "ĐIỀN_ID_SERIES_VÀO_ĐÂY"
      title: "Chương 5: Trận chiến bắt đầu"
      note: "Bản thảo chương 5 hoàn chỉnh"
      manuscripts: [
        {
          name: "Bản thảo hoàn chỉnh"
          assetUrls: ["https://storage.manga.com/submissions/ch5_final.png"]
        }
      ]
    }
  ) {
    id
    title
    status
    seriesTitle
    submittedAt
  }
}
```

---

## 4. CÁC HOẠT ĐỘNG CỦA ASSISTANT (Token Assistant)

*Đính kèm Header:* `Authorization: Bearer <TOKEN_ASSISTANT>`

### Xem danh sách công việc của mình:
```graphql
query {
  myTasks {
    id
    title
    description
    resourceUrl
    status
    priority
    region {
      id
      name
      description
      pageId
      x
      y
      width
      height
    }
  }
}
```

### Nhận việc (Chuyển sang InProgress):
```graphql
mutation {
  updateTaskStatus(
    taskId: "ĐIỀN_ID_TASK_VÀO_ĐÂY"
    input: { newStatus: IN_PROGRESS }
  ) {
    id
    title
    status
  }
}
```

### Nộp sản phẩm để tác giả duyệt (Chuyển sang Review):
```graphql
mutation {
  updateTaskStatus(
    taskId: "ĐIỀN_ID_TASK_VÀO_ĐÂY"
    input: {
      newStatus: REVIEW
      resultFileUrl: "https://storage.manga.com/results/task_completed.psd"
      resultNote: "Em đã hoàn thành nét vẽ mắt nhân vật chính!"
    }
  ) {
    id
    title
    status
    resultFileUrl
    resultNote
  }
}
```

### Tra cứu ghi chú sửa đổi chi tiết trên trang (Đã cập nhật Phase 6):
*(Thay `pageId` lấy được từ trường `region.pageId` trong thông tin task)*
```graphql
query {
  annotationsByPage(pageId: "ĐIỀN_ID_PAGE_LẤY_ĐƯỢC_TỪ_TASK_VÀO_ĐÂY") {
    id
    content
    status
    category
    shape
    x
    y
    width
    height
    userName
    createdAt
  }
}
```

### Trợ lý theo dõi thu nhập hàng tháng:
```graphql
query {
  monthlyIncomeStats {
    year
    month
    baseSalary
    onTimeTasksCount
    totalTasksCount
    performanceRate
    performanceBonus
    totalIncome
  }
}
```

---

## 5. CÁC HOẠT ĐỘNG QUẢN LÝ GÓP Ý & XÉT DUYỆT (PHASE 5 & 6)
*Lưu ý: Yêu cầu token của role tương ứng: Tantou, Mangaka, EditorialBoard hoặc Assistant.*

### Đánh dấu Annotation đã sửa xong (Role: Mangaka, Assistant)
```graphql
mutation ResolveAnnotation {
  resolveAnnotation(annotationId: "YOUR_ANNOTATION_ID_HERE") {
    id
    status
    content
    resolvedAt
    resolvedByUserName
  }
}
```

### Duyệt / Từ chối bản sửa Annotation (Role: Tantou, EditorialBoard)

**Trường hợp 1: Chấp nhận bản sửa (Approve)**
```graphql
mutation ApproveAnnotation {
  reviewAnnotation(
    annotationId: "YOUR_ANNOTATION_ID_HERE", 
    request: {
      isApproved: true
    }
  ) {
    id
    status
    closedAt
    closedByUserName
  }
}
```

**Trường hợp 2: Bắt sửa lại (Reject + Feedback mới)**
```graphql
mutation RejectAnnotation {
  reviewAnnotation(
    annotationId: "YOUR_ANNOTATION_ID_HERE", 
    request: {
      isApproved: false,
      feedback: "Phần viền môi vẫn bị thô, hãy dùng nét thanh hơn một chút."
    }
  ) {
    id
    status
    content # Sẽ nối thêm feedback mới vào nội dung cũ
    resolvedAt # Được reset lại
  }
}
```

### Đếm số lượng Annotation chưa đóng theo Chapter (Dùng cho Dashboard)
```graphql
query GetUnresolvedCount {
  unresolvedCountByChapter(chapterId: "YOUR_CHAPTER_ID_HERE") {
    pageId
    pageNumber
    unresolvedCount
  }
}
```

### Xoá Annotation (Vai trò: Chính người tạo ra nó)
*Lưu ý: Chỉ xoá được khi status đang là OPEN.*
```graphql
mutation DeleteAnnotation {
  deleteAnnotation(annotationId: "YOUR_ANNOTATION_ID_HERE")
}
```

### Gửi duyệt bản thảo (Submit) (Role: Mangaka)
```graphql
mutation SubmitManuscript {
  submitManuscript(
    manuscriptId: "YOUR_MANUSCRIPT_ID_HERE"
    note: "Bản vẽ thô (name) đã hoàn thành, nhờ Tantou duyệt giúp."
  ) {
    id
    workflowStatus
  }
}
```

### Tantou tiếp nhận bản nộp (Accept) (Role: Tantou, Admin, Editor)
```graphql
mutation AcceptSubmission {
  acceptSubmission(submissionId: "YOUR_SUBMISSION_ID_HERE") {
    id
    workflowStatus
  }
}
```

### Tantou yêu cầu sửa lại (Return for Revision)
```graphql
mutation ReturnForRevision {
  returnForRevision(
    submissionId: "YOUR_SUBMISSION_ID_HERE"
    comment: "Nhịp truyện đoạn giữa còn hơi chậm, cần cắt bớt 1 page."
  ) {
    id
    workflowStatus
  }
}
```

### Tantou chuyển tiếp lên Hội đồng Biên tập (Forward to Board)
```graphql
mutation ForwardToBoard {
  forwardToBoard(
    submissionId: "YOUR_SUBMISSION_ID_HERE"
    comment: "Tác phẩm đạt chuẩn kỹ thuật, xin hội đồng duyệt để xuất bản."
  ) {
    id
    workflowStatus
  }
}
```

---

## 6. QUẢN LÝ TÁC PHẨM / SERIES

### Mangaka đăng ký Series mới (REST API - multipart/form-data):
Sử dụng RESTful API với định dạng `multipart/form-data` để đăng ký bộ truyện mới kèm ảnh bìa:
- **HTTP Method**: `POST`
- **Endpoint**: `https://localhost:7129/api/upload/series` (hoặc cổng HTTP tương ứng của bạn)
- **Headers**: 
  - `Authorization: Bearer <TOKEN_MANGAKA>`
  - `Content-Type: multipart/form-data`
- **Body (form-data)**:
  - `Title`: `Hành Trình Mới`
  - `Description`: `Mô tả về hành trình mới của nhân vật`
  - `AlternativeTitle`: `New Journey`
  - `File`: `[Chọn 1 File ảnh làm ảnh bìa truyện]`

#### Ví dụ cURL:
```bash
curl -X POST "https://localhost:7129/api/upload/series" \
  -H "accept: */*" \
  -H "Authorization: Bearer <TOKEN_MANGAKA>" \
  -H "Content-Type: multipart/form-data" \
  -F "Title=Hành Trình Mới" \
  -F "Description=Mô tả về hành trình mới của nhân vật" \
  -F "AlternativeTitle=New Journey" \
  -F "File=@/path/to/cover.png"
```
*(Yêu cầu đăng ký thành công sẽ tạo Series ở trạng thái `Draft` chờ Admin duyệt).*

### Admin / Editor duyệt hoặc từ chối đăng ký Series mới (GraphQL Mutation - Token Admin/Editor):
*Khi duyệt thành công: Series chuyển sang trạng thái `Ongoing` và gán Biên tập viên (Tantou Editor) phụ trách.*

```graphql
mutation ReviewSeries($request: ReviewSeriesRequestInput!) {
  reviewSeries(request: $request) {
    succeeded
    message
    errors
    data {
      id
      title
      status
      coverImageUrl
      authorId
      tantouEditorId
    }
  }
}
```

**GraphQL Variables (JSON) - Duyệt Chấp Nhận (Approve):**
```json
{
  "request": {
    "seriesId": "ĐIỀN_ID_SERIES_VỪA_TẠO_VÀO_ĐÂY",
    "isApproved": true,
    "tantouEditorId": "ĐIỀN_ID_TANTOU_EDITOR_VÀO_ĐÂY"
  }
}
```

**GraphQL Variables (JSON) - Từ Chối & Xóa Series (Reject):**
```json
{
  "request": {
    "seriesId": "ĐIỀN_ID_SERIES_VỪA_TẠO_VÀO_ĐÂY",
    "isApproved": false
  }
}
```

### Gán thứ hạng mới cho Series (Dùng trong hệ thống Ranking)
```graphql
mutation AddSeriesRanking {
  addSeriesRanking(
    seriesId: "YOUR_SERIES_ID_HERE"
    position: 1
    period: "May 2026 - Week 4"
  ) {
    seriesId
    seriesTitle
    position
    period
    calculatedAt
  }
}
```

### Kích hoạt cảnh báo khai tử (Survival Check)
*Sẽ tự động quét toàn bộ DB để tìm ra các tác phẩm rớt chót bảng 3 lần liên tiếp và phát cảnh báo.*
```graphql
mutation TriggerSurvivalCheck {
  triggerSurvivalCheck
}
```

---

## 7. TUYỂN DỤNG MANGAKA & ĐƠN ỨNG TUYỂN (CANDIDATE APPLICATION)

Mục này hướng dẫn test luồng tuyển dụng Mangaka mới. Tác giả vô danh gửi đơn ứng tuyển (kèm Series và các file Bản thảo), sau đó Admin thực hiện xét duyệt đơn.

### Ứng viên gửi đơn ứng tuyển (REST API - Public - Không cần Token):
Sử dụng RESTful API với định dạng `multipart/form-data` để gửi đơn ứng tuyển và upload nhiều file bản thảo (Assets):

- **HTTP Method**: `POST`
- **Endpoint**: `https://localhost:7129/api/candidate-application/submit` (hoặc cổng HTTP tương ứng của bạn)
- **Headers**: `Content-Type: multipart/form-data`
- **Body (form-data)**:
  - `Email`: `candidate@example.com`
  - `Password`: `Password123!`
  - `FullName`: `Nguyễn Văn Nghệ`
  - `SeriesTitle`: `Hành Trình Kỳ Thú`
  - `Files`: `[Chọn 1 hoặc nhiều File bản thảo để tải lên (ví dụ: .png, .jpg)]`

#### Ví dụ cURL:
```bash
curl -X POST "https://localhost:7129/api/candidate-application/submit" \
  -H "accept: */*" \
  -H "Content-Type: multipart/form-data" \
  -F "Email=candidate@example.com" \
  -F "Password=Password123!" \
  -F "FullName=Nguyễn Văn Nghệ" \
  -F "SeriesTitle=Hành Trình Kỳ Thú" \
  -F "Files=@/path/to/page1.png" \
  -F "Files=@/path/to/page2.png"
```

### Admin lấy danh sách đơn đang chờ duyệt (Token Admin):
```graphql
query GetPendingApplications {
  pendingApplications {
    succeeded
    message
    errors
    data {
      id
      fullName
      email
      seriesTitle
      status
      createdAt
      assets {
        id
        name
        type
        fileUrl
        size
      }
    }
  }
}
```
*(Lưu lại `id` của đơn ứng tuyển cần xử lý).*

### Admin xem chi tiết một đơn ứng tuyển (Token Admin):
```graphql
query GetApplicationById($id: UUID!) {
  applicationById(id: $id) {
    succeeded
    message
    errors
    data {
      id
      fullName
      email
      seriesTitle
      status
      reviewNote
      createdAt
      reviewedAt
      assets {
        id
        name
        type
        fileUrl
        size
        uploadedAt
      }
    }
  }
}
```

### Admin duyệt đơn ứng tuyển - Chấp nhận (Token Admin):
*Khi duyệt đơn thành công: Tài khoản được kích hoạt, gán role Mangaka, chuyển Series nháp sang trạng thái Ongoing và liên kết với Biên tập viên (Tantou Editor).*
```graphql
mutation ReviewApplication($request: ReviewApplicationRequestInput!) {
  reviewApplication(request: $request) {
    succeeded
    message
    errors
    data {
      id
      fullName
      email
      seriesTitle
      status
      reviewNote
      reviewedAt
    }
  }
}
```
**GraphQL Variables (JSON):**
```json
{
  "request": {
    "applicationId": "ĐIỀN_ID_ĐƠN_ỨNG_TUYỂN_VÀO_ĐÂY",
    "isApproved": true,
    "reviewNote": "Nét vẽ tốt, cốt truyện cuốn hút. Duyệt làm Mangaka chính thức của hệ thống.",
    "tantouEditorId": "ĐIỀN_ID_TANTOU_EDITOR_VÀO_ĐÂY"
  }
}
```

### Admin duyệt đơn ứng tuyển - Từ chối (Token Admin):
*Khi từ chối đơn: Xóa tài khoản pending và tác phẩm nháp khỏi DB, xóa file bản thảo khỏi Cloudinary và gửi email thông báo từ chối.*
```graphql
mutation ReviewApplication($request: ReviewApplicationRequestInput!) {
  reviewApplication(request: $request) {
    succeeded
    message
    errors
    data {
      id
      fullName
      email
      seriesTitle
      status
      reviewNote
      reviewedAt
    }
  }
}
```
**GraphQL Variables (JSON):**
```json
{
  "request": {
    "applicationId": "ĐIỀN_ID_ĐƠN_ỨNG_TUYỂN_VÀO_ĐÂY",
    "isApproved": false,
    "reviewNote": "Nét vẽ chưa đạt tiêu chuẩn nghệ thuật của nhà xuất bản. Hãy cố gắng luyện tập thêm."
  }
}
```
