# Manga Management System - PROJECT PLAN

Hệ thống quản lý quy trình sáng tác và xuất bản Manga (Manga Creation Workflow and Publishing Management System).

---

## 1. Kiến trúc đề xuất (Architecture)
Hệ thống sử dụng **Clean Architecture** (Domain, Application, Infrastructure, API) với sự kết hợp:
*   **GraphQL (HotChocolate)**: API chính cho Frontend, tối ưu cho dữ liệu lồng nhau phức tạp.
*   **gRPC**: Dùng cho giao tiếp nội bộ hiệu năng cao và tích hợp AI (Tô màu, Phân vùng).
*   **Redis**: Làm Cache và Blacklist Token.

---

## 2. Luồng nghiệp vụ (Business Flows)

### Luồng 1: Khởi tạo Series (Mangaka & Board)
- Mangaka nộp bản thảo sơ bộ (`Manuscript`).
- Editorial Board thực hiện Vote phê duyệt lịch xuất bản.

### Luồng 2: Quy trình Studio (Mangaka & Assistant)
- Mangaka giao việc cụ thể trên từng vùng (`Region`) của `Page`.
- Assistant thực hiện Task và nộp lại `Submission`.
- Mangaka phê duyệt hoặc yêu cầu chỉnh sửa trực tiếp trên trang truyện.

### Luồng 3: Biên tập & Ra quyết định (Tantou Editor & Board)
- Editor theo dõi tiến độ thời gian thực để đảm bảo deadline bản in.
- Board dựa trên dữ liệu bình chọn (`ReaderVote`) để tính toán `Ranking` và quyết định tương lai của Series (Tiếp tục hoặc Hủy).

---

## 3. Điều chỉnh Database (Entity Refinement)

| Entity | Trạng thái | Ghi chú bổ sung |
| :--- | :--- | :--- |
| **User/Role** | Đã xong | Đã thêm 5 Role chuyên biệt. |
| **Series** | Cần cập nhật | Thêm Status (Active/Cancelled) & Schedule. |
| **Page** | Cần cập nhật | Thêm ImageUrl & danh sách Annotation. |
| **Task** | Cần cập nhật | Thêm RegionData (JSON tọa độ) & Reward. |
| **Ranking** | Cần tạo mới | Lưu lịch sử thứ hạng theo tuần/tháng. |
| **ReaderVote** | Cần tạo mới | Dữ liệu bình chọn thô từ độc giả. |

---

## 4. Lộ trình triển khai (Roadmap)

1.  **Giai đoạn 1**: Hoàn thiện toàn bộ Domain POCO và Migration Database.
2.  **Giai đoạn 2**: Xây dựng Core Service xử lý Task và Series (Logic giao việc, duyệt bài).
3.  **Giai đoạn 3**: Mở rộng GraphQL Mutations cho tất cả các Actor.
4.  **Giai đoạn 4**: Tích hợp gRPC cho AI Service (Simulation mode).
5.  **Giai đoạn 5**: Hoàn thiện hệ thống thông báo (Notifications) và báo cáo thu nhập.

---
**Ghi chú**: Mọi logic mapping đều tuân thủ SOLID và sử dụng Mapster để bảo mật thông tin nhạy cảm.
