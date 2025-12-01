CREATE TABLE dbo.Ticket
(
    TicketID     INT IDENTITY(1,1) PRIMARY KEY,
    CuDanID      INT           NOT NULL,      -- cư dân gửi
    Title        NVARCHAR(200) NOT NULL,      -- tiêu đề
    Content      NVARCHAR(MAX) NOT NULL,      -- nội dung phản ánh
    Status       NVARCHAR(20)  NOT NULL 
                    CONSTRAINT DF_Ticket_Status DEFAULT N'Mới', -- Mới / Đang xử lý / Hoàn tất
    CreatedAt    DATETIME      NOT NULL 
                    CONSTRAINT DF_Ticket_CreatedAt DEFAULT (GETDATE()),
    UpdatedAt    DATETIME      NULL,
    AssignedTo   INT           NULL           -- nhân viên/manager xử lý (FK TaiKhoan)
);

ALTER TABLE dbo.Ticket
ADD CONSTRAINT FK_Ticket_CuDan
    FOREIGN KEY (CuDanID) REFERENCES dbo.CuDan(CuDanID);

ALTER TABLE dbo.Ticket
ADD CONSTRAINT FK_Ticket_TaiKhoan_Assigned
    FOREIGN KEY (AssignedTo) REFERENCES dbo.TaiKhoan(TaiKhoanID);
