/*
SACH MOI DA DEN - SQL Server - thiet ke theo sachmoidaden (1).zip va yeu cau bo sung ngay 06/10/2026.
Noi dung chuong chi luu duong dan TXT/MD/anh/PDF. Dien dan doc lap.
Binh luan tach ba pham vi: sach, chuong, bai thao luan.
16 bang; ten tieng Viet khong dau; noi dung Unicode; thoi gian UTC.
Chay ca file trong SSMS tren SQL Server 2016+ (Database Engine).
File tao moi, KHONG xoa/ghi de database da co bang cua thiet ke nay.
Chi seed 4 vai tro va 8 the loai; khong tao tai khoan/mat khau mac dinh.
DDL + seed + views + triggers + procedures nam trong mot transaction.
*/
USE [master];
GO
IF DB_ID(N'SachMoiDaDen') IS NULL
    EXEC(N'CREATE DATABASE [SachMoiDaDen] COLLATE Vietnamese_100_CI_AS;');
GO
USE [SachMoiDaDen];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.VaiTro', N'U') IS NOT NULL
    THROW 50000, N'Da co thiet ke SachMoiDaDen. Khong chay lai file tao moi tren du lieu dang su dung.', 1;

CREATE TABLE dbo.VaiTro (
    MaVaiTro varchar(20) NOT NULL CONSTRAINT PK_VaiTro PRIMARY KEY,
    TenVaiTro nvarchar(50) NOT NULL CONSTRAINT UQ_VaiTro_Ten UNIQUE,
    CONSTRAINT CK_VaiTro_Ma CHECK (MaVaiTro IN ('Member','Contributor','Moderator','Admin'))
);

CREATE TABLE dbo.TaiKhoan (
    MaTaiKhoan int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TaiKhoan PRIMARY KEY,
    TenHienThi nvarchar(80) NOT NULL,
    Email nvarchar(200) COLLATE Vietnamese_100_CI_AS NOT NULL,
    MatKhauHash nvarchar(512) NOT NULL,
    MaVaiTro varchar(20) NOT NULL CONSTRAINT DF_TaiKhoan_VaiTro DEFAULT 'Member',
    BiKhoa bit NOT NULL CONSTRAINT DF_TaiKhoan_BiKhoa DEFAULT 0,
    DauBaoMat uniqueidentifier NOT NULL CONSTRAINT DF_TaiKhoan_DauBaoMat DEFAULT NEWID(),
    NgayTao datetime2(3) NOT NULL CONSTRAINT DF_TaiKhoan_NgayTao DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_TaiKhoan_Email UNIQUE (Email),
    CONSTRAINT FK_TaiKhoan_VaiTro FOREIGN KEY (MaVaiTro) REFERENCES dbo.VaiTro(MaVaiTro),
    CONSTRAINT CK_TaiKhoan_Ten CHECK (LEN(LTRIM(RTRIM(TenHienThi))) BETWEEN 2 AND 80),
    -- Email duoc Trim va kiem tra dinh dang tai BLL; day chi la rang buoc toi thieu.
    CONSTRAINT CK_TaiKhoan_Email CHECK (LEN(Email)>3 AND CHARINDEX(N'@',Email)>1
        AND CHARINDEX(N' ',Email)=0 AND Email NOT LIKE N'%'+NCHAR(9)+N'%'
        AND Email NOT LIKE N'%'+NCHAR(10)+N'%' AND Email NOT LIKE N'%'+NCHAR(13)+N'%'),
    CONSTRAINT CK_TaiKhoan_Hash CHECK (LEN(LTRIM(RTRIM(MatKhauHash)))>0)
);

CREATE TABLE dbo.TheLoai (
    MaTheLoai int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TheLoai PRIMARY KEY,
    TenTheLoai nvarchar(60) COLLATE Vietnamese_100_CI_AS NOT NULL,
    CONSTRAINT UQ_TheLoai_Ten UNIQUE (TenTheLoai),
    CONSTRAINT CK_TheLoai_Ten CHECK (LEN(LTRIM(RTRIM(TenTheLoai)))>0)
);

CREATE TABLE dbo.Sach (
    MaSach int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sach PRIMARY KEY,
    TenSach nvarchar(160) NOT NULL,
    TacGia nvarchar(100) NOT NULL,
    GioiThieu nvarchar(3000) NOT NULL,
    HinhAnhBia varbinary(max) NULL, -- Bytes anh bia; NULL neu chua tai len.
    KieuAnhBia varchar(20) NULL, -- image/jpeg, image/png, image/webp
    LoaiTruyen tinyint NOT NULL CONSTRAINT DF_Sach_Loai DEFAULT 0, -- 0: chu; 1: tranh
    -- 0: Dang ra; 1: Hoan thanh. Khac voi trang thai kiem duyet.
    TrangThaiSangTac tinyint NOT NULL CONSTRAINT DF_Sach_SangTac DEFAULT 0,
    MaNguoiDang int NOT NULL,
    ChoDuyet bit NOT NULL CONSTRAINT DF_Sach_ChoDuyet DEFAULT 1,
    BiAn bit NOT NULL CONSTRAINT DF_Sach_BiAn DEFAULT 0,
    LuotDoc bigint NOT NULL CONSTRAINT DF_Sach_LuotDoc DEFAULT 0,
    NgayTao datetime2(3) NOT NULL CONSTRAINT DF_Sach_NgayTao DEFAULT SYSUTCDATETIME(),
    NgayCapNhat datetime2(3) NOT NULL CONSTRAINT DF_Sach_CapNhat DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Sach_NguoiDang FOREIGN KEY (MaNguoiDang) REFERENCES dbo.TaiKhoan(MaTaiKhoan),
    CONSTRAINT CK_Sach_AnhBia CHECK (
        (HinhAnhBia IS NULL AND KieuAnhBia IS NULL) OR
        (HinhAnhBia IS NOT NULL AND KieuAnhBia IS NOT NULL AND
         DATALENGTH(HinhAnhBia) BETWEEN 1 AND 5242880 AND KieuAnhBia IN ('image/jpeg','image/png','image/webp'))),
    CONSTRAINT CK_Sach_Loai CHECK (LoaiTruyen IN (0,1)),
    CONSTRAINT CK_Sach_Ten CHECK (LEN(LTRIM(RTRIM(TenSach)))>0),
    CONSTRAINT CK_Sach_TacGia CHECK (LEN(LTRIM(RTRIM(TacGia)))>0),
    CONSTRAINT CK_Sach_GioiThieu CHECK (LEN(LTRIM(RTRIM(GioiThieu)))>0),
    CONSTRAINT CK_Sach_SangTac CHECK (TrangThaiSangTac IN (0,1)),
    CONSTRAINT CK_Sach_LuotDoc CHECK (LuotDoc>=0),
    CONSTRAINT CK_Sach_Ngay CHECK (NgayCapNhat>=NgayTao)
);

CREATE TABLE dbo.SachTheLoai (
    MaSach int NOT NULL,
    MaTheLoai int NOT NULL,
    ThuTu smallint NOT NULL,
    CONSTRAINT PK_SachTheLoai PRIMARY KEY(MaSach,MaTheLoai),
    CONSTRAINT UQ_SachTheLoai_ThuTu UNIQUE(MaSach,ThuTu),
    CONSTRAINT FK_SachTheLoai_Sach FOREIGN KEY(MaSach) REFERENCES dbo.Sach(MaSach),
    CONSTRAINT FK_SachTheLoai_TheLoai FOREIGN KEY(MaTheLoai) REFERENCES dbo.TheLoai(MaTheLoai),
    CONSTRAINT CK_SachTheLoai_ThuTu CHECK(ThuTu>0)
);

