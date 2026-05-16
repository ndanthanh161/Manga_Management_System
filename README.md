# Manga Management System (MMS)

## Giới thiệu
Hệ thống quản lý quy trình sáng tác và xuất bản Manga (Manga Creation Workflow and Publishing Management System). Dự án hỗ trợ quản lý toàn diện từ lúc nộp bản thảo, phân công trợ lý đến khi hội đồng biên tập ra quyết định xuất bản.

## Công nghệ sử dụng
- **Backend**: .NET 8, C#
- **Kiến trúc**: Clean Architecture (Domain, Application, Infrastructure, API)
- **API**: GraphQL (HotChocolate)
- **Database**: SQL Server (EF Core)
- **Authentication**: JWT & Refresh Token rotation
- **Caching**: Redis

## Các vai trò trong hệ thống (Roles)
- **Admin**: Quản trị hệ thống.
- **Mangaka**: Tác giả, quản lý series và giao việc.
- **Assistant**: Trợ lý, thực hiện các task vẽ và nộp kết quả.
- **Tantou Editor**: Biên tập viên trực tiếp, duyệt nội dung và deadline.
- **Editorial Board**: Hội đồng biên tập, quyết định lịch xuất bản và hủy series.

## Hướng dẫn chạy dự án
1. Clone dự án: `git clone https://github.com/ndanthanh161/Manga_Management_System.git`
2. Cấu hình chuỗi kết nối Database và JWT Secret trong `appsettings.Development.json`.
3. Chạy lệnh: `dotnet build`
4. Update Database: `dotnet ef database update --project src/MMS.Infrastructure --startup-project src/MMS.API`
5. Chạy ứng dụng và truy cập `/graphql` để trải nghiệm Banana Cake Pop.
