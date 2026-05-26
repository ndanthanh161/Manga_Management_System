# GraphQL Test Suite - Mangaka & Assistant Workflows

Tài liệu này viết lại test suite theo đúng các query/mutation đang expose trong `src/MMS.API/GraphQL`, đồng thời sắp xếp theo nghiệp vụ Mangaka và Assistant.

Lưu ý phạm vi:

- Không có mutation GraphQL `createSeries` hoặc `updateSeries` trong code hiện tại. Series ứng tuyển được tạo qua REST `POST /api/candidate-application/submit`; Series nội bộ thứ 2 cần được seed/tạo bằng endpoint/service hiện có trước khi test `createSubmission`.
- Form ứng tuyển public là REST multipart, không phải GraphQL.
- Các enum GraphQL thường dùng dạng `IN_PROGRESS`, `NEEDS_REVISION`, `REVIEW`, `DONE`, `APPROVED`, `REJECTED`.
- Header khi cần auth: `Authorization: Bearer <TOKEN>`.

---

## 0. Biến dùng trong quá trình test

Thay các placeholder sau bằng ID/token thực tế sau từng bước:

```text
<ADMIN_TOKEN>
<TANTOU_TOKEN>
<BOARD_TOKEN>
<MANGAKA_TOKEN>
<ASSISTANT_TOKEN>

<APPLICATION_ID>
<TANTOU_EDITOR_ID>
<MANGAKA_ID>
<ASSISTANT_ID>
<SERIES_ID>
<CHAPTER_ID>
<PAGE_ID>
<REGION_ID>
<TASK_ID>
<MANUSCRIPT_ID>
<SUBMISSION_ID>
<ANNOTATION_ID>
```

---

## 1. Auth & kiểm tra role

### 1.1. Đăng nhập

```graphql
mutation Login {
  login(request: { email: "admin@example.com", password: "Password123!" }) {
    succeeded
    message
    data {
      accessToken
      refreshToken
      id
      username
      email
      roles
      mangakaId
    }
  }
}
```

### 1.2. Kiểm tra user hiện có

```graphql
query Users {
  users {
    id
    username
    email
    roles
    isActive
    mangakaId
    baseSalary
  }
}
```

### 1.3. Kiểm tra chi tiết một user

```graphql
query UserById {
  userById(id: "<MANGAKA_ID>") {
    succeeded
    message
    data {
      id
      username
      email
      roles
      isActive
      mangakaId
    }
  }
}
```

---

## 2. Luồng public: Ứng viên gửi bản thảo để trở thành Mangaka

### 2.1. Ứng viên gửi form ứng tuyển

REST public, không cần token.

```bash
curl -X POST "https://localhost:7129/api/candidate-application/submit" \
  -H "accept: */*" \
  -F "Email=candidate01@example.com" \
  -F "Password=Password123!" \
  -F "FullName=Nguyen Van Sang" \
  -F "SeriesTitle=Moonlit Ink" \
  -F "Files=@D:/test-files/storyboard-page-01.png" \
  -F "Files=@D:/test-files/storyboard-page-02.png"
```

Kỳ vọng:

- Tạo `CandidateApplication` trạng thái `Pending`.
- Tạo user pending/inactive, chưa có role Mangaka.
- Tạo Series nháp liên kết với hồ sơ.
- Trả về `data.id`; lưu lại thành `<APPLICATION_ID>`.

### 2.2. Admin xem danh sách đơn chờ duyệt

Header: `<ADMIN_TOKEN>`.

```graphql
query PendingApplications {
  pendingApplications {
    succeeded
    message
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
        uploadedAt
      }
    }
  }
}
```

### 2.3. Admin xem chi tiết đơn ứng tuyển

Header: `<ADMIN_TOKEN>`.

```graphql
query ApplicationById {
  applicationById(id: "<APPLICATION_ID>") {
    succeeded
    message
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
        publicId
        size
      }
    }
  }
}
```