CREATE TABLE dbo.Chuong (
    MaChuong int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Chuong PRIMARY KEY,
    MaSach int NOT NULL,
    SoChuong int NOT NULL,
    TieuDe nvarchar(160) NOT NULL,
    NgayTao datetime2(3) NOT NULL CONSTRAINT DF_Chuong_NgayTao DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Chuong_Sach FOREIGN KEY(MaSach) REFERENCES dbo.Sach(MaSach),
    CONSTRAINT UQ_Chuong_So UNIQUE(MaSach,SoChuong),
    CONSTRAINT UQ_Chuong_Sach UNIQUE(MaChuong,MaSach),
    CONSTRAINT CK_Chuong_So CHECK(SoChuong>0),
    CONSTRAINT CK_Chuong_TieuDe CHECK(LEN(LTRIM(RTRIM(TieuDe)))>0)
);

-- Tat ca TXT/MD/anh/PDF dung CHUNG bang nay; khong luu bytes/noi dung chuong trong SQL.
CREATE TABLE dbo.NoiDungChuong (
    MaNoiDung int IDENTITY(1,1) NOT NULL CONSTRAINT PK_NoiDungChuong PRIMARY KEY,
    MaChuong int NOT NULL,
    ThuTu int NOT NULL,
    DuongDanTep varchar(450) COLLATE Latin1_General_100_BIN2 NOT NULL,
    KieuTep varchar(30) NOT NULL,
    TenTepGoc nvarchar(255) NOT NULL,
    DungLuong bigint NOT NULL,
    CONSTRAINT FK_NoiDungChuong_Chuong FOREIGN KEY(MaChuong) REFERENCES dbo.Chuong(MaChuong),
    CONSTRAINT UQ_NoiDungChuong_ThuTu UNIQUE(MaChuong,ThuTu),
    CONSTRAINT UQ_NoiDungChuong_DuongDan UNIQUE(DuongDanTep),
    CONSTRAINT CK_NoiDungChuong_ThuTu CHECK(ThuTu>0),
    -- Khoa tep ASCII do server sinh; vi du noi-dung/12/9a2c.pdf, KHONG phai duong dan may/URL.
    CONSTRAINT CK_NoiDungChuong_DuongDan CHECK(
        LEN(DuongDanTep)>0 AND DuongDanTep NOT LIKE '%[^a-z0-9/._-]%'
        AND LEFT(DuongDanTep,1)<>'/' AND RIGHT(DuongDanTep,1)<>'/'
        AND CHARINDEX('..',DuongDanTep)=0 AND CHARINDEX('//',DuongDanTep)=0
        AND DuongDanTep<>'.' AND LEFT(DuongDanTep,2)<>'./'
        AND RIGHT(DuongDanTep,2)<>'/.' AND CHARINDEX('/./',DuongDanTep)=0),
    CONSTRAINT CK_NoiDungChuong_Kieu CHECK(KieuTep IN
        ('text/plain','text/markdown','image/jpeg','image/png','image/webp','application/pdf')),
    CONSTRAINT CK_NoiDungChuong_Ten CHECK(LEN(LTRIM(RTRIM(TenTepGoc)))>0),
    CONSTRAINT CK_NoiDungChuong_Size CHECK(DungLuong>0 AND
        (KieuTep NOT IN('image/jpeg','image/png','image/webp') OR DungLuong<=5242880))
);

CREATE TABLE dbo.LuotDocSach (
    MaLuotDoc bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_LuotDocSach PRIMARY KEY,
    MaSach int NOT NULL,
    MaTaiKhoan int NULL,
    MaKhach uniqueidentifier NULL,
    ThoiDiem datetime2(3) NOT NULL CONSTRAINT DF_LuotDocSach_ThoiDiem DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_LuotDocSach_Sach FOREIGN KEY(MaSach) REFERENCES dbo.Sach(MaSach),
    CONSTRAINT FK_LuotDocSach_TaiKhoan FOREIGN KEY(MaTaiKhoan) REFERENCES dbo.TaiKhoan(MaTaiKhoan),
    CONSTRAINT CK_LuotDocSach_Nguoi CHECK(
        (MaTaiKhoan IS NOT NULL AND MaKhach IS NULL) OR (MaTaiKhoan IS NULL AND MaKhach IS NOT NULL))
);

CREATE TABLE dbo.TuSach (
    MaTaiKhoan int NOT NULL,
    MaSach int NOT NULL,
    NgayLuu datetime2(3) NOT NULL CONSTRAINT DF_TuSach_NgayLuu DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_TuSach PRIMARY KEY (MaTaiKhoan,MaSach),
    CONSTRAINT FK_TuSach_TaiKhoan FOREIGN KEY (MaTaiKhoan) REFERENCES dbo.TaiKhoan(MaTaiKhoan),
    CONSTRAINT FK_TuSach_Sach FOREIGN KEY (MaSach) REFERENCES dbo.Sach(MaSach)
);

CREATE TABLE dbo.LichSuDoc (
    MaTaiKhoan int NOT NULL,
    MaSach int NOT NULL,
    MaChuong int NOT NULL,
    LanDocCuoi datetime2(3) NOT NULL CONSTRAINT DF_LichSuDoc_ThoiGian DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_LichSuDoc PRIMARY KEY (MaTaiKhoan,MaSach),
    CONSTRAINT FK_LichSuDoc_TaiKhoan FOREIGN KEY (MaTaiKhoan) REFERENCES dbo.TaiKhoan(MaTaiKhoan),
    -- FK ghep ngan chuong thuoc sach khac. Khong luu so thu tu thay cho ID chuong.
    CONSTRAINT FK_LichSuDoc_Chuong FOREIGN KEY (MaChuong,MaSach) REFERENCES dbo.Chuong(MaChuong,MaSach)
);

CREATE TABLE dbo.DanhGia (
    MaTaiKhoan int NOT NULL,
    MaSach int NOT NULL,
    SoSao tinyint NOT NULL,
    NgayDanhGia datetime2(3) NOT NULL CONSTRAINT DF_DanhGia_Ngay DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_DanhGia PRIMARY KEY (MaTaiKhoan,MaSach),
    CONSTRAINT FK_DanhGia_TaiKhoan FOREIGN KEY (MaTaiKhoan) REFERENCES dbo.TaiKhoan(MaTaiKhoan),
    CONSTRAINT FK_DanhGia_Sach FOREIGN KEY (MaSach) REFERENCES dbo.Sach(MaSach),
    CONSTRAINT CK_DanhGia_Sao CHECK (SoSao BETWEEN 1 AND 5)
);

CREATE TABLE dbo.BaiThaoLuan (
    MaBaiThaoLuan int IDENTITY(1,1) NOT NULL CONSTRAINT PK_BaiThaoLuan PRIMARY KEY,
    MaTaiKhoan int NOT NULL,
    TieuDe nvarchar(160) NOT NULL,
    NoiDung nvarchar(max) NOT NULL,
    BiAn bit NOT NULL CONSTRAINT DF_BaiThaoLuan_BiAn DEFAULT 0,
    NgayTao datetime2(3) NOT NULL CONSTRAINT DF_BaiThaoLuan_NgayTao DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_BaiThaoLuan_TaiKhoan FOREIGN KEY (MaTaiKhoan) REFERENCES dbo.TaiKhoan(MaTaiKhoan),
    CONSTRAINT CK_BaiThaoLuan_TieuDe CHECK (LEN(LTRIM(RTRIM(TieuDe)))>0),
    CONSTRAINT CK_BaiThaoLuan_NoiDung CHECK (LEN(LTRIM(RTRIM(NoiDung)))>0 AND DATALENGTH(NoiDung)<=10000)
);

