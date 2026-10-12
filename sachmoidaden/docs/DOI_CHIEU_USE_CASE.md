# Đối chiếu yêu cầu và mã nguồn

Căn cứ: Nhom2_LuongHoatDong_UseCase_TheoDatabaseV3(1).docx, Mo_ta_database_SachMoiDaDen(1).docx và 01_SachMoiDaDen (1)(1).sql do người dùng cung cấp. Cột xử lý mô tả mã nguồn đã viết; không thay thế kết quả chạy tích hợp SQL Server.

| Use case | Xử lý trong bản bàn giao | Vị trí chính |
| --- | --- | --- |
| UC-01 Đăng ký | Validate form, trim, email duy nhất, hash, Member mặc định | AccountController; LibraryService.RegisterAsync |
| UC-02 Đăng nhập và đăng xuất | Cookie, kiểm tra khóa và dấu phiên trên mỗi request, chuyển URL nội bộ | AccountController; Program |
| UC-03 Tìm kiếm và xếp hạng | Tìm không dấu, lọc nhiều thể loại theo OR, phân trang, top từ event SQL UTC+7 | HomeController; usp_BangXepHang |
| UC-04 Chi tiết | Chỉ sách công khai, chương và gợi ý nhiều thể loại, điều hướng diễn đàn độc lập | StoryController.Details |
| UC-05 Đọc chương | Kiểm tra tệp trước, đọc TXT/MD/ảnh/PDF, chống đếm 30 phút, cập nhật lịch sử | StoryController.Read; ChapterFiles; usp_DocChuong |
| UC-06 Tủ sách và lịch sử | Trạng thái đích khi lưu/bỏ lưu, lịch sử giữ MaChuong, đọc tiếp theo số chương | UserController; usp_LuuTuSach |
| UC-07 Đánh giá | Sao nguyên 1–5, cập nhật một dòng/tài khoản/sách | DiscussionController.Rate; usp_LuuDanhGia |
| UC-08 Diễn đàn | Không liên kết sách; bài và tag lưu cùng giao dịch; gợi ý theo tag | DiscussionController; CreateDiscussionAsync |
| UC-09 Bình luận | Đúng một đích sách/chương/bài; phản hồi cùng phạm vi và mọi tổ tiên công khai | CommentAsync; usp_ThemBinhLuan; vw_BinhLuanCongKhai |
| UC-10 Báo cáo | Đích công khai, snapshot 2.000 ký tự, lý do bắt buộc, không tự ẩn | ReportAsync |
| UC-11 Tạo/sửa sách | Quyền/chủ sở hữu, ít nhất một thể loại; Contributor chờ duyệt; Admin giữ trạng thái khi sửa | SaveBookAsync |
| UC-12 Chương nhập chữ | Chuyển thành TXT UTF-8, thêm metadata nguyên tử, dọn tệp khi xác nhận SQL chưa lưu | ManagementController.Chapter |
| UC-13 Chương ảnh | 50 ảnh, 5 MiB/ảnh, tổng ảnh 50 MiB; kiểm tra chữ ký, nối và sắp thứ tự | ComicChapter; ChapterFiles; site.js |
| UC-14 Chương từ tệp | TXT/MD/PDF/ảnh; giới hạn riêng theo loại tệp, thứ tự và quyền truy cập | FileChapter; _ChapterContent |
| UC-15 Xem trước | Contributor chỉ sách của mình; Moderator/Admin xem mọi sách; không tăng lượt/lịch sử | Management.Preview; Story.Preview; usp_LayTepNoiDung |
| UC-16 Duyệt/ẩn sách | Hai cờ độc lập; duyệt không tự khôi phục, khôi phục không tự duyệt | ModerateAsync |
| UC-17 Ẩn bài/bình luận | Đúng bản ghi; ẩn tổ tiên ẩn nhánh; diễn đàn không phụ thuộc sách | ModerateAsync; ContentQueries; view SQL |
| UC-18 Xử lý báo cáo | Người, kết quả, thời điểm; UPDATE khi DaXuLy=0; link đúng bài/bình luận quản lý | ResolveAsync; Management/Index |
| UC-19 Thể loại | Admin; tên duy nhất; đổi tên giữ mã; xóa chỉ khi chưa có SachTheLoai, gồm sách ẩn/chờ | GenreAsync |
| UC-20 Vai trò/khóa | Admin; không tự khóa/hạ quyền; stamp đổi; phiên cũ mất hiệu lực | UserRoleAsync; LockUserAsync; Program |

## Các thay đổi so với bản demo

DemoStore, ReadingStats và ComicFiles đã được thay bằng LibraryService, SqlDatabase và ChapterFiles. Không còn tạo tài khoản hay sách mẫu khi khởi động. Discussion không có StoryId. Comment có ba đích nullable; Chapter giữ danh sách ContentFile thay nội dung trực tiếp. Story.Views và số lượt xếp hạng dùng long.

Kho chương và khóa cookie ở App_Data hoặc đường dẫn bền vững cấu hình riêng. Endpoint /Story/File/{MaNoiDung} kiểm tra SQL mỗi lần và trả cache no-store. Khu quản lý hiển thị trạng thái duyệt và ẩn riêng, đếm thể loại qua danh sách liên kết; báo cáo có anchor tới đúng nội dung.

## Phạm vi theo tài liệu

Không bổ sung quên mật khẩu, đổi hồ sơ/mật khẩu, xin quyền Contributor, chat, thanh toán, sửa/xóa chương, sửa/xóa bài cá nhân, thông báo hoặc nhật ký duyệt riêng. Tài liệu đã nêu các phần này chưa thuộc phạm vi. Không tạo bảng mới hoặc thay thiết kế SQL V3.

## Lưu ý triển khai

Ứng dụng dùng schema V3 mới; không tự nhập dữ liệu trong bộ nhớ hay chuyển V2. Cần tạo database, đặt connection string, tạo Admin và cấp vai trò trước khi dùng luồng đóng góp/kiểm duyệt. Không tuyên bố hoàn tất nghiệm thu SQL khi chưa chạy các kiểm tra tích hợp trong môi trường có SQL Server.
