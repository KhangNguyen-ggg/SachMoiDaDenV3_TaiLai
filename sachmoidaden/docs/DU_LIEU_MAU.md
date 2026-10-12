# Tài khoản và dữ liệu mẫu

## Nạp và chạy

1. Tạo database V3 bằng `Database/01_SachMoiDaDen.sql` nếu chưa có.
2. Chỉnh connection string trong `appsettings.json` tới SQL Server của bạn.
3. F5 trong Visual Studio hoặc chạy `dotnet run --launch-profile http`. Development tự nạp mẫu một lần.
4. Mở http://localhost:5010 và đăng nhập.

Nạp riêng bằng `.\scripts\Seed-Demo.ps1` hoặc `dotnet run --no-launch-profile -- --seed-demo`. Lệnh này kết thúc sau khi nạp, sau đó chạy web bình thường.

Tài khoản kết nối SQL cần quyền thêm dữ liệu và thêm extended property cho database; tài khoản Windows tạo database từ script thường có quyền này. Lỗi kết nối/schema/quyền được báo tại terminal. Tắt tự nạp bằng `DemoData:SeedOnStartup=false` trong `appsettings.Development.json`. Production chỉ nạp khi gọi `--seed-demo` rõ ràng.

## Tài khoản

Mật khẩu chung: **`Demo123!`**. SQL lưu hash bằng ASP.NET PasswordHasher.

| Email | Vai trò | Dùng để thử |
|---|---|---|
| `admin@demo.local` | Admin | Quản lý tất cả nội dung, tài khoản, vai trò |
| `kiemduyet@demo.local` | Moderator | Duyệt, ẩn/khôi phục và xử lý báo cáo |
| `donggop@demo.local` | Contributor | Tạo/sửa sách sở hữu, thêm chương |
| `donggop2@demo.local` | Contributor | So sánh quyền sở hữu với người thứ nhất |
| `thanhvien@demo.local` | Member | Đọc, tủ sách, lịch sử, đánh giá, diễn đàn |
| `docgia2@demo.local` | Member | Phản hồi, dữ liệu cá nhân khác nhau |
| `docgia3@demo.local` | Member | Thêm độc giả để thử đánh giá/lịch sử |
| `bikhoa@demo.local` | Member bị khóa | Đăng nhập bị từ chối; Admin có thể mở khóa |

## Dữ liệu được thêm lần đầu

Nội dung truyện hư cấu phục vụ kiểm thử, tranh minh họa đơn giản. Nếu database có dữ liệu riêng, tổng số dòng sẽ lớn hơn.

| Dữ liệu | Số lượng |
|---|---:|
| Tài khoản | 8 |
| Truyện chữ | 36: 28 TXT, 4 Markdown, 4 PDF |
| Truyện tranh | 12 |
| Sách công khai | 36: 27 chữ, 9 tranh |
| Chờ duyệt, chưa ẩn | 8 |
| Bị ẩn | 4, trong đó 1 đồng thời chờ duyệt |
| Chương | 288: 6 mỗi sách |
| Tệp | 432: 168 TXT, 24 MD, 24 PDF, 216 PNG |
| Trang tranh | 3 mỗi chương tranh |
| Liên kết sách–thể loại | 96, phủ 8 thể loại gốc |
| Đánh giá | 36 |
| Tủ sách | 24 dòng |
| Lịch sử đọc | 18 dòng |
| Lượt đọc giả lập | 1.008 trong 14 ngày gần nhất |
| Diễn đàn | 18 bài: 17 công khai, 1 bị ẩn |
| Tag | 6 mẫu, 2 tag mỗi bài |
| Bình luận | 156: sách, chương, diễn đàn, phản hồi |
| Báo cáo | 12: 6 chưa xử lý, 6 đã xử lý |