CREATE TABLE dbo.Tag (
    MaTag int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tag PRIMARY KEY,
    TenTag nvarchar(30) COLLATE Vietnamese_100_CI_AS NOT NULL,
    CONSTRAINT UQ_Tag_Ten UNIQUE(TenTag),
    CONSTRAINT CK_Tag_Ten CHECK(LEN(LTRIM(RTRIM(TenTag)))>0 AND CHARINDEX(N',',TenTag)=0)
);

CREATE TABLE dbo.BaiThaoLuanTag (
    MaBaiThaoLuan int NOT NULL,
    MaTag int NOT NULL,
    ThuTu tinyint NOT NULL,
    CONSTRAINT PK_BaiThaoLuanTag PRIMARY KEY(MaBaiThaoLuan,MaTag),
    CONSTRAINT UQ_BaiThaoLuanTag_ThuTu UNIQUE(MaBaiThaoLuan,ThuTu),
    CONSTRAINT FK_BaiThaoLuanTag_Bai FOREIGN KEY(MaBaiThaoLuan) REFERENCES dbo.BaiThaoLuan(MaBaiThaoLuan),
    CONSTRAINT FK_BaiThaoLuanTag_Tag FOREIGN KEY(MaTag) REFERENCES dbo.Tag(MaTag),
    CONSTRAINT CK_BaiThaoLuanTag_ThuTu CHECK(ThuTu BETWEEN 1 AND 5)
);

CREATE TABLE dbo.BinhLuan (
    MaBinhLuan int IDENTITY(1,1) NOT NULL CONSTRAINT PK_BinhLuan PRIMARY KEY,
    MaSach int NULL, -- chi dien neu binh luan TRUC TIEP sach
    MaChuong int NULL, -- chi dien neu binh luan TRONG CHUONG
    MaBaiThaoLuan int NULL, -- dien dan doc lap, chi dien MaBai
    MaBinhLuanCha int NULL,
    MaTaiKhoan int NOT NULL,
    NoiDung nvarchar(2000) NOT NULL,
    BiAn bit NOT NULL CONSTRAINT DF_BinhLuan_BiAn DEFAULT 0,
    NgayTao datetime2(3) NOT NULL CONSTRAINT DF_BinhLuan_NgayTao DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_BinhLuan_Sach FOREIGN KEY (MaSach) REFERENCES dbo.Sach(MaSach),
    CONSTRAINT FK_BinhLuan_Chuong FOREIGN KEY(MaChuong) REFERENCES dbo.Chuong(MaChuong),
    CONSTRAINT FK_BinhLuan_Bai FOREIGN KEY (MaBaiThaoLuan) REFERENCES dbo.BaiThaoLuan(MaBaiThaoLuan),
    CONSTRAINT FK_BinhLuan_TaiKhoan FOREIGN KEY (MaTaiKhoan) REFERENCES dbo.TaiKhoan(MaTaiKhoan),
    CONSTRAINT FK_BinhLuan_Cha FOREIGN KEY(MaBinhLuanCha) REFERENCES dbo.BinhLuan(MaBinhLuan),
    CONSTRAINT CK_BinhLuan_Dich CHECK(
        (CASE WHEN MaSach IS NOT NULL THEN 1 ELSE 0 END)+
        (CASE WHEN MaChuong IS NOT NULL THEN 1 ELSE 0 END)+
        (CASE WHEN MaBaiThaoLuan IS NOT NULL THEN 1 ELSE 0 END)=1),
    -- Cha da ton tai truoc con: chan tu tham chieu va moi chu trinh, khong gioi han do sau.
    CONSTRAINT CK_BinhLuan_ThuTuCha CHECK(MaBinhLuanCha IS NULL OR MaBinhLuanCha<MaBinhLuan),
    CONSTRAINT CK_BinhLuan_NoiDung CHECK (LEN(LTRIM(RTRIM(NoiDung)))>0)
);

CREATE TABLE dbo.BaoCao (
    MaBaoCao int IDENTITY(1,1) NOT NULL CONSTRAINT PK_BaoCao PRIMARY KEY,
    MaNguoiBaoCao int NOT NULL,
    MaSach int NULL,
    MaBaiThaoLuan int NULL,
    MaBinhLuan int NULL,
    -- Snapshot ten sach/tieu de bai/noi dung binh luan tai luc gui, nhu TargetTitle cua giao dien.
    NoiDungBiBaoCao nvarchar(2000) NOT NULL,
    LyDo nvarchar(1000) NOT NULL,
    NgayBaoCao datetime2(3) NOT NULL CONSTRAINT DF_BaoCao_Ngay DEFAULT SYSUTCDATETIME(),
    DaXuLy bit NOT NULL CONSTRAINT DF_BaoCao_DaXuLy DEFAULT 0,
    MaNguoiXuLy int NULL,
    KetQuaXuLy nvarchar(1000) NULL,
    NgayXuLy datetime2(3) NULL,
    CONSTRAINT FK_BaoCao_NguoiGui FOREIGN KEY (MaNguoiBaoCao) REFERENCES dbo.TaiKhoan(MaTaiKhoan),
    CONSTRAINT FK_BaoCao_Sach FOREIGN KEY (MaSach) REFERENCES dbo.Sach(MaSach),
    CONSTRAINT FK_BaoCao_Bai FOREIGN KEY (MaBaiThaoLuan) REFERENCES dbo.BaiThaoLuan(MaBaiThaoLuan),
    CONSTRAINT FK_BaoCao_BinhLuan FOREIGN KEY (MaBinhLuan) REFERENCES dbo.BinhLuan(MaBinhLuan),
    CONSTRAINT FK_BaoCao_NguoiXuLy FOREIGN KEY (MaNguoiXuLy) REFERENCES dbo.TaiKhoan(MaTaiKhoan),
    -- Chi mot dich den; ba FK that thay cho Kind + TargetId khong co FK.
    CONSTRAINT CK_BaoCao_DoiTuong CHECK (
        (CASE WHEN MaSach IS NOT NULL THEN 1 ELSE 0 END)+
        (CASE WHEN MaBaiThaoLuan IS NOT NULL THEN 1 ELSE 0 END)+
        (CASE WHEN MaBinhLuan IS NOT NULL THEN 1 ELSE 0 END)=1),
    CONSTRAINT CK_BaoCao_Snapshot CHECK (LEN(LTRIM(RTRIM(NoiDungBiBaoCao)))>0),
    CONSTRAINT CK_BaoCao_LyDo CHECK (LEN(LTRIM(RTRIM(LyDo)))>0),
    CONSTRAINT CK_BaoCao_XuLy CHECK (
        (DaXuLy=0 AND MaNguoiXuLy IS NULL AND KetQuaXuLy IS NULL AND NgayXuLy IS NULL)
        OR (DaXuLy=1 AND MaNguoiXuLy IS NOT NULL AND KetQuaXuLy IS NOT NULL
            AND LEN(LTRIM(RTRIM(KetQuaXuLy)))>0 AND NgayXuLy IS NOT NULL AND NgayXuLy>=NgayBaoCao))
);

