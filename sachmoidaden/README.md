# Sách Mới Đã Đến

Ứng dụng ASP.NET Core MVC .NET 8 sử dụng SQL Server và cấu trúc SachMoiDaDen trong các tài liệu kèm theo. Bản này thay lưu trữ DemoStore bằng SQL, giữ giao diện hiện có và bổ sung các luồng còn thiếu của UC-01 đến UC-20.

## Chạy trên Windows và Visual Studio 2022

1. Cài workload **ASP.NET and web development**, .NET 8 SDK và SQL Server/SQL Server Express.
2. Trong SSMS, mở `Database/01_SachMoiDaDen.sql` và Execute toàn bộ. Script tạo database mới `SachMoiDaDen`; không chạy lại trên V3 đã có bảng. Nếu đã tạo V3 từ file bạn gửi, dùng database đó.
3. Sửa `ConnectionStrings:SachMoiDaDen` trong `appsettings.json` cho đúng tên SQL Server của bạn. Giá trị mặc định dùng `.\SQLEXPRESS` và Windows Authentication. Nếu máy dùng instance khác, thay phần `Server=`. `TrustServerCertificate=True` chỉ phục vụ SQL Server local.
4. Mở `sachmoidaden.sln`, Restore NuGet rồi bấm F5. Hoặc chạy tại thư mục project:

```powershell
dotnet restore
dotnet build -c Release
dotnet run --launch-profile http
```

Khi chạy F5 hoặc profile `http` ở môi trường **Development**, ứng dụng tự nạp dữ liệu mẫu một lần: **8 tài khoản, 48 truyện, 288 chương**, bài thảo luận, bình luận, tủ sách, lịch sử và báo cáo. Database V3 phải được tạo ở bước 2 trước khi chạy. Không ghi đè dữ liệu đã có khi khởi động lại.

Đăng nhập nhanh: `admin@demo.local`, `kiemduyet@demo.local`, `donggop@demo.local`, `thanhvien@demo.local`. Mật khẩu chung: **`Demo123!`**. Danh sách đầy đủ và các ca test ở **`docs/DU_LIEU_MAU.md`**.

Muốn nạp mẫu bằng lệnh riêng, chạy `.\scripts\Seed-Demo.ps1` hoặc `dotnet run --no-launch-profile -- --seed-demo`. Lệnh này kết thúc sau khi nạp; sau đó chạy web như bình thường. Không dùng `--create-admin` cùng `--seed-demo`.

Địa chỉ mặc định: http://localhost:5010. Nếu gặp lỗi kết nối, kiểm tra SQL Server đang chạy, tên instance và quyền truy cập database của tài khoản Windows.

## Tạo Admin riêng, không dùng bộ mẫu

Nếu muốn database không có dữ liệu mẫu, đặt `DemoData:SeedOnStartup` thành `false` trong `appsettings.Development.json` trước lần chạy đầu, rồi tạo Admin theo cách dưới. Nếu đã nạp mẫu, đăng nhập Admin mẫu để quản lý tài khoản.

Mở PowerShell ở thư mục project:

```powershell
.\scripts\Create-FirstAdmin.ps1
```

Script hỏi tên, email và mật khẩu, tạo tài khoản Admin trong SQL bằng PasswordHasher rồi xóa các biến mật khẩu tạm. Mật khẩu từ 8–100 ký tự. Script dừng nếu đã có Admin; không tự nâng quyền một tài khoản đăng ký. Sau khi tạo, chạy ứng dụng bình thường và đăng nhập bằng email vừa nhập.

Nếu PowerShell chặn script, có thể dùng các lệnh sau trong terminal cục bộ:

```powershell
$env:BOOTSTRAP_ADMIN_EMAIL = 'email-cua-ban@example.com'
$env:BOOTSTRAP_ADMIN_NAME = 'Quản trị viên'
# Nhập mật khẩu bằng script Create-FirstAdmin.ps1, hoặc đặt BOOTSTRAP_ADMIN_PASSWORD trong phiên terminal riêng.
dotnet run --no-launch-profile -- --create-admin
```

Đăng ký trên web luôn tạo Member. Admin đổi vai trò sang Contributor hoặc Moderator tại **Quản lý → Tài khoản**. Các tài khoản mẫu chỉ được tạo bởi chế độ Development hoặc lệnh `--seed-demo` đã mô tả ở trên.

## Các luồng đã nối

- Khám phá, tìm kiếm tiếng Việt không dấu, lọc nhiều thể loại, gợi ý sách và xếp hạng ngày/tuần/tháng.
- Đăng ký/đăng nhập/đăng xuất, khóa tài khoản, dấu bảo mật phiên khi đổi vai trò hoặc khóa.
- Tủ sách lưu theo trạng thái đích; lịch sử giữ mã chương gần nhất; đánh giá cập nhật cùng một dòng.
- Diễn đàn độc lập với sách, tag và gợi ý theo tag; bình luận riêng ở sách, chương và bài diễn đàn.
- Contributor tạo/sửa sách của mình, đăng chương rồi chờ duyệt; Moderator kiểm duyệt; Admin quản lý nội dung và tài khoản.
- Nhập văn bản thành TXT; upload TXT/MD, PDF và ảnh; sắp thứ tự tệp; xem trước và phục vụ tệp qua endpoint có quyền.
- Báo cáo lưu snapshot, người và thời gian xử lý; hoàn tất chỉ khi báo cáo chưa được xử lý.