Lượt đọc là số liệu thử nghiệm; thời điểm tương đối lúc nạp giúp thử xếp hạng ngày/tuần/tháng. Tổng lượt của sách khớp các sự kiện mẫu đã thêm. Tag đã có được dùng lại.

## Các ca test gợi ý

1. Member tìm `ngon den` để thử không dấu; kết hợp thể loại, loại truyện, trạng thái sáng tác và phân trang.
2. Đọc **Ngọn đèn bên bến sương** (TXT), **Gió qua cửa sổ tầng ba** (MD), **Xưởng chế tạo những điều vui** (PDF), **Mèo Mây và phố nhỏ** (3 ảnh/chương). Đổi chương, lưu/bỏ lưu, đọc tiếp và đánh giá lại.
3. So sánh Tủ sách/Lịch sử của 3 Member. Bình luận ở sách, chương, diễn đàn; trả lời, tag/gợi ý và báo cáo.
4. Contributor sửa sách của mình, thử sửa sách của Contributor khác. Thêm chương/sửa sách đưa sách về chờ duyệt.
5. Moderator duyệt **Chiếc đồng hồ không có kim** hoặc **Đường đua xe giấy**, rồi Member kiểm tra sách xuất hiện.
6. **Câu lạc bộ sửa đồ cũ** vừa bị ẩn vừa chờ duyệt: duyệt vẫn giữ ẩn; khôi phục sau duyệt mới công khai. **Khu vườn sau bức tường** đã duyệt nhưng bị ẩn.
7. 6 sách công khai đầu có cha bị ẩn và phản hồi chưa ẩn: Member không thấy cả nhánh; khôi phục cha để thử phản hồi xuất hiện.
8. Moderator/Admin thử hoàn tất 6 báo cáo chưa xử lý. 6 dòng đã xử lý có snapshot, người xử lý, kết quả và thời gian.
9. Admin đổi vai trò/khóa Member để kiểm tra thu hồi phiên cũ; mở khóa tài khoản `bikhoa@demo.local`.
10. Khởi động lại app: những thay đổi bạn làm khi test phải giữ nguyên và không sinh dữ liệu trùng.

## Nạp lại và bảo toàn dữ liệu

Dữ liệu quan hệ tạo trong một transaction, nạp đồng thời khóa bằng `sp_getapplock`. Khi hoàn tất, extended property `SachMoiDaDen.Demo.v1` đánh dấu database đã nạp. Lần sau bỏ qua kể cả khi bạn đã xóa sách/tài khoản mẫu. Không tạo thêm bảng và không sửa schema SQL gốc.

Không ghi đè mật khẩu, quyền, khóa hay nội dung đã sửa. Nếu một email mẫu tồn tại trước khi nạp mà chưa có marker, lệnh dừng và rollback; không nâng quyền tài khoản trùng.

Tệp trong `DemoData/files` được kiểm tra SHA-256/dung lượng rồi sao chép vào `Storage:Root`, ngoài wwwroot. SQL lưu metadata. Tệp trùng khóa chỉ dùng lại nếu bytes khớp. Khi SQL lỗi, tệp đã chép có thể được giữ để tránh xóa dữ liệu trong tình huống chưa xác định được commit; lần chạy lại dùng lại đúng bytes.

Muốn bắt đầu sạch sau nhiều lần thử, dùng database thử nghiệm mới từ script V3 và `Storage:Root` mới. Không xóa marker để ép nạp vào database còn tài khoản mẫu. Sao lưu dữ liệu trước khi thay môi trường.

## Kiểm tra

`tests/ForumChecks` kiểm tra offline toàn bộ manifest và 432 tệp bằng bộ upload/trình đọc của app. Phần SQL tùy chọn kiểm tra nạp một lần, chạy lặp, giữ hash/quyền/khóa/dấu phiên và đăng nhập mẫu; chỉ chạy trên database kết thúc `_Tests` theo README. Phần SQL Server thật chưa chạy trong môi trường bàn giao.