-- PK va UNIQUE tu tao index; chi them index cho duong truy van giao dien.
CREATE INDEX IX_SachTheLoai_TheLoai ON dbo.SachTheLoai(MaTheLoai,MaSach);
CREATE INDEX IX_BaiThaoLuanTag_Tag ON dbo.BaiThaoLuanTag(MaTag,MaBaiThaoLuan);
CREATE INDEX IX_BinhLuan_Cha ON dbo.BinhLuan(MaBinhLuanCha,BiAn);
CREATE INDEX IX_LuotDocSach_Ky ON dbo.LuotDocSach(ThoiDiem,MaSach);
CREATE INDEX IX_LuotDocSach_TaiKhoan ON dbo.LuotDocSach(MaSach,MaTaiKhoan,ThoiDiem DESC) WHERE MaTaiKhoan IS NOT NULL;
CREATE INDEX IX_LuotDocSach_Khach ON dbo.LuotDocSach(MaSach,MaKhach,ThoiDiem DESC) WHERE MaKhach IS NOT NULL;
CREATE INDEX IX_Sach_NguoiDang ON dbo.Sach(MaNguoiDang,NgayTao DESC);
CREATE INDEX IX_Sach_CongKhai_Moi ON dbo.Sach(NgayTao DESC,MaSach DESC) WHERE ChoDuyet=0 AND BiAn=0;
CREATE INDEX IX_Sach_CongKhai_DocNhieu ON dbo.Sach(LuotDoc DESC,MaSach DESC) WHERE ChoDuyet=0 AND BiAn=0;
CREATE INDEX IX_DanhGia_Sach ON dbo.DanhGia(MaSach) INCLUDE(SoSao);
CREATE INDEX IX_BaiThaoLuan_DienDan ON dbo.BaiThaoLuan(BiAn,NgayTao DESC,MaBaiThaoLuan);
CREATE INDEX IX_BinhLuan_Sach ON dbo.BinhLuan(MaSach,BiAn,NgayTao,MaBinhLuan) WHERE MaSach IS NOT NULL;
CREATE INDEX IX_BinhLuan_Chuong ON dbo.BinhLuan(MaChuong,BiAn,NgayTao,MaBinhLuan) WHERE MaChuong IS NOT NULL;
CREATE INDEX IX_BinhLuan_Bai ON dbo.BinhLuan(MaBaiThaoLuan,BiAn,NgayTao,MaBinhLuan) WHERE MaBaiThaoLuan IS NOT NULL;
CREATE INDEX IX_BaoCao_HangDoi ON dbo.BaoCao(DaXuLy,MaBaoCao DESC);

INSERT dbo.VaiTro(MaVaiTro,TenVaiTro) VALUES
('Member',N'Thành viên'),('Contributor',N'Thành viên đóng góp'),
('Moderator',N'Kiểm duyệt viên'),('Admin',N'Quản trị viên');
INSERT dbo.TheLoai(TenTheLoai) VALUES
(N'Kỳ ảo'),(N'Phiêu lưu'),(N'Văn học'),(N'Tản văn'),
(N'Trinh thám'),(N'Khoa học'),(N'Lịch sử'),(N'Kỹ năng sống');
EXEC sys.sp_executesql N'CREATE TYPE dbo.DanhSachTepNoiDung AS TABLE (
    ThuTu int NOT NULL PRIMARY KEY,
    DuongDanTep varchar(450) COLLATE Latin1_General_100_BIN2 NOT NULL UNIQUE,
    KieuTep varchar(30) NOT NULL,
    TenTepGoc nvarchar(255) NOT NULL,
    DungLuong bigint NOT NULL
);';

EXEC sys.sp_executesql N'CREATE TRIGGER dbo.trg_BinhLuan_NguCanh ON dbo.BinhLuan
AFTER INSERT,UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.MaBinhLuan=i.MaBinhLuan
        WHERE EXISTS(SELECT i.MaSach,i.MaChuong,i.MaBaiThaoLuan,i.MaBinhLuanCha
                     EXCEPT SELECT d.MaSach,d.MaChuong,d.MaBaiThaoLuan,d.MaBinhLuanCha))
        THROW 50100,N''Khong duoc doi pham vi hoac binh luan cha.'',1;
    -- EXCEPT coi hai NULL la bang nhau, tranh bo sot do logic UNKNOWN.
    IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.BinhLuan p ON p.MaBinhLuan=i.MaBinhLuanCha
        WHERE EXISTS(SELECT i.MaSach,i.MaChuong,i.MaBaiThaoLuan
                     EXCEPT SELECT p.MaSach,p.MaChuong,p.MaBaiThaoLuan))
        THROW 50101,N''Cha va con phai cung sach, cung chuong hoac cung bai thao luan.'',1;
END;';

EXEC sys.sp_executesql N'CREATE TRIGGER dbo.trg_NoiDungChuong_GioiHanAnh ON dbo.NoiDungChuong
AFTER INSERT,UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Lock int;
    SELECT @Lock=c.MaChuong FROM dbo.Chuong c WITH(UPDLOCK,HOLDLOCK)
    WHERE c.MaChuong IN(SELECT MaChuong FROM inserted);
    IF EXISTS(SELECT n.MaChuong FROM dbo.NoiDungChuong n
        WHERE n.MaChuong IN(SELECT MaChuong FROM inserted) AND n.KieuTep IN(''image/jpeg'',''image/png'',''image/webp'')
        GROUP BY n.MaChuong HAVING COUNT(*)>50 OR SUM(n.DungLuong)>52428800)
        THROW 50102,N''Moi chuong toi da 50 anh va tong dung luong anh toi da 50 MiB.'',1;
END;';

EXEC sys.sp_executesql N'CREATE TRIGGER dbo.trg_Sach_KhongDoiLoai ON dbo.Sach
AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.MaSach=i.MaSach
        WHERE i.LoaiTruyen<>d.LoaiTruyen AND EXISTS(SELECT 1 FROM dbo.Chuong c WHERE c.MaSach=i.MaSach))
        THROW 50103,N''Khong doi loai truyen khi da co chuong.'',1;
END;';

