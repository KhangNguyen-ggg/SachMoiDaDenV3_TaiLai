# Sách Mới Đã Đến — dữ liệu SQL và ảnh bìa

Ứng dụng ASP.NET Core MVC .NET 8 dùng SQL Server. Toàn bộ thông tin sách, chương, tài khoản, đánh giá, lượt đọc, bình luận, tủ sách và diễn đàn được truy vấn/lưu trong database được cấu hình trong `ConnectionStrings:SachMoiDaDen` (repo hiện chọn `SachMoiDaDen`). Bộ truyện, tài khoản và lệnh nạp mẫu đã được bỏ; khởi động ứng dụng không tự thêm nội dung.

## Database đang có dữ liệu

1. Giữ nguyên database hiện tại và thư mục `App_Data` khi thay mã nguồn.
2. Trong SSMS, chạy `Database/02_ThemHinhAnhBia.sql` trên database `SachMoiDaDen` có schema V3. Script thêm cột, không tạo lại bảng; chạy lại được.
3. Sửa `ConnectionStrings:SachMoiDaDen` trong `appsettings.json` theo SQL Server của bạn. Mặc định: `.\SQLEXPRESS`, Windows Authentication.
4. Mở `sachmoidaden.sln` trong Visual Studio 2022, Restore rồi chạy F5.

Các script nâng cấp 02 và 03 đang chọn database `SachMoiDaDen`, khớp với cấu hình hiện tại của repo. Nếu database của bạn dùng tên khác, thay dòng `USE [SachMoiDaDen]` trong script và `Database=` trong chuỗi kết nối cho cùng tên.

Dữ liệu mẫu đã nạp ở bản cũ vẫn thuộc database hiện tại. `Database/03_AnTruyenMauCu.sql` là tùy chọn để ẩn các sách khớp chính xác tên, tác giả, giới thiệu và email người đăng của bộ mẫu cũ. Script không xóa dòng, chương hoặc tệp; sách đã chỉnh các thông tin trên hoặc đã có bìa được giữ lại. Kiểm tra các chương đã chỉnh trước khi chạy; có thể khôi phục sách trong Quản lý. Bình luận, bài thảo luận và tài khoản mẫu cũ được quản lý riêng qua giao diện. Lệnh này không tự chạy khi mở web.

## Database mới

1. Cài SQL Server/Express, .NET 8 SDK và workload ASP.NET trong Visual Studio 2022.
2. Chạy toàn bộ `Database/01_SachMoiDaDenV3.sql` trong SSMS. Script tạo 16 bảng, các vai trò và danh mục thể loại; chưa có tài khoản hoặc sách.
3. Kiểm tra chuỗi kết nối chọn database `SachMoiDaDen` do script 01 tạo, rồi mở PowerShell tại thư mục project:

```powershell
.\scripts\Create-FirstAdmin.ps1
```

Nhập email, tên và mật khẩu riêng. Script chỉ tạo Admin đầu tiên khi chưa có Admin đang hoạt động. Database cũ đã có Admin thì dùng tài khoản hiện có để phân quyền cho tài khoản riêng của bạn.

```powershell
dotnet restore
dotnet build -c Release
dotnet run --launch-profile http
```

Địa chỉ mặc định: http://localhost:5010. Khi database trống, giao diện hiển thị trạng thái chưa có nội dung. Thông tin sách thật cần được đăng qua giao diện hoặc nhập vào SQL; ứng dụng không tự bịa thông tin sách.

## Đăng sách và ảnh bìa

- Admin hoặc Contributor vào **Quản lý → Thêm sách**, nhập thông tin sách, thể loại và chọn ảnh bìa.
- Nhận JPG/JPEG, PNG, WebP, tối đa 5 MiB; kiểm tra nội dung tệp phía server.
- Lưu thông tin sách rồi thêm chương bằng văn bản hoặc tải tệp/ảnh như trước.
- Contributor chờ duyệt; Admin/Moderator kiểm duyệt trước khi nội dung công khai.
- Khi sửa sách, không chọn ảnh sẽ giữ bìa cũ. Chọn ảnh mới để thay, hoặc chọn **Xóa ảnh bìa hiện tại**. Không chọn hai thao tác cùng lúc.
- Ảnh xuất hiện ở thẻ sách, trang chi tiết, tủ sách và trang xem trước. Sách chưa có ảnh hiển thị “Chưa có ảnh bìa”.

`dbo.Sach.HinhAnhBia` có kiểu `varbinary(max)` chứa bytes ảnh; `KieuAnhBia` có kiểu `varchar(20)` chứa MIME. Cả hai nullable để nâng cấp sách cũ. Ảnh lưu cùng thông tin sách trong một giao dịch SQL. Endpoint `/Story/Cover/{id}` chỉ trả bìa công khai hoặc cho người có quyền xem sách chờ duyệt/bị ẩn. Metadata danh sách không tải bytes của toàn bộ bìa.

```sql
SELECT MaSach, TenSach, KieuAnhBia,
       DATALENGTH(HinhAnhBia) AS DungLuongBia
FROM dbo.Sach;
```

## Lưu trữ, kiểm tra và triển khai

Nội dung chương vẫn được lưu tại `Storage:Root` (mặc định `App_Data/Chapters`), SQL lưu metadata và khóa tệp. Không thay form đăng ảnh truyện tranh. Tệp chương và khóa đăng nhập cần giữ nguyên khi cập nhật; ảnh bìa được sao lưu cùng database.

```powershell
dotnet run --project tests/ForumChecks/ForumChecks.csproj
```

Kiểm tra không cần SQL bao gồm upload chương, ảnh bìa và các quy tắc hiển thị. Để kiểm tra SQL, tạo database riêng có tên kết thúc `_Tests` bằng script 01 và đặt `SACH_TEST_CONNECTION`; không dùng database đang vận hành. Xem `docs/KET_QUA_KIEM_TRA.md`.

Trên server, cấu hình `ConnectionStrings__SachMoiDaDen`, `Storage__Root`, `Storage__Keys`, dùng HTTPS và kho tệp bền vững. Không lưu mật khẩu SQL trong mã nguồn. Không có cơ chế quay về dữ liệu giả khi mất kết nối SQL; ứng dụng báo lỗi kết nối.
