using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using sachmoidaden.Models;

namespace sachmoidaden.Services;
public sealed class BusinessErrorFilter(ILogger<BusinessErrorFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        int status;
        string message;
        if (context.Exception is BusinessException business)
        {
            status = business.Status;
            message = business.Message;
        }
        else if (context.Exception is InvalidDataException invalid)
        {
            status = 400;
            message = invalid.Message;
        }
        else if (context.Exception is SqlException sql)
        {
            logger.LogError(sql, "SQL operation failed");
            status = sql.Number >= 50000 || sql.Number is 2601 or 2627 or 547 ? 400 : 503;
            message = sql.Number switch
            {
                2601 or 2627 => "Dữ liệu đã tồn tại. Hãy kiểm tra email, thể loại hoặc tên tag.",
                547 => "Dữ liệu vẫn đang được sử dụng hoặc liên kết không hợp lệ.",
                50002 or 50004 or 50005 => "Tài khoản không có quyền hoặc đã bị khóa. Hãy đăng nhập lại.",
                50003 or 50006 or 50008 or 50022 => "Sách, chương hoặc bài viết không tồn tại hay chưa công khai.",
                50007 => "Số sao phải từ 1 đến 5.",
                50023 => "Bình luận cha hoặc tổ tiên đã bị ẩn.",
                50101 or 50021 => "Không thể gửi phản hồi sang khu vực khác.",
                1205 => "Dữ liệu đang được người khác cập nhật. Hãy gửi lại thao tác.",
                _ => status == 503 ? "Chưa kết nối được database. Kiểm tra SQL Server và ConnectionStrings:SachMoiDaDen." : "Không thể lưu do dữ liệu hoặc trạng thái đã thay đổi. Hãy tải lại trang."
            };
        }
        else if (context.Exception is IOException)
        {
            status = 503;
            message = "Chưa đọc hoặc lưu được tệp nội dung. Hãy thử lại.";
            logger.LogError(context.Exception, "File operation failed");
        }
        else
            return;
        context.Result = new ViewResult
        {
            ViewName = "~/Views/Shared/Error.cshtml",
            StatusCode = status,
            ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<ErrorViewModel>(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), context.ModelState)
            {
                Model = new ErrorViewModel
                {
                    Message = message
                }
            }
        };
        context.ExceptionHandled = true;
    }
}