EXEC sys.sp_executesql N'CREATE VIEW dbo.vw_SachCongKhai AS
SELECT s.MaSach,s.TenSach,s.TacGia,s.GioiThieu,s.LoaiTruyen,s.TrangThaiSangTac,
       s.MaNguoiDang,s.LuotDoc,s.NgayTao,s.NgayCapNhat,
       STUFF((SELECT N'' · ''+t.TenTheLoai FROM dbo.SachTheLoai st
           JOIN dbo.TheLoai t ON t.MaTheLoai=st.MaTheLoai WHERE st.MaSach=s.MaSach
           ORDER BY st.ThuTu FOR XML PATH(''''),TYPE).value(''.'',''nvarchar(max)''),1,3,N'''') AS TenTheLoai,
       (SELECT COUNT(*) FROM dbo.Chuong c WHERE c.MaSach=s.MaSach) AS SoChuong,
       (SELECT COUNT(*) FROM dbo.DanhGia d WHERE d.MaSach=s.MaSach) AS SoDanhGia,
       (SELECT AVG(CONVERT(decimal(10,6),d.SoSao)) FROM dbo.DanhGia d WHERE d.MaSach=s.MaSach) AS DiemTrungBinh
FROM dbo.Sach s WHERE s.ChoDuyet=0 AND s.BiAn=0;';

EXEC sys.sp_executesql N'CREATE VIEW dbo.vw_BaiThaoLuanCongKhai AS
SELECT p.*,u.TenHienThi FROM dbo.BaiThaoLuan p JOIN dbo.TaiKhoan u ON u.MaTaiKhoan=p.MaTaiKhoan
WHERE p.BiAn=0;';

EXEC sys.sp_executesql N'CREATE VIEW dbo.vw_BinhLuanCongKhai AS
WITH Cay AS (
    SELECT b.MaBinhLuan,b.MaSach,b.MaChuong,b.MaBaiThaoLuan,b.MaBinhLuanCha,b.MaTaiKhoan,b.NoiDung,b.BiAn,b.NgayTao,
        b.MaBinhLuan AS MaGoc
    FROM dbo.BinhLuan b WHERE b.MaBinhLuanCha IS NULL AND b.BiAn=0
      AND ((b.MaSach IS NOT NULL AND EXISTS(SELECT 1 FROM dbo.Sach s WHERE s.MaSach=b.MaSach AND s.ChoDuyet=0 AND s.BiAn=0))
        OR (b.MaChuong IS NOT NULL AND EXISTS(SELECT 1 FROM dbo.Chuong c JOIN dbo.Sach s ON s.MaSach=c.MaSach
            WHERE c.MaChuong=b.MaChuong AND s.ChoDuyet=0 AND s.BiAn=0))
        OR (b.MaBaiThaoLuan IS NOT NULL AND EXISTS(SELECT 1 FROM dbo.vw_BaiThaoLuanCongKhai p WHERE p.MaBaiThaoLuan=b.MaBaiThaoLuan)))
    UNION ALL
    SELECT b.MaBinhLuan,b.MaSach,b.MaChuong,b.MaBaiThaoLuan,b.MaBinhLuanCha,b.MaTaiKhoan,b.NoiDung,b.BiAn,b.NgayTao,c.MaGoc
    FROM dbo.BinhLuan b JOIN Cay c ON c.MaBinhLuan=b.MaBinhLuanCha WHERE b.BiAn=0
)
SELECT c.*,u.TenHienThi FROM Cay c JOIN dbo.TaiKhoan u ON u.MaTaiKhoan=c.MaTaiKhoan;';

EXEC sys.sp_executesql N'CREATE PROCEDURE dbo.usp_ThemChuong
    @MaNguoiThaoTac int,@MaSach int,@TieuDe nvarchar(max),@Tep dbo.DanhSachTepNoiDung READONLY
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @N int=(SELECT COUNT(*) FROM @Tep);
    IF @TieuDe IS NULL OR LEN(LTRIM(RTRIM(@TieuDe)))=0 OR DATALENGTH(@TieuDe)>320
        THROW 50011,N''Tieu de bat buoc, toi da 160 ky tu.'',1;
    IF @N=0 OR (SELECT MIN(ThuTu) FROM @Tep)<>1 OR (SELECT MAX(ThuTu) FROM @Tep)<>@N
        THROW 50012,N''Can it nhat mot tep, thu tu lien tuc tu 1.'',1;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @VaiTro varchar(20),@Chu int,@So int,@MaChuong int;
        SELECT @VaiTro=MaVaiTro FROM dbo.TaiKhoan WITH(HOLDLOCK) WHERE MaTaiKhoan=@MaNguoiThaoTac AND BiKhoa=0;
        IF @VaiTro IS NULL OR @VaiTro NOT IN(''Contributor'',''Admin'') THROW 50002,N''Khong co quyen dang chuong.'',1;
        SELECT @Chu=MaNguoiDang FROM dbo.Sach WITH(UPDLOCK,HOLDLOCK) WHERE MaSach=@MaSach;
        IF @Chu IS NULL THROW 50003,N''Sach khong ton tai.'',1;
        IF @VaiTro<>''Admin'' AND @Chu<>@MaNguoiThaoTac THROW 50004,N''Khong phai sach cua ban.'',1;
        SELECT @So=ISNULL(MAX(SoChuong),0)+1 FROM dbo.Chuong WHERE MaSach=@MaSach;
        INSERT dbo.Chuong(MaSach,SoChuong,TieuDe) VALUES(@MaSach,@So,LTRIM(RTRIM(@TieuDe)));
        SET @MaChuong=CONVERT(int,SCOPE_IDENTITY());
        INSERT dbo.NoiDungChuong(MaChuong,ThuTu,DuongDanTep,KieuTep,TenTepGoc,DungLuong)
        SELECT @MaChuong,ThuTu,DuongDanTep,KieuTep,TenTepGoc,DungLuong FROM @Tep;
        UPDATE dbo.Sach SET ChoDuyet=CASE WHEN @VaiTro=''Admin'' THEN ChoDuyet ELSE 1 END,
            NgayCapNhat=SYSUTCDATETIME() WHERE MaSach=@MaSach;
        COMMIT;
        SELECT @MaChuong AS MaChuong,@So AS SoChuong;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK;
        THROW;
    END CATCH
END;';

EXEC sys.sp_executesql N'CREATE PROCEDURE dbo.usp_DocChuong
    @MaSach int,@SoChuong int,@MaTaiKhoan int=NULL,@MaKhach uniqueidentifier=NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF (@MaTaiKhoan IS NULL AND @MaKhach IS NULL) OR (@MaTaiKhoan IS NOT NULL AND @MaKhach IS NOT NULL)
        THROW 50015,N''Can dung mot dinh danh: tai khoan hoac cookie khach.'',1;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF @MaTaiKhoan IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.TaiKhoan WITH(HOLDLOCK)
            WHERE MaTaiKhoan=@MaTaiKhoan AND BiKhoa=0) THROW 50005,N''Tai khoan khong hop le.'',1;
        DECLARE @Sach int,@MaChuong int,@Luc datetime2(3),@LanTruoc datetime2(3),@DaTinh bit=0;
        SELECT @Sach=MaSach FROM dbo.Sach WITH(UPDLOCK,HOLDLOCK) WHERE MaSach=@MaSach AND ChoDuyet=0 AND BiAn=0;
        IF @Sach IS NULL THROW 50006,N''Sach chua cong khai.'',1;
        SELECT @MaChuong=MaChuong FROM dbo.Chuong WHERE MaSach=@MaSach AND SoChuong=@SoChuong;
        IF @MaChuong IS NULL THROW 50006,N''Chuong khong ton tai.'',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.NoiDungChuong WHERE MaChuong=@MaChuong)
            THROW 50017,N''Chuong chua co tep noi dung.'',1;
        -- Lay gio SAU khi da lay khoa: request doi khoa khong mang moc gio cu.
        SET @Luc=SYSUTCDATETIME();
        IF @MaTaiKhoan IS NOT NULL
            SELECT @LanTruoc=MAX(ThoiDiem) FROM dbo.LuotDocSach WHERE MaSach=@MaSach AND MaTaiKhoan=@MaTaiKhoan;
        ELSE SELECT @LanTruoc=MAX(ThoiDiem) FROM dbo.LuotDocSach WHERE MaSach=@MaSach AND MaKhach=@MaKhach;
        IF @LanTruoc IS NULL OR @LanTruoc<=DATEADD(minute,-30,@Luc)
        BEGIN
            INSERT dbo.LuotDocSach(MaSach,MaTaiKhoan,MaKhach,ThoiDiem) VALUES(@MaSach,@MaTaiKhoan,@MaKhach,@Luc);
            UPDATE dbo.Sach SET LuotDoc=LuotDoc+1 WHERE MaSach=@MaSach;
            SET @DaTinh=1;
        END;
        -- Lich su van doi chuong du lan mo nay khong duoc cong luot doc.
        IF @MaTaiKhoan IS NOT NULL
        BEGIN
            UPDATE dbo.LichSuDoc WITH(UPDLOCK,HOLDLOCK) SET MaChuong=@MaChuong,LanDocCuoi=@Luc
            WHERE MaTaiKhoan=@MaTaiKhoan AND MaSach=@MaSach;
            IF @@ROWCOUNT=0 INSERT dbo.LichSuDoc(MaTaiKhoan,MaSach,MaChuong,LanDocCuoi) VALUES(@MaTaiKhoan,@MaSach,@MaChuong,@Luc);
        END;
        SELECT c.*,s.TenSach,@DaTinh AS DaTinhLuot,
            (SELECT COUNT(*) FROM dbo.Chuong WHERE MaSach=@MaSach) AS TongSoChuong
        FROM dbo.Chuong c JOIN dbo.Sach s ON s.MaSach=c.MaSach WHERE c.MaChuong=@MaChuong;
        SELECT MaNoiDung,ThuTu,DuongDanTep,KieuTep,TenTepGoc,DungLuong FROM dbo.NoiDungChuong WHERE MaChuong=@MaChuong ORDER BY ThuTu;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK;
        THROW;
    END CATCH
END;';

EXEC sys.sp_executesql N'CREATE PROCEDURE dbo.usp_LuuDanhGia
    @MaTaiKhoan int, @MaSach int, @SoSao int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @SoSao IS NULL OR @SoSao NOT BETWEEN 1 AND 5 THROW 50007,N''So sao phai tu 1 den 5.'',1;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF NOT EXISTS(SELECT 1 FROM dbo.TaiKhoan WITH(HOLDLOCK) WHERE MaTaiKhoan=@MaTaiKhoan AND BiKhoa=0)
            THROW 50005,N''Tai khoan khong hop le.'',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.Sach WITH(HOLDLOCK) WHERE MaSach=@MaSach AND ChoDuyet=0 AND BiAn=0)
            THROW 50008,N''Sach chua cong khai.'',1;
        UPDATE dbo.DanhGia WITH(UPDLOCK,HOLDLOCK) SET SoSao=@SoSao,NgayDanhGia=SYSUTCDATETIME()
        WHERE MaTaiKhoan=@MaTaiKhoan AND MaSach=@MaSach;
        IF @@ROWCOUNT=0 INSERT dbo.DanhGia(MaTaiKhoan,MaSach,SoSao) VALUES(@MaTaiKhoan,@MaSach,@SoSao);
        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK;
        THROW;
    END CATCH
