# Hướng Dẫn Chạy Test GraphQL (Manga Management System)

Tài liệu này chứa toàn bộ các câu lệnh GraphQL (Queries & Mutations) đã được tối ưu hóa dạng **inline** và cập nhật theo cơ chế phân quyền mới nhất (đã loại bỏ `payout`).

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
    errors
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
    errors
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
  login(request: { username: "mangaka_test", password: "Password123!" }) {
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
*(Copy `accessToken` đính kèm vào Header `Authorization: Bearer <TOKEN_MANGAKA>` khi gọi các lệnh ở Phần 3)*

### Đăng nhập Assistant:
```graphql
mutation {
  login(request: { username: "assistant_test", password: "Password123!" }) {
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

### Tạo ghi chú lỗi cần sửa trên trang truyện (Annotation):
```graphql
mutation {
  createAnnotation(
    input: {
      pageId: "ĐIỀN_ID_PAGE_VÀO_ĐÂY"
      content: "Chỉnh lại mắt bên phải hơi lệch 5 độ"
      x: 45.5
      y: 60.2
    }
  ) {
    id
    content
    x
    y
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

### Tra cứu ghi chú sửa đổi chi tiết trên trang:
*(Thay `pageId` lấy được từ trường `region.pageId` trong thông tin task)*
```graphql
query {
  getAnnotationsByPage(pageId: "ĐIỀN_ID_PAGE_LẤY_ĐƯỢC_TỪ_TASK_VÀO_ĐÂY") {
    id
    content
    x
    y
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
