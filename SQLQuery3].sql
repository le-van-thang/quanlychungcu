/* Tạo schema ai nếu chưa có */
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'ai')
    EXEC('CREATE SCHEMA ai');
GO

/* Proc chính: ai.sp_Search */
CREATE OR ALTER PROCEDURE ai.sp_Search
    @q          nvarchar(200),
    @top        int,
    @entityType nvarchar(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @q = ISNULL(@q, N'');
    DECLARE @qLike nvarchar(210) = N'%' + @q + N'%';

    ;WITH T AS (
        /* ===== CƯ DÂN ===== */
        SELECT
            N'CuDan' AS EntityType,
            CAST(cd.CuDanID AS nvarchar(50)) AS EntityId,
            N'Cư dân ' + ISNULL(cd.HoTen, N'(chưa có tên)') AS Title,
            N'Email: ' + ISNULL(cd.Email, N'') +
            N', SĐT: ' + ISNULL(cd.DienThoai, N'') +
            N', Căn hộ: ' + ISNULL(ch.SoCanHo, N'') AS Detail,
            ISNULL(cd.HoTen, N'') + N' ' +
            ISNULL(cd.Email, N'') + N' ' +
            ISNULL(cd.DienThoai, N'') + N' ' +
            ISNULL(ch.SoCanHo, N'') AS SearchText,
            10 AS RankScore
        FROM dbo.CuDan cd
        LEFT JOIN dbo.CanHo ch ON ch.CanHoID = cd.CanHoID

        UNION ALL

        /* ===== CĂN HỘ ===== */
        SELECT
            N'CanHo' AS EntityType,
            CAST(ch.CanHoID AS nvarchar(50)) AS EntityId,
            N'Căn hộ ' + ISNULL(ch.SoCanHo, N'') AS Title,
            N'Tầng: ' + ISNULL(t.TenTang, N'') +
            N', Diện tích: ' + ISNULL(CONVERT(nvarchar(20), ch.DienTich), N'') +
            N', Giá trị: ' + ISNULL(CONVERT(nvarchar(20), ch.GiaTri), N'') AS Detail,
            ISNULL(ch.SoCanHo, N'') + N' ' +
            ISNULL(t.TenTang, N'') AS SearchText,
            20 AS RankScore
        FROM dbo.CanHo ch
        LEFT JOIN dbo.Tang t ON t.TangID = ch.TangID

        UNION ALL

        /* ===== HÓA ĐƠN CƯ DÂN ===== */
        SELECT
            N'HoaDonCuDan' AS EntityType,
            CAST(h.HoaDonID AS nvarchar(50)) AS EntityId,
            N'Hóa đơn cư dân: ' + ISNULL(h.LoaiDichVu, N'') +
            N' - ' + CONVERT(nvarchar(10), h.NgayLap, 120) AS Title,
            N'Cư dân: ' + ISNULL(cd.HoTen, N'') +
            N', Căn hộ: ' + ISNULL(ch.SoCanHo, N'') +
            N', Số tiền: ' + CONVERT(nvarchar(50), h.SoTien) +
            N', Trạng thái: ' + ISNULL(h.TrangThai, N'') AS Detail,
            ISNULL(h.LoaiDichVu, N'') + N' ' +
            ISNULL(h.TrangThai, N'') + N' ' +
            ISNULL(cd.HoTen, N'') + N' ' +
            ISNULL(ch.SoCanHo, N'') AS SearchText,
            30 AS RankScore
        FROM dbo.HoaDonCuDan h
        JOIN dbo.CuDan cd ON cd.CuDanID = h.CuDanID
        JOIN dbo.CanHo ch ON ch.CanHoID = h.CanHoID

        UNION ALL

        /* ===== HÓA ĐƠN THƯƠNG MẠI ===== */
        SELECT
            N'HoaDonTM' AS EntityType,
            CAST(h.HoaDonTMID AS nvarchar(50)) AS EntityId,
            N'Hóa đơn TM: ' + ISNULL(h.NoiDung, N'') +
            N' - ' + CONVERT(nvarchar(10), h.NgayLap, 120) AS Title,
            N'Mặt bằng: ' + ISNULL(mb.TenMatBang, N'') +
            N', Số tiền: ' + CONVERT(nvarchar(50), h.SoTien) +
            N', Trạng thái: ' + ISNULL(h.TrangThai, N'') AS Detail,
            ISNULL(h.NoiDung, N'') + N' ' +
            ISNULL(h.TrangThai, N'') + N' ' +
            ISNULL(mb.TenMatBang, N'') AS SearchText,
            40 AS RankScore
        FROM dbo.HoaDonTM h
        JOIN dbo.MatBangThuongMai mb ON mb.MatBangID = h.MatBangID

        UNION ALL

        /* ===== VẬT TƯ ===== */
        SELECT
            N'VatTu' AS EntityType,
            CAST(v.VatTuID AS nvarchar(50)) AS EntityId,
            N'Vật tư ' + ISNULL(v.TenVatTu, N'') AS Title,
            N'Đơn vị: ' + ISNULL(v.DonVi, N'') +
            N', Số lượng: ' + ISNULL(CONVERT(nvarchar(20), v.SoLuong), N'') +
            N', Giá: ' + ISNULL(CONVERT(nvarchar(20), v.Gia), N'') +
            CASE WHEN v.GhiChu IS NULL OR v.GhiChu = N'' THEN N'' ELSE N', Ghi chú: ' + v.GhiChu END AS Detail,
            ISNULL(v.TenVatTu, N'') + N' ' +
            ISNULL(v.DonVi, N'') + N' ' +
            ISNULL(v.GhiChu, N'') AS SearchText,
            50 AS RankScore
        FROM dbo.VatTu v

        UNION ALL

        /* ===== MẶT BẰNG THƯƠNG MẠI ===== */
        SELECT
            N'MatBangThuongMai' AS EntityType,
            CAST(mb.MatBangID AS nvarchar(50)) AS EntityId,
            N'Mặt bằng ' + ISNULL(mb.TenMatBang, N'') AS Title,
            N'Diện tích: ' + ISNULL(CONVERT(nvarchar(20), mb.DienTich), N'') +
            N', Giá thuê: ' + ISNULL(CONVERT(nvarchar(20), mb.GiaThue), N'') +
            CASE WHEN mb.GhiChu IS NULL OR mb.GhiChu = N'' THEN N'' ELSE N', Ghi chú: ' + mb.GhiChu END AS Detail,
            ISNULL(mb.TenMatBang, N'') + N' ' +
            ISNULL(mb.GhiChu, N'') AS SearchText,
            60 AS RankScore
        FROM dbo.MatBangThuongMai mb

        UNION ALL

        /* ===== XE Ô TÔ ===== */
        SELECT
            N'XeOTo' AS EntityType,
            CAST(x.XeOToID AS nvarchar(50)) AS EntityId,
            N'Ô tô ' + ISNULL(x.BKS, N'') AS Title,
            N'Chủ: ' + ISNULL(cd.HoTen, N'') +
            N', SĐT: ' + ISNULL(cd.DienThoai, N'') AS Detail,
            ISNULL(x.BKS, N'') + N' ' +
            ISNULL(cd.HoTen, N'') + N' ' +
            ISNULL(cd.DienThoai, N'') AS SearchText,
            70 AS RankScore
        FROM dbo.XeOTo x
        JOIN dbo.CuDan cd ON cd.CuDanID = x.CuDanID

        UNION ALL

        /* ===== XE MÁY ===== */
        SELECT
            N'XeMay' AS EntityType,
            CAST(x.XeMayID AS nvarchar(50)) AS EntityId,
            N'Xe máy ' + ISNULL(x.BKS, N'') AS Title,
            N'Chủ: ' + ISNULL(cd.HoTen, N'') +
            N', SĐT: ' + ISNULL(cd.DienThoai, N'') AS Detail,
            ISNULL(x.BKS, N'') + N' ' +
            ISNULL(cd.HoTen, N'') + N' ' +
            ISNULL(cd.DienThoai, N'') AS SearchText,
            80 AS RankScore
        FROM dbo.XeMay x
        JOIN dbo.CuDan cd ON cd.CuDanID = x.CuDanID

        UNION ALL

        /* ===== XE ĐẠP ===== */
        SELECT
            N'XeDap' AS EntityType,
            CAST(x.XeDapID AS nvarchar(50)) AS EntityId,
            N'Xe đạp ' + ISNULL(x.BKS, N'') AS Title,
            N'Chủ: ' + ISNULL(cd.HoTen, N'') +
            N', SĐT: ' + ISNULL(cd.DienThoai, N'') AS Detail,
            ISNULL(x.BKS, N'') + N' ' +
            ISNULL(cd.HoTen, N'') + N' ' +
            ISNULL(cd.DienThoai, N'') AS SearchText,
            90 AS RankScore
        FROM dbo.XeDap x
        JOIN dbo.CuDan cd ON cd.CuDanID = x.CuDanID

        UNION ALL

        /* ===== NHÂN VIÊN ===== */
        SELECT
            N'NhanVien' AS EntityType,
            CAST(nv.NhanVienID AS nvarchar(50)) AS EntityId,
            N'Nhân viên ' + ISNULL(nv.HoTen, N'') AS Title,
            N'SĐT: ' + ISNULL(nv.DienThoai, N'') +
            N', Email: ' + ISNULL(nv.Email, N'') AS Detail,
            ISNULL(nv.HoTen, N'') + N' ' +
            ISNULL(nv.Email, N'') + N' ' +
            ISNULL(nv.DienThoai, N'') AS SearchText,
            100 AS RankScore
        FROM dbo.NhanVien nv

        UNION ALL

        /* ===== TÀI KHOẢN ĐĂNG NHẬP ===== */
        SELECT
            N'TaiKhoan' AS EntityType,
            CAST(tk.TaiKhoanID AS nvarchar(50)) AS EntityId,
            N'Tài khoản ' + ISNULL(tk.Username, N'') AS Title,
            N'Email: ' + ISNULL(tk.Email, N'') +
            N', Vai trò: ' + ISNULL(tk.VaiTro, N'') +
            N', Trạng thái: ' +
                CASE WHEN tk.IsActive = 1 THEN N'Active' ELSE N'Inactive' END AS Detail,
            ISNULL(tk.Username, N'') + N' ' +
            ISNULL(tk.Email, N'') + N' ' +
            ISNULL(tk.VaiTro, N'') AS SearchText,
            110 AS RankScore
        FROM dbo.TaiKhoan tk

        UNION ALL

        /* ===== USER (bảng User) ===== */
        SELECT
            N'User' AS EntityType,
            CAST(u.UserID AS nvarchar(50)) AS EntityId,
            N'User ' + ISNULL(u.FullName, N'') AS Title,
            N'Loại: ' + ISNULL(u.UserType, N'') +
            N', Phone: ' + ISNULL(u.Phone, N'') +
            N', Email: ' + ISNULL(u.Email, N'') AS Detail,
            ISNULL(u.FullName, N'') + N' ' +
            ISNULL(u.UserType, N'') + N' ' +
            ISNULL(u.Phone, N'') + N' ' +
            ISNULL(u.Email, N'') AS SearchText,
            120 AS RankScore
        FROM dbo.[User] u

        UNION ALL

        /* ===== VAI TRÒ ===== */
        SELECT
            N'VaiTro' AS EntityType,
            CAST(vt.VaiTroID AS nvarchar(50)) AS EntityId,
            N'Vai trò ' + ISNULL(vt.TenVaiTro, N'') AS Title,
            N'Tên vai trò: ' + ISNULL(vt.TenVaiTro, N'') AS Detail,
            ISNULL(vt.TenVaiTro, N'') AS SearchText,
            130 AS RankScore
        FROM dbo.VaiTro vt
    )

    SELECT TOP (@top)
        EntityType,
        EntityId,
        Title,
        Detail
    FROM T
    WHERE
        (@entityType IS NULL OR T.EntityType = @entityType)
        AND (@q = N'' OR T.SearchText LIKE @qLike)
    ORDER BY
        RankScore, EntityType, EntityId;
END
GO