END;
';

EXEC sys.sp_executesql N'CREATE PROCEDURE dbo.usp_LuuTuSach
    @MaTaiKhoan int, @MaSach int, @Luu bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @Luu IS NULL THROW 50009,N''Phai chi dinh luu hoac bo luu.'',1;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF NOT EXISTS(SELECT 1 FROM dbo.TaiKhoan WITH(HOLDLOCK) WHERE MaTaiKhoan=@MaTaiKhoan AND BiKhoa=0)
            THROW 50005,N''Tai khoan khong hop le.'',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.Sach WITH(HOLDLOCK) WHERE MaSach=@MaSach AND ChoDuyet=0 AND BiAn=0)
            THROW 50008,N''Sach chua cong khai.'',1;
        IF @Luu=0 DELETE dbo.TuSach WHERE MaTaiKhoan=@MaTaiKhoan AND MaSach=@MaSach;
        ELSE IF NOT EXISTS(SELECT 1 FROM dbo.TuSach WITH(UPDLOCK,HOLDLOCK)
                           WHERE MaTaiKhoan=@MaTaiKhoan AND MaSach=@MaSach)
            INSERT dbo.TuSach(MaTaiKhoan,MaSach) VALUES(@MaTaiKhoan,@MaSach);
        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK;
        THROW;
    END CATCH
END;
';

EXEC sys.sp_executesql N'CREATE PROCEDURE dbo.usp_BangXepHang
    @Ky varchar(5) -- Day / Week / Month, nhu RankingPeriod cua du an
AS
BEGIN
    SET NOCOUNT ON;
    IF @Ky IS NULL OR @Ky NOT IN(''Day'',''Week'',''Month'') THROW 50016,N''Ky phai la Day, Week hoac Month.'',1;
    DECLARE @DenUTC datetime2(3)=SYSUTCDATETIME(),@NgayVN date,@DauVN date,@TuUTC datetime2(3);
    SET @NgayVN=CONVERT(date,DATEADD(hour,7,@DenUTC));
    SET @DauVN=CASE @Ky WHEN ''Day'' THEN @NgayVN
        WHEN ''Week'' THEN DATEADD(day,-(DATEDIFF(day,CONVERT(date,''19000101'',112),@NgayVN)%7),@NgayVN)
        ELSE DATEFROMPARTS(YEAR(@NgayVN),MONTH(@NgayVN),1) END;
    SET @TuUTC=DATEADD(hour,-7,CONVERT(datetime2(3),@DauVN));
    SELECT TOP(10) s.MaSach,s.TenSach,COUNT_BIG(*) AS LuotDocTrongKy
    FROM dbo.LuotDocSach l JOIN dbo.vw_SachCongKhai s ON s.MaSach=l.MaSach
    WHERE l.ThoiDiem>=@TuUTC AND l.ThoiDiem<=@DenUTC
    GROUP BY s.MaSach,s.TenSach
    ORDER BY COUNT_BIG(*) DESC,s.TenSach COLLATE Latin1_General_100_BIN2,s.MaSach;
END;';