### 2.4A. Admin từ chối đơn ứng tuyển

Header: `<ADMIN_TOKEN>`.

```graphql
mutation RejectApplication {
  reviewApplication(
    request: {
      applicationId: "<APPLICATION_ID>"
      isApproved: false
      reviewNote: "Storyboard còn thiếu nhịp kể chuyện và bố cục panel chưa rõ."
    }
  ) {
    succeeded
    message
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

Kỳ vọng:

- `status = Rejected`.
- User pending và Series nháp bị xóa/ẩn theo service hiện tại.
- Ứng viên không login được.

### 2.4B. Admin chấp nhận đơn ứng tuyển

Header: `<ADMIN_TOKEN>`.

```graphql
mutation ApproveApplication {
  reviewApplication(
    request: {
      applicationId: "<APPLICATION_ID>"
      isApproved: true
      reviewNote: "Nét vẽ tốt, tiềm năng thương mại rõ. Duyệt làm Mangaka chính thức."
      tantouEditorId: "<TANTOU_EDITOR_ID>"
    }
  ) {
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

Kỳ vọng:

- `CandidateApplication.status = Approved`.
- User được active và có role `Mangaka`.
- Series chuyển `Ongoing` và có `TantouEditorId`.
- Hệ thống tạo Chapter 1 và Manuscript từ assets ứng tuyển.
- Ứng viên có thể login bằng email/password đã gửi.

### 2.5. Mangaka mới đăng nhập sau khi được duyệt

```graphql
mutation LoginApprovedMangaka {
  login(request: { email: "candidate01@example.com", password: "Password123!" }) {
    succeeded
    message
    data {
      accessToken
      id
      username
      email
      roles
      mangakaId
    }
  }
}
```

### 2.6. Luồng mời và Đăng ký thành viên (Invite & Register)

Luồng này cho phép Admin mời Mangaka trực tiếp, hoặc Mangaka mời các Trợ lý (Assistant) của mình tham gia vào hệ thống.

#### 2.6.1. Admin/Mangaka gửi thư mời (Invite User)

Header: `<ADMIN_TOKEN>` (để mời Mangaka) hoặc `<MANGAKA_TOKEN>` (để mời Assistant).

```graphql
mutation InviteUser {
  inviteUser(request: {
    email: "assistant1_v2@example.com",
    roleName: "Assistant"   # Hoặc "Mangaka" (chỉ Admin mới được mời Mangaka)
  }) {
    succeeded
    message
    errors
    data {
      email
      token
      roleName
    }
  }
}
```

Kỳ vọng:
- Hệ thống ghi nhận thư mời ở trạng thái chờ kích hoạt.
- Trả về lời mời thành công kèm `token` mời độc nhất. Lưu lại làm `<INVITE_TOKEN>`.

#### 2.6.2. Đăng ký tài khoản qua Token mời (Register by Invite Token)

Không cần gửi Header Authorization (Công khai).

```graphql
mutation RegisterViaInvite {
  register(request: {
    token: "<INVITE_TOKEN>",
    username: "TestAssistantV2",
    password: "Password123!"
  }) {
    succeeded
    message
    errors
    data {
      accessToken
      refreshToken
      id
      username
      email
      roles
      mangakaId
    }
  }
}
```

Kỳ vọng:
- Tạo tài khoản thành công cho người được mời.
- Tài khoản được gán đúng Vai trò (`roles` như `Assistant` hoặc `Mangaka`) tương ứng với thư mời ban đầu.
- Trả về `accessToken` để đăng nhập hoạt động.

---

## 3. Mangaka: quản lý studio và tạo submission nội bộ

Header mặc định: `<MANGAKA_TOKEN>`.

### 3.1. Xem danh sách Series của Mangaka

```graphql
query MySeries {
  mySeries(mangakaId: "<MANGAKA_ID>") {
    id
    title
    alternativeTitle
    description
    coverImageUrl
    status
    authorId
    authorName
    createdAt
    updatedAt
  }
}
```

### 3.2. Xem chi tiết Series

```graphql
query SeriesById {
  seriesById(id: "<SERIES_ID>") {
    id
    title
    alternativeTitle
    description
    coverImageUrl
    status
    authorId
    authorName
    createdAt
    updatedAt
  }
}
```

### 3.3. Luồng Mangaka nội bộ tạo bộ truyện thứ 2

Hiện chưa có GraphQL mutation tạo Series. Test setup cần tạo `<SERIES_ID>` bằng một trong các cách ngoài GraphQL hiện có trong môi trường test:

- Seed trực tiếp DB với `AuthorId = <MANGAKA_ID>`, `Status = Draft`.
- Gọi service/endpoint upload-cover nếu project test có expose.
- Dùng dữ liệu Series đã có của Mangaka.

Sau khi có `<SERIES_ID>`, Mangaka nộp bản thảo nội bộ:

```graphql
mutation CreateInternalSubmission {
  createSubmission(
    input: {
      seriesId: "<SERIES_ID>"
      title: "Pitch bộ truyện thứ 2 - Chapter 0"
      note: "Bản pitch nội bộ, gửi hội đồng phân công Tantou và duyệt lên Ongoing."
      manuscripts: [
        {
          name: "Storyboard pitch nội bộ"
          assetUrls: [
            "https://storage.example.com/internal-pitch/page-01.png",
            "https://storage.example.com/internal-pitch/page-02.png"
          ]
        }
      ]
    }
  ) {
    id
    title
    note
    status
    seriesId
    seriesTitle
    mangakaId
    mangakaName
    assignedTantouId
    submittedAt
    manuscripts {
      id
      name
      version
      status
      isCurrentVersion
      chapterId
      createdAt
    }
  }
}
```

Kỳ vọng:

- Tạo `Submission` cho Series nội bộ.
- Vì Series mới chưa có Tantou, submission đi vào inbox Admin/Editorial Board theo service hiện tại.
- Lưu `id` thành `<SUBMISSION_ID>`.

### 3.4. Xem submission của Mangaka

```graphql
query MySubmissions {
  mySubmissions(mangakaId: "<MANGAKA_ID>") {
    id
    title
    note
    status
    seriesId
    seriesTitle
    mangakaId
    mangakaName
    assignedTantouId
    assignedTantouName
    submittedAt
    resolvedAt
  }
}
```

### 3.5. Tạo Chapter chính thức

```graphql
mutation CreateChapter {
  createChapter(
    input: {
      seriesId: "<SERIES_ID>"
      title: "Chapter 1: Loi mo dau"
      chapterNumber: 1
    }
  ) {
    id
    title
    chapterNumber
    isPublished
    createdAt
    seriesId
  }
}
```

### 3.6. Tạo Page bản thảo thô

```graphql
mutation CreatePage {
  createPage(
    input: {
      chapterId: "<CHAPTER_ID>"
      pageNumber: 1
      imageUrl: "https://storage.example.com/pages/chapter-1-page-1-rough.png"
    }
  ) {
    id
    pageNumber
    imageUrl
    chapterId
    version
    isCurrentVersion
    width
    height
    fileSizeBytes
    createdAt
  }
}
```

### 3.7. Tạo Region bằng tọa độ phần trăm

```graphql
mutation CreateRegion {
  createRegion(
    input: {
      pageId: "<PAGE_ID>"
      name: "Panel 01 - Nen lau dai"
      description: "Vung nen can ve chi tiet kien truc lau dai."
      x: 8.5
      y: 12.0
      width: 42.0
      height: 30.5
    }
  ) {
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
```

Kỳ vọng:

- `x/y/width/height` lưu theo tỉ lệ phần trăm.
- Lưu `id` thành `<REGION_ID>`.

### 3.8. Xem Region của Page

```graphql
query RegionsByPage {
  regionsByPage(pageId: "<PAGE_ID>") {
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
```

### 3.9. Xem danh sách Assistant của Mangaka

```graphql
query MyAssistants {
  myAssistants {
    id
    username
    email
    roles
    mangakaId
    baseSalary
  }
}
```

### 3.10. Thiết lập lương cứng cho Assistant

```graphql
mutation SetAssistantBaseSalary {
  setAssistantBaseSalary(assistantId: "<ASSISTANT_ID>", baseSalary: 6000000) {
    succeeded
    message
    errors
    data {
      id
      username
      email
      roles
      mangakaId
      baseSalary
    }
  }
}
```

### 3.11. Giao WorkTask cho Assistant

```graphql
mutation AssignTask {
  assignTask(
    input: {
      title: "Ve nen lau dai cho Panel 01"
      description: "Hoan thien background lau dai, giu dung phoi canh va mood u toi."
      assistantId: "<ASSISTANT_ID>"
      regionId: "<REGION_ID>"
      priority: "High"
      dueDate: "2026-06-30T17:00:00Z"
      resourceUrl: "https://storage.example.com/resources/castle-reference-pack.zip"
      reward: 750000
    }
  ) {
    id
    title
    description
    status
    priority
    dueDate
    assignedUserId
    assignedUserName
    regionId
    pageImageUrl
    resourceUrl
    reward
    createdAt
    region {
      id
      name
      x
      y
      width
      height
    }
  }
}
```

Kỳ vọng:

- `status = Todo`.
- Task gắn đúng `assistantId`, `regionId`, `reward`.
- Lưu `id` thành `<TASK_ID>`.

---

## 4. Assistant: nhận việc, làm việc, nộp kết quả

Header mặc định: `<ASSISTANT_TOKEN>`.

### 4.1. Xem danh sách task được giao

```graphql
query MyTasks {
  myTasks {
    id
    title
    description
    status
    priority
    dueDate
    assignedUserId
    assignedUserName
    regionId
    pageImageUrl
    resourceUrl
    resultFileUrl
    resultNote
    reward
    completedAt
    createdAt
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

Kỳ vọng:

- Danh sách task của Assistant được sắp xếp/phục vụ theo priority và due date nếu service hỗ trợ.
- Task hiển thị `region` để UI tô sáng đúng vùng cần xử lý.
- `resourceUrl` dùng để tải tài nguyên hỗ trợ.

### 4.2. Assistant bắt đầu làm việc

```graphql
mutation StartTask {
  updateTaskStatus(
    taskId: "<TASK_ID>"
    input: { newStatus: IN_PROGRESS }
  ) {
    id
    title
    status
    resultFileUrl
    resultNote
    reward
  }
}
```

Kỳ vọng: `status = InProgress`.

### 4.3. Assistant nộp sản phẩm hoàn thiện

```graphql
mutation SubmitTaskResult {
  updateTaskStatus(
    taskId: "<TASK_ID>"
    input: {
      newStatus: REVIEW
      resultFileUrl: "https://storage.example.com/results/panel-01-castle-bg-final.psd"
      resultNote: "Em da hoan thien nen lau dai va can lai tuong phan theo storyboard."
    }
  ) {
    id
    title
    status
    resultFileUrl
    resultNote
    completedAt
  }
}
```

Kỳ vọng:

- `status = Review`.
- Mangaka nhận notification theo nghiệp vụ/service tích hợp.

### 4.4. Assistant xem ghi chú chỉnh sửa trên Page

```graphql
query AnnotationsByPage {
  annotationsByPage(pageId: "<PAGE_ID>") {
    id
    pageId
    userId
    userName
    content
    category
    shape
    x
    y
    width
    height
    strokeData
    status
    resolvedAt
    resolvedByUserName
    closedAt
    closedByUserName
    createdAt
  }
}
```

### 4.5. Assistant đánh dấu đã xử lý annotation

```graphql
mutation ResolveAnnotation {
  resolveAnnotation(annotationId: "<ANNOTATION_ID>") {
    id
    status
    content
    resolvedAt
    resolvedByUserId
    resolvedByUserName
  }
}
```

---

## 5. Mangaka: duyệt hoặc yêu cầu sửa task của Assistant

Header mặc định: `<MANGAKA_TOKEN>`.

### 5.1. Mangaka tạo annotation trực quan khi cần sửa

```graphql
mutation CreateRevisionAnnotation {
  createAnnotation(
    input: {
      pageId: "<PAGE_ID>"
      content: "Tang do sau bong do o chan cot, diem sang hien dang phang."
      category: ARTWORK
      shape: RECTANGLE
      x: 22.5
      y: 31.2
      width: 12.0
      height: 8.0
      strokeData: null
    }
  ) {
    id
    pageId
    userId
    userName
    content
    category
    shape
    x
    y
    width
    height
    status
    createdAt
  }
}
```

### 5.2A. Mangaka yêu cầu Assistant sửa lại

```graphql
mutation RequestTaskRevision {
  updateTaskStatus(
    taskId: "<TASK_ID>"
    input: {
      newStatus: NEEDS_REVISION
      resultNote: "Can sua theo annotation tren Panel 01, uu tien bong do chan cot va texture da."
    }
  ) {
    id
    title
    status
    resultFileUrl
    resultNote
    completedAt
  }
}
```

Kỳ vọng:

- `status = NeedsRevision`.
- Task quay lại danh sách công việc của Assistant kèm ghi chú.

### 5.2B. Mangaka phê duyệt task

```graphql
mutation ApproveTask {
  updateTaskStatus(
    taskId: "<TASK_ID>"
    input: { newStatus: DONE }
  ) {
    id
    title
    status
    reward
    completedAt
  }
}
```

Kỳ vọng:

- `status = Done`.
- `completedAt` có giá trị.
- Reward được tính vào báo cáo thu nhập tháng của Assistant theo service hiện tại.

---

## 6. Assistant: dashboard thu nhập tháng

Header: `<ASSISTANT_TOKEN>`.

```graphql
query MonthlyIncomeStats {
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

Kỳ vọng:

- Tháng hiện tại liệt kê các task `Done`.
- `totalIncome` phản ánh lương cứng/bonus/reward theo logic service.
- Đây là dữ liệu nền để UI export PDF báo cáo thanh toán.

---

## 7. Tantou / Editorial Board: xét duyệt submission

### 7.1. Tantou/Admin/Board xem inbox submission

Header: `<ADMIN_TOKEN>`, `<TANTOU_TOKEN>` hoặc `<BOARD_TOKEN>`.

```graphql
query SubmissionInbox {
  submissionInbox(filter: SUBMITTED) {
    id
    title
    note
    status
    seriesId
    seriesTitle
    mangakaId
    mangakaName
    assignedTantouId
    assignedTantouName
    submittedAt
    resolvedAt
  }
}
```

### 7.2. Tantou tiếp nhận thẩm định

Header: `<TANTOU_TOKEN>`.

```graphql
mutation AcceptSubmission {
  acceptSubmission(submissionId: "<SUBMISSION_ID>") {
    id
    title
    status
    assignedTantouId
    assignedTantouName
  }
}
```

### 7.3. Tantou yêu cầu sửa submission

Header: `<TANTOU_TOKEN>`.

```graphql
mutation ReturnForRevision {
  returnForRevision(
    submissionId: "<SUBMISSION_ID>"
    comment: "Can rut gon phan exposition va them cao trao ro hon o 5 trang dau."
  ) {
    id
    title
    status
    resolvedAt
  }
}
```

### 7.4. Tantou chuyển submission lên hội đồng

Header: `<TANTOU_TOKEN>`.

```graphql
mutation ForwardToBoard {
  forwardToBoard(
    submissionId: "<SUBMISSION_ID>"
    comment: "Ban thao dat yeu cau ve hinh anh va co tiem nang thuong mai."
  ) {
    id
    title
    status
    assignedTantouId
    assignedTantouName
  }
}
```

### 7.5. Thành viên hội đồng bỏ phiếu

Header: `<BOARD_TOKEN>`.

```graphql
mutation VoteSubmission {
  voteSubmission(
    submissionId: "<SUBMISSION_ID>"
    approve: true
    note: "Dong y duyet, concept ro va art direction on dinh."
  ) {
    id
    title
    status
    votes {
      id
      boardMemberId
      boardMemberName
      voteType
      comment
    }
  }
}
```

### 7.6A. Hội đồng phê duyệt cuối cùng

Header: `<BOARD_TOKEN>` hoặc `<ADMIN_TOKEN>`.

```graphql
mutation FinalizeApproved {
  finalizeBoardDecision(
    submissionId: "<SUBMISSION_ID>"
    decision: APPROVED
    comment: "Hoi dong thong qua. Series duoc dua vao ke hoach phat hanh."
  ) {
    id
    title
    status
    resolvedAt
    transitions {
      fromStatus
      toStatus
      comment
      occurredAt
    }
  }
}
```

Kỳ vọng:

- `Submission.status = Approved`.
- Với luồng pitch được duyệt, Series liên quan được đưa lên `Ongoing` theo service hiện tại.

### 7.6B. Hội đồng từ chối cuối cùng

Header: `<BOARD_TOKEN>` hoặc `<ADMIN_TOKEN>`.

```graphql
mutation FinalizeRejected {
  finalizeBoardDecision(
    submissionId: "<SUBMISSION_ID>"
    decision: REJECTED
    comment: "Chua dat yeu cau ve cau truc kich ban, de nghi phat trien lai premise."
  ) {
    id
    title
    status
    resolvedAt
  }
}
```

Kỳ vọng:

- `Submission.status = Rejected`.
- Mangaka nhận feedback; role Mangaka hiện hữu không bị thay đổi trong luồng nội bộ.

### 7.7. Xem chi tiết submission

```graphql
query SubmissionById {
  submissionById(id: "<SUBMISSION_ID>") {
    id
    title
    note
    status
    seriesId
    seriesTitle
    mangakaId
    mangakaName
    assignedTantouId
    assignedTantouName
    submittedAt
    resolvedAt
    manuscripts {
      id
      name
      version
      status
      createdAt
      isCurrentVersion
      chapterId
    }
    votes {
      id
      boardMemberId
      boardMemberName
      voteType
      comment
    }
    transitions {
      fromStatus
      toStatus
      comment
      occurredAt
    }
  }
}
```

### 7.8. Xem lịch sử workflow submission

```graphql
query SubmissionWorkflowHistory {
  submissionWorkflowHistory(submissionId: "<SUBMISSION_ID>") {
    id
    fromStatus
    toStatus
    comment
    occurredAt
  }
}
```

---

## 8. Survival Tracker: ranking và cảnh báo hủy bỏ

### 8.1. Admin/Editor thêm ranking cho Series

Header: `<ADMIN_TOKEN>` hoặc `<TANTOU_TOKEN>` nếu user có role `Editor`.

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
mutation AddSeriesRankingWeek1 {
  addSeriesRanking(
    seriesId: "<SERIES_ID>"
    position: 18
    period: "2026-W23"
  ) {
    seriesId
    seriesTitle
    position
    period
    calculatedAt
  }
}
```

Chạy thêm 2 lần cho 3 kỳ liên tiếp:

```graphql
mutation AddSeriesRankingWeek2 {
  addSeriesRanking(
    seriesId: "<SERIES_ID>"
    position: 19
    period: "2026-W24"
  ) {
    seriesId
    seriesTitle
    position
    period
    calculatedAt
  }
}
```

```graphql
mutation AddSeriesRankingWeek3 {
  addSeriesRanking(
    seriesId: "<SERIES_ID>"
    position: 20
    period: "2026-W25"
  ) {
    seriesId
    seriesTitle
    position
    period
    calculatedAt
  }
}
```

### 8.2. Mangaka xem lịch sử ranking

```graphql
query SeriesRankings {
  seriesRankings(seriesId: "<SERIES_ID>") {
    seriesId
    seriesTitle
    position
    period
    calculatedAt
  }
}
```

### 8.3. Admin/Editor kích hoạt survival check

Header: `<ADMIN_TOKEN>` hoặc token có role `Editor`.

```graphql
mutation TriggerSurvivalCheck {
  triggerSurvivalCheck
}
```

Kỳ vọng:

- Trả về `true`.
- Nếu Series nằm bottom 3 trong 3 kỳ mới nhất, service gửi email và tạo notification cảnh báo cho Mangaka.

---

## 9. Manuscript versioning cho Chapter

Header: `<MANGAKA_TOKEN>`.

### 9.1. Snapshot Chapter thành Manuscript

```graphql
mutation SnapshotChapter {
  snapshotChapter(
    input: {
      chapterId: "<CHAPTER_ID>"
      name: "Chapter 1 storyboard snapshot v1"
    }
  ) {
    succeeded
    message
    errors
    data {
      id
      name
      version
      chapterId
      status
      isCurrentVersion
      createdAt
    }
  }
}
```

### 9.2. Tạo version mới

```graphql
mutation CreateNewVersion {
  createNewVersion(
    input: { currentManuscriptId: "<MANUSCRIPT_ID>" }
  ) {
    succeeded
    message
    errors
    data {
      id
      name
      version
      chapterId
      status
      isCurrentVersion
      createdAt
    }
  }
}
```

### 9.3. Revert về version cũ

```graphql
mutation RevertToVersion {
  revertToVersion(versionId: "<MANUSCRIPT_ID>") {
    succeeded
    message
    errors
    data {
      id
      name
      version
      chapterId
      status
      isCurrentVersion
      createdAt
    }
  }
}
```

### 9.4. Xem lịch sử version

```graphql
query VersionHistory {
  versionHistory(manuscriptId: "<MANUSCRIPT_ID>") {
    succeeded
    message
    data {
      id
      name
      version
      chapterId
      status
      isCurrentVersion
      createdAt
    }
  }
}
```

### 9.5. Xem chi tiết Manuscript

```graphql
query ManuscriptById {
  manuscriptById(id: "<MANUSCRIPT_ID>") {
    succeeded
    message
    data {
      id
      name
      version
      chapterId
      status
      isCurrentVersion
      createdAt
    }
  }
}
```

### 9.6. Submit Manuscript cho Tantou/Board

```graphql
mutation SubmitManuscript {
  submitManuscript(
    manuscriptId: "<MANUSCRIPT_ID>"
    note: "Chapter 1 da hoan thien, nho Tantou review giup."
  ) {
    id
    title
    note
    status
    seriesId
    seriesTitle
    mangakaId
    mangakaName
    submittedAt
    manuscripts {
      id
      name
      version
      status
    }
  }
}
```

---

## 10. Chapter, Page và Annotation queries bổ sung

### 10.1. Xem chapter theo Series

```graphql
query ChaptersBySeries {
  chaptersBySeries(seriesId: "<SERIES_ID>") {
    id
    title
    chapterNumber
    isPublished
    createdAt
    seriesId
  }
}
```

### 10.2. Xem page theo Chapter

```graphql
query PagesByChapter {
  pagesByChapter(chapterId: "<CHAPTER_ID>") {
    id
    pageNumber
    imageUrl
    chapterId
    version
    isCurrentVersion
    width
    height
    fileSizeBytes
    createdAt
  }
}
```

### 10.3. Xem lịch sử page

```graphql
query PageHistory {
  pageHistory(chapterId: "<CHAPTER_ID>", pageNumber: 1) {
    succeeded
    message
    data {
      id
      pageNumber
      imageUrl
      chapterId
      version
      isCurrentVersion
      width
      height
      fileSizeBytes
      createdAt
    }
  }
}
```

### 10.4. Đếm annotation chưa xử lý theo Chapter

```graphql
query UnresolvedCountByChapter {
  unresolvedCountByChapter(chapterId: "<CHAPTER_ID>") {
    pageId
    pageNumber
    unresolvedCount
  }
}
```

### 10.5. Tantou/EditorialBoard review annotation đã resolve

Header: `<TANTOU_TOKEN>` hoặc `<BOARD_TOKEN>`.

```graphql
mutation ReviewAnnotationApproved {
  reviewAnnotation(
    annotationId: "<ANNOTATION_ID>"
    request: { isApproved: true }
  ) {
    id
    status
    content
    resolvedAt
    closedAt
    closedByUserName
  }
}
```

```graphql
mutation ReviewAnnotationRejected {
  reviewAnnotation(
    annotationId: "<ANNOTATION_ID>"
    request: {
      isApproved: false
      feedback: "Van thieu texture da o vung chan cot, can xu ly them."
    }
  ) {
    id
    status
    content
    resolvedAt
    closedAt
  }
}
```

### 10.6. Xóa annotation đang Open

Header: user tạo annotation hoặc user được service cho phép.

```graphql
mutation DeleteAnnotation {
  deleteAnnotation(annotationId: "<ANNOTATION_ID>")
}
```

---

## 11. Negative tests theo role

Các case này dùng để xác nhận authorization và ranh giới nghiệp vụ.

### 11.1. Candidate chưa được duyệt không login được

```graphql
mutation LoginPendingCandidate {
  login(request: { email: "candidate01@example.com", password: "Password123!" }) {
    succeeded
    message
    errors
    data {
      accessToken
      roles
    }
  }
}
```

Kỳ vọng: login thất bại khi hồ sơ còn `Pending` hoặc đã `Rejected`.

### 11.2. Assistant không được giao task

Header: `<ASSISTANT_TOKEN>`.

```graphql
mutation AssistantCannotAssignTask {
  assignTask(
    input: {
      title: "Invalid assign"
      description: "Assistant khong duoc giao viec cho nguoi khac."
      assistantId: "<ASSISTANT_ID>"
      regionId: "<REGION_ID>"
      priority: "Low"
      reward: 100000
    }
  ) {
    id
    status
  }
}
```

Kỳ vọng: bị chặn bởi role, vì `assignTask` chỉ cho `Mangaka`.

### 11.3. Mangaka không được xem dashboard thu nhập của Assistant qua `monthlyIncomeStats`

Header: `<MANGAKA_TOKEN>`.

```graphql
query MangakaCannotReadAssistantIncome {
  monthlyIncomeStats {
    year
    month
    totalIncome
  }
}
```

Kỳ vọng: bị chặn bởi role, vì query chỉ cho `Assistant`.

### 11.4. Non-admin không được duyệt CandidateApplication

Header: `<MANGAKA_TOKEN>` hoặc `<TANTOU_TOKEN>`.

```graphql
mutation NonAdminCannotReviewApplication {
  reviewApplication(
    request: {
      applicationId: "<APPLICATION_ID>"
      isApproved: true
      reviewNote: "Khong hop le"
      tantouEditorId: "<TANTOU_EDITOR_ID>"
    }
  ) {
    succeeded
    message
  }
}
```

Kỳ vọng: bị chặn bởi role, vì `reviewApplication` chỉ cho `Admin`.

### 11.5. Mangaka nội bộ không đi qua luồng CandidateApplication cho bộ thứ 2

Không gọi `POST /api/candidate-application/submit` cho Mangaka đã có tài khoản nội bộ. Luồng đúng là tạo Series nội bộ bằng setup hiện có, sau đó gọi `createSubmission`.

Kỳ vọng:

- Không yêu cầu email/password mới.
- Không tạo user pending mới.
- Submission đi vào Admin/Editorial Board pool nếu Series chưa có Tantou.
