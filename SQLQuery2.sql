CREATE TABLE ActivityLog
(
    LogID       INT IDENTITY(1,1) PRIMARY KEY,
    UserId      INT NULL,               -- FK sang User (nếu có)
    Action      NVARCHAR(50)  NOT NULL, -- "Create", "Update", "Delete", "Login", ...
    EntityName  NVARCHAR(100) NOT NULL, -- "CuDan", "CanHo", "HoaDonTM", ...
    EntityId    NVARCHAR(50)  NULL,     -- Id của bản ghi bị tác động
    Detail      NVARCHAR(MAX) NULL,     -- Mô tả thêm (JSON / text tự do)
    CreatedAt   DATETIME       NOT NULL DEFAULT(GETDATE())
);