EXEC sys.sp_executesql N'CREATE PROCEDURE dbo.usp_ThemBinhLuan
    @MaTaiKhoan int,@NoiDung nvarchar(max),@MaSach int=NULL,@MaChuong int=NULL,
    @MaBaiThaoLuan int=NULL,@MaBinhLuanCha int=NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @NoiDung IS NULL OR LEN(LTRIM(RTRIM(@NoiDung)))=0 OR DATALENGTH(@NoiDung)>4000
        THROW 50020,N''Binh luan bat buoc, toi da 2000 ky tu.'',1;
    IF (CASE WHEN @MaSach IS NULL THEN 0 ELSE 1 END)+(CASE WHEN @MaChuong IS NULL THEN 0 ELSE 1 END)
       +(CASE WHEN @MaBaiThaoLuan IS NULL THEN 0 ELSE 1 END)<>1 THROW 50021,N''Chi chon mot pham vi binh luan.'',1;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF NOT EXISTS(SELECT 1 FROM dbo.TaiKhoan WITH(HOLDLOCK) WHERE MaTaiKhoan=@MaTaiKhoan AND BiKhoa=0)
            THROW 50005,N''Tai khoan khong hop le.'',1;
        IF @MaSach IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Sach WITH(HOLDLOCK) WHERE MaSach=@MaSach AND ChoDuyet=0 AND BiAn=0)
            THROW 50008,N''Sach chua cong khai.'',1;
        IF @MaChuong IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Chuong c WITH(HOLDLOCK)
            JOIN dbo.Sach s WITH(HOLDLOCK) ON s.MaSach=c.MaSach WHERE c.MaChuong=@MaChuong AND s.ChoDuyet=0 AND s.BiAn=0)
            THROW 50006,N''Chuong khong ton tai hoac sach chua cong khai.'',1;
        IF @MaBaiThaoLuan IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.BaiThaoLuan WITH(HOLDLOCK) WHERE MaBaiThaoLuan=@MaBaiThaoLuan AND BiAn=0)
            THROW 50022,N''Bai thao luan khong cong khai.'',1;
        -- Kiem tra TOAN BO to tien va cung pham vi; HOLDLOCK giu trang thai den khi commit.
        DECLARE @Cha int=@MaBinhLuanCha,@Ke int,@SachCha int,@ChuongCha int,@BaiCha int,@An bit;
        WHILE @Cha IS NOT NULL
        BEGIN
            SET @An=NULL; SET @Ke=NULL;
            SELECT @Ke=MaBinhLuanCha,@SachCha=MaSach,@ChuongCha=MaChuong,@BaiCha=MaBaiThaoLuan,@An=BiAn
            FROM dbo.BinhLuan WITH(HOLDLOCK) WHERE MaBinhLuan=@Cha;
            IF @An IS NULL OR @An=1 THROW 50023,N''Binh luan cha/to tien khong hien thi.'',1;
            IF EXISTS(SELECT @MaSach,@MaChuong,@MaBaiThaoLuan EXCEPT SELECT @SachCha,@ChuongCha,@BaiCha)
                THROW 50101,N''Khong duoc tra loi cheo pham vi.'',1;
            SET @Cha=@Ke;
        END;
        INSERT dbo.BinhLuan(MaSach,MaChuong,MaBaiThaoLuan,MaBinhLuanCha,MaTaiKhoan,NoiDung)
        VALUES(@MaSach,@MaChuong,@MaBaiThaoLuan,@MaBinhLuanCha,@MaTaiKhoan,LTRIM(RTRIM(@NoiDung)));
        DECLARE @MaMoi int=CONVERT(int,SCOPE_IDENTITY());
        COMMIT;
        SELECT @MaMoi AS MaBinhLuan;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK;
        THROW;
    END CATCH
END;';

EXEC sys.sp_executesql N'CREATE PROCEDURE dbo.usp_LayTepNoiDung
    @MaNoiDung int,@MaTaiKhoan int=NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Dung endpoint nay cho ca anh/PDF/TXT. Khong cong luot doc khi lay tung tep.
    SELECT n.MaNoiDung,n.MaChuong,n.DuongDanTep,n.KieuTep,n.TenTepGoc,n.DungLuong
    FROM dbo.NoiDungChuong n JOIN dbo.Chuong c ON c.MaChuong=n.MaChuong JOIN dbo.Sach s ON s.MaSach=c.MaSach
    WHERE n.MaNoiDung=@MaNoiDung AND (
       (s.ChoDuyet=0 AND s.BiAn=0) OR EXISTS(SELECT 1 FROM dbo.TaiKhoan u
          WHERE u.MaTaiKhoan=@MaTaiKhoan AND u.BiKhoa=0 AND (u.MaVaiTro IN(''Admin'',''Moderator'')
            OR (u.MaVaiTro=''Contributor'' AND s.MaNguoiDang=u.MaTaiKhoan))));
END;';

