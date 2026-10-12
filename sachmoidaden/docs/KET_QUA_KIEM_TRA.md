# Kết quả kiểm tra bản dữ liệu SQL và ảnh bìa — 12/10/2026

- Biên dịch toàn bộ mã C# bằng Roslyn từ .NET SDK 8.0.415: đạt, không lỗi/cảnh báo.
- Biên dịch 25 Razor views bằng Razor source generator của SDK: đạt, không lỗi/cảnh báo.
- Chạy `tests/ForumChecks`: 33 kiểm tra không cần SQL đều đạt, gồm chữ UTF-8, Markdown/PDF, upload ảnh chương, thứ tự ảnh, ảnh bìa PNG/JPEG/WebP, từ chối ảnh giả/rỗng/quá dung lượng, dọn tệp khi upload lỗi và quyền hiển thị bình luận.
- Kiểm tra SQL chưa chạy vì phiên làm việc không có SQL Server và không có kết nối đến database trên máy người dùng.

Môi trường này không chạy được MSBuild do cơ chế đọc thông tin process, nên kiểm tra biên dịch sử dụng trực tiếp compiler và Razor source generator của cùng SDK. Source đã biên dịch, nhưng cần chạy `dotnet build -c Release` trên máy Windows để kiểm tra quy trình build dự án đầy đủ.

## Kiểm tra SQL có sẵn

Tạo database `SachMoiDaDen_Tests` từ script 01 (đổi tên database trong một bản sao), rồi chạy:

```powershell
$env:SACH_TEST_CONNECTION = 'Server=.\SQLEXPRESS;Database=SachMoiDaDen_Tests;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --project tests/ForumChecks/ForumChecks.csproj
Remove-Item Env:SACH_TEST_CONNECTION
```

Các kiểm tra SQL gồm lưu bìa và đọc lại qua service mới, giữ bìa khi sửa metadata, xóa/thay bìa, quyền xem bìa chờ duyệt/bị ẩn, duyệt sách, tủ sách, lịch sử, đánh giá, chống đếm lượt lặp và phân quyền. Dữ liệu kiểm thử chỉ tạo khi chạy lệnh này trong database riêng.

## Kiểm tra thực tế cần thực hiện trên SQL Server của bạn

1. Sao lưu và chạy script 02 trên database đang dùng; chạy lại script để kiểm tra tính lặp an toàn.
2. Đăng nhập bằng tài khoản riêng, thêm sách với bìa; kiểm tra bytes và MIME trong bảng Sach.
3. Sửa thông tin sách không chọn ảnh, thay ảnh, xóa ảnh; kiểm tra thông tin và bìa sau mỗi lần.
4. Đóng/mở ứng dụng; dữ liệu và bìa phải còn nguyên.
5. Contributor đăng sách có ảnh; Guest không xem được bìa trước khi duyệt và sau khi ẩn.
6. Thêm chương truyện tranh bằng nút chọn ảnh/Thêm ảnh, đổi thứ tự và mở chương để kiểm tra chức năng cũ.
