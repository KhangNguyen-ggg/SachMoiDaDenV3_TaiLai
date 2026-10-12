-- Chay tren database V3 hien co. Khong xoa hay tao lai bang.
USE [SachMoiDaDen];
GO
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.Sach', N'U') IS NULL
        THROW 50201, N'Chua co bang Sach. Chay script 01 de tao database moi.', 1;
    IF COL_LENGTH(N'dbo.Sach', N'HinhAnhBia') IS NULL
        ALTER TABLE dbo.Sach ADD HinhAnhBia varbinary(max) NULL;
    IF COL_LENGTH(N'dbo.Sach', N'KieuAnhBia') IS NULL
        ALTER TABLE dbo.Sach ADD KieuAnhBia varchar(20) NULL;
    -- SQL dong de bien dich sau khi cac cot moi da duoc them.
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.Sach') AND name=N'CK_Sach_AnhBia')
        EXEC(N'ALTER TABLE dbo.Sach WITH CHECK ADD CONSTRAINT CK_Sach_AnhBia CHECK (
            (HinhAnhBia IS NULL AND KieuAnhBia IS NULL) OR
            (HinhAnhBia IS NOT NULL AND KieuAnhBia IS NOT NULL AND
             DATALENGTH(HinhAnhBia) BETWEEN 1 AND 5242880 AND KieuAnhBia IN (''image/jpeg'',''image/png'',''image/webp'')));');
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
GO