COMMIT TRANSACTION;
PRINT N'Da tao SachMoiDaDen: 16 bang, 3 view, 3 trigger, 7 procedure, 1 table type.';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
-- KIEM TRA TU CHON: doi 0 thanh 1, chay rieng phan nay tren SachMoiDaDen thu nghiem.
-- Khong chay dong thoi voi web. Moi test rollback rieng; IDENTITY co the tang.
-- Chi kiem tra metadata, khong tao/kiem tra file tren server.
DECLARE @ChayKiemTra bit=0;
IF @ChayKiemTra=1
BEGIN
SET NOCOUNT ON; SET XACT_ABORT ON;
DECLARE @Setup nvarchar(max)=N'
DECLARE @U int,@A int,@S int,@C1 int,@C2 int,@P int,@BS int,@BC int,@BP int,@Token varchar(32)=REPLACE(CONVERT(varchar(36),NEWID()),''-'','''');
INSERT dbo.TaiKhoan(TenHienThi,Email,MatKhauHash) VALUES(N''Test user'',@Token+''@example.invalid'',N''NOT_A_LOGIN_HASH''); SET @U=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.TaiKhoan(TenHienThi,Email,MatKhauHash,MaVaiTro) VALUES(N''Test author'',''a''+@Token+''@example.invalid'',N''NOT_A_LOGIN_HASH'',''Contributor''); SET @A=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.Sach(TenSach,TacGia,GioiThieu,MaNguoiDang,ChoDuyet) VALUES(N''Sach test'',N''Tac gia'',N''Gioi thieu'',@A,0); SET @S=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.SachTheLoai SELECT TOP(1) @S,MaTheLoai,1 FROM dbo.TheLoai ORDER BY MaTheLoai;
INSERT dbo.Chuong(MaSach,SoChuong,TieuDe) VALUES(@S,1,N''Chuong 1''); SET @C1=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.Chuong(MaSach,SoChuong,TieuDe) VALUES(@S,2,N''Chuong 2''); SET @C2=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.NoiDungChuong(MaChuong,ThuTu,DuongDanTep,KieuTep,TenTepGoc,DungLuong) VALUES
(@C1,1,''test/''+@Token+''/1.txt'',''text/plain'',N''vanban.txt'',100),
(@C1,2,''test/''+@Token+''/2.png'',''image/png'',N''trang.png'',200),
(@C1,3,''test/''+@Token+''/3.pdf'',''application/pdf'',N''tailieu.pdf'',300),
(@C2,1,''test/''+@Token+''/4.md'',''text/markdown'',N''tailieu.md'',100);
INSERT dbo.BaiThaoLuan(MaTaiKhoan,TieuDe,NoiDung) VALUES(@U,N''Chu de doc lap'',N''Van de thao luan''); SET @P=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.BinhLuan(MaSach,MaTaiKhoan,NoiDung) VALUES(@S,@U,N''Ngoai sach''); SET @BS=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.BinhLuan(MaChuong,MaTaiKhoan,NoiDung) VALUES(@C1,@U,N''Trong chuong''); SET @BC=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.BinhLuan(MaBaiThaoLuan,MaTaiKhoan,NoiDung) VALUES(@P,@U,N''Trong bai''); SET @BP=CONVERT(int,SCOPE_IDENTITY());
';
DECLARE @Cases TABLE(Id int IDENTITY PRIMARY KEY,Ten nvarchar(150),Lenh nvarchar(max),Loi int NULL);
INSERT @Cases VALUES(N'TXT anh PDF cung bang va procedure dang tep',N'IF (SELECT COUNT(*) FROM dbo.NoiDungChuong WHERE MaChuong=@C1)<>3 THROW 51000,N''Thieu noi dung.'',1;
DECLARE @T dbo.DanhSachTepNoiDung; INSERT @T VALUES(1,''test/''+@Token+''/new.pdf'',''application/pdf'',N''moi.pdf'',100);
EXEC dbo.usp_ThemChuong @A,@S,N''Chuong moi'',@T;
IF NOT EXISTS(SELECT 1 FROM dbo.Chuong WHERE MaSach=@S AND SoChuong=3) THROW 51000,N''Chua tao chuong.'',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Sach WHERE MaSach=@S AND ChoDuyet=1) THROW 51000,N''Chua cho duyet.'',1;',NULL);
INSERT @Cases VALUES(N'Ba pham vi va an sach khong an dien dan',N'EXEC dbo.usp_ThemBinhLuan @U,N''Tra loi sach'',@MaSach=@S,@MaBinhLuanCha=@BS;
EXEC dbo.usp_ThemBinhLuan @U,N''Tra loi chuong'',@MaChuong=@C1,@MaBinhLuanCha=@BC;
EXEC dbo.usp_ThemBinhLuan @U,N''Tra loi bai'',@MaBaiThaoLuan=@P,@MaBinhLuanCha=@BP;
DECLARE @N int;
SELECT @N=COUNT(*) FROM dbo.vw_BinhLuanCongKhai WHERE MaSach=@S OPTION(MAXRECURSION 0);
IF @N<>2 THROW 51000,N''Tron binh luan sach.'',1;
SELECT @N=COUNT(*) FROM dbo.vw_BinhLuanCongKhai WHERE MaChuong=@C1 OPTION(MAXRECURSION 0);
IF @N<>2 THROW 51000,N''Tron binh luan chuong.'',1;
UPDATE dbo.Sach SET BiAn=1 WHERE MaSach=@S;
SELECT @N=COUNT(*) FROM dbo.vw_BinhLuanCongKhai WHERE MaSach=@S OR MaChuong=@C1 OPTION(MAXRECURSION 0);
IF @N<>0 THROW 51000,N''Lo binh luan sach an.'',1;
SELECT @N=COUNT(*) FROM dbo.vw_BinhLuanCongKhai WHERE MaBaiThaoLuan=@P OPTION(MAXRECURSION 0);
IF @N<>2 THROW 51000,N''Dien dan bi anh huong boi sach.'',1;
UPDATE dbo.BinhLuan SET BiAn=1 WHERE MaBinhLuan=@BP;
SELECT @N=COUNT(*) FROM dbo.vw_BinhLuanCongKhai WHERE MaBaiThaoLuan=@P OPTION(MAXRECURSION 0);
IF @N<>0 THROW 51000,N''Phan hoi cua cha an van hien.'',1;',NULL);
INSERT @Cases VALUES(N'Doc lap 30 phut van cap nhat lich su',N'EXEC dbo.usp_DocChuong @S,1,@U; EXEC dbo.usp_DocChuong @S,2,@U;
IF (SELECT LuotDoc FROM dbo.Sach WHERE MaSach=@S)<>1 THROW 51000,N''Dem trung.'',1;
IF NOT EXISTS(SELECT 1 FROM dbo.LichSuDoc WHERE MaTaiKhoan=@U AND MaSach=@S AND MaChuong=@C2) THROW 51000,N''Sai lich su.'',1;
EXEC dbo.usp_LuuDanhGia @U,@S,5; EXEC dbo.usp_LuuTuSach @U,@S,1;
EXEC dbo.usp_BangXepHang ''Week'';
DECLARE @NoiDung int=(SELECT MIN(MaNoiDung) FROM dbo.NoiDungChuong WHERE MaChuong=@C1);
EXEC dbo.usp_LayTepNoiDung @NoiDung,@U;',NULL);
INSERT @Cases VALUES(N'Cam phan hoi cheo chuong',N'INSERT dbo.BinhLuan(MaChuong,MaBinhLuanCha,MaTaiKhoan,NoiDung) VALUES(@C2,@BC,@U,N''Sai chuong'');',50101);
INSERT @Cases VALUES(N'Cam phan hoi sach sang chuong',N'INSERT dbo.BinhLuan(MaChuong,MaBinhLuanCha,MaTaiKhoan,NoiDung) VALUES(@C1,@BS,@U,N''Sai pham vi'');',50101);
INSERT @Cases VALUES(N'Cam phan hoi bai sang sach',N'INSERT dbo.BinhLuan(MaSach,MaBinhLuanCha,MaTaiKhoan,NoiDung) VALUES(@S,@BP,@U,N''Sai pham vi'');',50101);
INSERT @Cases VALUES(N'Cam chon hai dich',N'INSERT dbo.BinhLuan(MaSach,MaChuong,MaTaiKhoan,NoiDung) VALUES(@S,@C1,@U,N''Hai dich'');',547);
INSERT @Cases VALUES(N'Cam binh luan khong dich',N'INSERT dbo.BinhLuan(MaTaiKhoan,NoiDung) VALUES(@U,N''Khong dich'');',547);
INSERT @Cases VALUES(N'Cam tu tham chieu',N'UPDATE dbo.BinhLuan SET MaBinhLuanCha=MaBinhLuan WHERE MaBinhLuan=@BP;',547);
INSERT @Cases VALUES(N'Cam duong dan ra ngoai kho',N'UPDATE dbo.NoiDungChuong SET DuongDanTep=''../secret.txt'' WHERE MaChuong=@C1 AND ThuTu=1;',547);
INSERT @Cases VALUES(N'Cam thu tu tep trung',N'UPDATE dbo.NoiDungChuong SET ThuTu=1 WHERE MaChuong=@C1 AND ThuTu=2;',2627);
INSERT @Cases VALUES(N'Cam lich su sai sach',N'INSERT dbo.LichSuDoc(MaTaiKhoan,MaSach,MaChuong) VALUES(@U,-1,@C1);',547);
INSERT @Cases VALUES(N'Cam tra loi cha an',N'UPDATE dbo.BinhLuan SET BiAn=1 WHERE MaBinhLuan=@BP; EXEC dbo.usp_ThemBinhLuan @U,N''Noi dung'',@MaBaiThaoLuan=@P,@MaBinhLuanCha=@BP;',50023);
INSERT @Cases VALUES(N'Cam metadata anh qua 5 MiB',N'UPDATE dbo.NoiDungChuong SET DungLuong=5242881 WHERE MaChuong=@C1 AND ThuTu=2;',547);
DECLARE @I int=1,@N int=(SELECT COUNT(*) FROM @Cases),@Ten nvarchar(150),@Cmd nvarchar(max),@Loi int,@ThucTe int;
WHILE @I<=@N
BEGIN
 SELECT @Ten=Ten,@Cmd=@Setup+Lenh,@Loi=Loi FROM @Cases WHERE Id=@I;
 SET @ThucTe=NULL;
 BEGIN TRY
  BEGIN TRANSACTION;
  EXEC sys.sp_executesql @Cmd;
  IF XACT_STATE()<>0 ROLLBACK;
 END TRY
 BEGIN CATCH
  SET @ThucTe=ERROR_NUMBER();
  IF XACT_STATE()<>0 ROLLBACK;
  IF @Loi IS NULL OR @ThucTe<>@Loi BEGIN PRINT N'FAIL: '+@Ten; THROW; END;
 END CATCH;
 IF @Loi IS NOT NULL AND @ThucTe IS NULL BEGIN PRINT N'FAIL: '+@Ten; THROW 51000,N'Du lieu sai khong bi chan.',1; END;
 PRINT N'PASS: '+@Ten;
 SET @I+=1;
END;
PRINT N'Hoan tat 14 ca kiem tra; fixture da rollback.';
END;
GO
