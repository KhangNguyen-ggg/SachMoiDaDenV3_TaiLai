# Kết quả kiểm tra bản bàn giao

Ngày 08/10/2026. Phạm vi kiểm tra dưới đây phân biệt rõ mã nguồn được kiểm tra với tích hợp SQL Server chưa chạy.

## Đã chạy

- **31 kiểm tra cốt lõi đạt**: tìm tiếng Việt không dấu; gợi ý loại trừ sách ẩn; tag không phân biệt hoa/thường; ẩn tổ tiên/ẩn con độc lập; chặn phản hồi khác phạm vi; diễn đàn không phụ thuộc sách; hiển thị UTC+7; TXT UTF-8; tệp còn sau khởi tạo lại dịch vụ; MD/PDF metadata; ảnh PNG/JPEG/WebP và thứ tự; từ chối tệp giả, rỗng, không hỗ trợ, UTF-8 sai và thứ tự sai; dọn batch tệp lỗi; phát hiện tệp bị mất.
- **Bộ mẫu đã kiểm tra**: 48 sách, 288 chương, 432 tệp có SHA-256/dung lượng đúng; tất cả đi qua upload, kiểm tra MIME/thứ tự và đọc lại bằng ChapterFiles. Có truyện chữ/tranh công khai cùng các trạng thái chờ duyệt/bị ẩn. 24 PDF có văn bản tiếng Việt trích xuất được; mẫu PDF/PNG đã render và kiểm tra trực quan.
- **C# và 25 trang Razor qua kiểm tra biên dịch riêng** bằng Roslyn C# 12 và Razor source generator .NET 8. Phần API SqlClient dùng kiểu thay thế trong môi trường kiểm tra để kiểm tra cú pháp và liên kết kiểu; việc này không chứng minh driver NuGet hoặc SQL Server đã chạy. Các kiểu thay thế chỉ nằm trong khu vực kiểm tra tạm, không có trong ứng dụng bàn giao.
- **11 màn hình Razor render thành HTML** với fixture: Thư viện, Chi tiết, Đọc chương, form sách chữ, form truyện tranh, bài diễn đàn và 5 tab quản lý.
- Kiểm tra HTML các form POST có antiforgery token, văn bản trình đọc được escape, form upload tệp hiện diện, tủ sách gửi trạng thái đích và báo cáo liên kết tới đúng anchor.
- `node --check` đạt cho JavaScript. JSON và XML project hợp lệ. ZIP được kiểm tra tính toàn vẹn trước khi gửi.

## Chưa xác nhận

- `dotnet restore` và `dotnet build` đầy đủ với Microsoft.Data.SqlClient thật: môi trường không truy cập NuGet; dotnet CLI/MSBuild còn lỗi truy vấn thông tin process. Roslyn/Razor riêng chạy được.
- Nạp dữ liệu mẫu, extended property marker, quyền SQL, tính idempotent/concurrency của seeder và đăng nhập 8 tài khoản trên SQL thật.
- Các procedure, transaction, TVP và ánh xạ dữ liệu với SQL Server thật.
- Kiểm tra nhiều kết nối SQL đồng thời, chuỗi auth cookie thực với database và giữ dữ liệu sau restart web.
- Hiển thị PDF theo trình duyệt và kiểm tra responsive bằng browser thực: môi trường chưa có Chromium. HTML render không thay thế kiểm tra thị giác ở điện thoại/desktop.

Không coi kết quả biên dịch với kiểu API thay thế hoặc fixture HTML là build/phép thử end-to-end của ứng dụng SQL. Cần chạy các lệnh trong README trước khi nộp hoặc triển khai.

## Kiểm tra tích hợp đã chuẩn bị

`tests/ForumChecks/Program.cs` có phần tùy chọn dùng `SACH_TEST_CONNECTION`, chỉ chấp nhận database kết thúc bằng `_Tests`. Nó tạo fixture riêng và kiểm tra:

- Seeder nạp một lần và bỏ qua lần sau; giữ hash, quyền, khóa và dấu phiên; Admin mẫu đăng nhập được, tài khoản khóa bị từ chối.
- Đăng ký/login, email trùng không phân biệt hoa/thường, Member/Moderator không được đăng sửa sách.
- Contributor sở hữu sách, chờ duyệt, duyệt giữ cờ ẩn và khôi phục mới công khai.
- Thêm chương và metadata, đọc lặp không tăng lượt, lịch sử giữ mã chương.
- Lưu/bỏ tủ sách lặp không đảo trạng thái, đánh giá cập nhật cùng dòng.
- Chủ đề độc lập, bình luận chương và phản hồi khác phạm vi bị từ chối.
- Hai người hoàn tất báo cáo chỉ một kết quả được lưu; ẩn cha loại phản hồi khỏi view công khai.
- Tệp sách ẩn bị chặn với Member nhưng được xem trước bởi Moderator.
- Thể loại đang dùng không bị xóa; Admin không tự khóa/hạ quyền; đổi vai trò đổi dấu phiên; khóa chặn login.

Các kiểm tra SQL trên đã viết và kiểm tra cú pháp nhưng **chưa chạy**. Fixture được giữ trong database thử nghiệm; không chạy vào database vận hành.