Bảng đối chiếu chi tiết nằm ở `docs/DOI_CHIEU_USE_CASE.md`.

## Quy tắc tệp và dữ liệu

`Storage:Root` mặc định là `App_Data/Chapters`, nằm ngoài wwwroot. SQL lưu metadata và khóa tệp; không lưu nội dung chương trực tiếp vào cột văn bản. Tệp TXT/MD phải là UTF-8 và hiển thị dưới dạng văn bản đã được Razor mã hóa. Không chạy HTML hoặc Markdown do người dùng nhập. PDF dùng trình xem của trình duyệt, kèm link mở riêng.

Giới hạn server: 50 tệp/chương, tổng 100 MiB; ảnh 5 MiB/tệp và tổng ảnh 50 MiB; TXT/MD 2 MiB/tệp; PDF 20 MiB/tệp. Form nhập chữ giới hạn 100.000 ký tự. Loại sách phân loại tác phẩm; form tệp không áp đặt MIME theo loại sách.

Chỉ cộng lượt đọc sau khi kiểm tra tất cả tệp của chương đọc được và đúng dung lượng. Việc xem trước và lấy từng tệp không cộng lượt. SQL xử lý chống đếm lặp 30 phút và cập nhật lịch sử trong cùng giao dịch.

Contributor lưu sách hoặc thêm chương đưa toàn bộ sách về chờ duyệt. Duyệt và ẩn/khôi phục là hai thao tác độc lập. Khóa tài khoản không tự ẩn nội dung. Sách bị ẩn/chờ duyệt không xuất hiện trong tủ sách/lịch sử công khai nhưng các dòng cá nhân vẫn còn trong database.

Khi SQL thất bại trước khi lưu metadata, tệp mới được dọn. Nếu kết nối mất khiến kết quả commit chưa xác định, ứng dụng kiểm tra lại khóa tệp; nếu chưa xác nhận được, giữ tệp và báo lỗi để tránh xóa nội dung có thể đã được lưu. Kiểm tra danh sách chương trước khi gửi lại.

## Kiểm tra

```powershell
dotnet run --project tests/ForumChecks/ForumChecks.csproj
```

Mặc định chạy kiểm tra không cần SQL: Unicode, gợi ý, visibility nhánh bình luận, upload và kho tệp. Muốn chạy thêm kiểm tra BLL/DAL:

1. Tạo bản database kiểm tra từ script gốc: thay tên `SachMoiDaDen` thành `SachMoiDaDen_Tests` trong một bản sao rồi chạy.
2. Đặt `SACH_TEST_CONNECTION` đến database này (tên phải kết thúc bằng `_Tests`).
3. Chạy lại lệnh kiểm tra. Fixture có email bắt đầu bằng `checks-`, được giữ trong database riêng để xem kết quả. Không dùng database đang vận hành.

```powershell
$env:SACH_TEST_CONNECTION = 'Server=.\SQLEXPRESS;Database=SachMoiDaDen_Tests;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --project tests/ForumChecks/ForumChecks.csproj
Remove-Item Env:SACH_TEST_CONNECTION
```

Xem `docs/KET_QUA_KIEM_TRA.md` để phân biệt kiểm tra đã chạy và kiểm tra còn cần thực hiện với SQL Server thật.

## Khi đưa lên server

Production không tự nạp mẫu. Khi dùng database thử nghiệm cho triển khai thật, đổi mật khẩu hoặc xóa tài khoản mẫu có mật khẩu công khai trước khi sử dụng.

Đặt connection string qua biến `ConnectionStrings__SachMoiDaDen`; dùng tài khoản SQL chỉ có quyền cần thiết. Đặt `Storage__Root` và `Storage__Keys` tới thư mục bền vững có quyền đọc/ghi cho tiến trình ứng dụng, ngoài thư mục static. Bật HTTPS tại IIS hoặc reverse proxy. Không để mật khẩu SQL trong mã nguồn.

Sao lưu đồng thời database, kho chương và khóa Data Protection. Không xóa App_Data khi cập nhật phiên bản. Nếu nhiều instance cùng chạy, cần dùng chung kho tệp và khóa Data Protection. Database là script tạo mới, không tự chuyển dữ liệu DemoStore hoặc V2 sang V3.

## Cấu trúc

- `Controllers`, `Views`: Web và Razor.
- `Services/LibraryService.cs`: nghiệp vụ, quyền và giao dịch.
- `Services/ChapterFiles.cs`: kho tệp và kiểm tra upload.
- `Services/DemoSeeder.cs`, `DemoData`: tài khoản, dữ liệu quan hệ và tệp chương mẫu; nạp một lần theo database.
- `Data/SqlDatabase.cs`: DAL ADO.NET, tham số SQL và TVP.
- `Models`: các DTO và ViewModel dùng chung.

Ứng dụng chỉ thêm Microsoft.Data.SqlClient; không thêm ORM hoặc framework kiểm thử. Truy vấn đang nạp metadata cho các ViewModel có sẵn và phân trang tại BLL; nếu dữ liệu lên hàng nghìn sách, chuyển phân trang/tìm kiếm sang SQL. Nội dung tệp chỉ được đọc khi mở chương.
