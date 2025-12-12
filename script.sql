IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Account] (
    [AccountId] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(256) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [IsLocked] bit NOT NULL,
    [LastLoginAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_Account] PRIMARY KEY ([AccountId])
);

CREATE TABLE [Amenity] (
    [AmenityId] int NOT NULL IDENTITY,
    [AmenityName] nvarchar(100) NOT NULL,
    [Description] nvarchar(255) NULL,
    [AmenityType] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Amenity] PRIMARY KEY ([AmenityId])
);

CREATE TABLE [BankConfig] (
    [BankConfigId] int NOT NULL IDENTITY,
    [BankName] nvarchar(100) NOT NULL,
    [BankCode] nvarchar(20) NOT NULL,
    [AccountNumber] nvarchar(50) NOT NULL,
    [AccountName] nvarchar(100) NOT NULL,
    [BankBranch] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_BankConfig] PRIMARY KEY ([BankConfigId])
);

CREATE TABLE [CommonCode] (
    [CodeId] int NOT NULL IDENTITY,
    [CodeType] nvarchar(50) NOT NULL,
    [CodeValue] nvarchar(50) NOT NULL,
    [CodeName] nvarchar(100) NOT NULL,
    [Description] nvarchar(255) NULL,
    [DisplayOrder] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_CommonCode] PRIMARY KEY ([CodeId])
);

CREATE TABLE [Holiday] (
    [HolidayId] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [Description] nvarchar(255) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Holiday] PRIMARY KEY ([HolidayId])
);

CREATE TABLE [Role] (
    [RoleId] int NOT NULL IDENTITY,
    [RoleValue] nvarchar(50) NOT NULL,
    [RoleName] nvarchar(50) NOT NULL,
    [Description] nvarchar(255) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Role] PRIMARY KEY ([RoleId])
);

CREATE TABLE [RoomType] (
    [RoomTypeId] int NOT NULL IDENTITY,
    [TypeName] nvarchar(100) NOT NULL,
    [TypeCode] nvarchar(50) NOT NULL,
    [Description] nvarchar(500) NULL,
    [BasePriceNight] decimal(18,2) NOT NULL,
    [MaxOccupancy] int NOT NULL,
    [RoomSize] decimal(10,2) NULL,
    [NumberOfBeds] int NULL,
    [BedType] nvarchar(50) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_RoomType] PRIMARY KEY ([RoomTypeId])
);

CREATE TABLE [Service] (
    [ServiceId] int NOT NULL IDENTITY,
    [ServiceName] nvarchar(100) NOT NULL,
    [Description] nvarchar(255) NULL,
    [Price] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_Service] PRIMARY KEY ([ServiceId])
);

CREATE TABLE [Voucher] (
    [VoucherId] int NOT NULL IDENTITY,
    [Code] nvarchar(50) NOT NULL,
    [Description] nvarchar(255) NULL,
    [DiscountType] int NOT NULL,
    [DiscountValue] decimal(18,2) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [UsageLimit] int NOT NULL,
    [UsedCount] int NOT NULL,
    [ExpiredDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_Voucher] PRIMARY KEY ([VoucherId])
);

CREATE TABLE [ChatSession] (
    [SessionId] uniqueidentifier NOT NULL,
    [AccountId] int NULL,
    [GuestIdentifier] nvarchar(50) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [LastActivityAt] datetime2 NULL,
    [IsSummarized] bit NOT NULL,
    [ConversationSummary] nvarchar(2000) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_ChatSession] PRIMARY KEY ([SessionId]),
    CONSTRAINT [FK_ChatSession_Account_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Account] ([AccountId])
);

CREATE TABLE [Employee] (
    [EmployeeId] int NOT NULL IDENTITY,
    [AccountId] int NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [PhoneNumber] nvarchar(100) NULL,
    [EmployeeTypeId] int NOT NULL,
    [HireDate] date NOT NULL,
    [TerminationDate] date NULL,
    [BaseSalary] decimal(18,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_Employee] PRIMARY KEY ([EmployeeId]),
    CONSTRAINT [FK_Employee_Account_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Account] ([AccountId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Employee_CommonCode_EmployeeTypeId] FOREIGN KEY ([EmployeeTypeId]) REFERENCES [CommonCode] ([CodeId]) ON DELETE CASCADE
);

CREATE TABLE [Notification] (
    [NotificationId] int NOT NULL IDENTITY,
    [AccountId] int NOT NULL,
    [Message] nvarchar(255) NOT NULL,
    [NotificationTypeId] int NOT NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    CONSTRAINT [PK_Notification] PRIMARY KEY ([NotificationId]),
    CONSTRAINT [FK_Notification_Account_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Account] ([AccountId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Notification_CommonCode_NotificationTypeId] FOREIGN KEY ([NotificationTypeId]) REFERENCES [CommonCode] ([CodeId]) ON DELETE CASCADE
);

CREATE TABLE [AccountRole] (
    [AccountId] int NOT NULL,
    [RoleId] int NOT NULL,
    CONSTRAINT [PK_AccountRole] PRIMARY KEY ([AccountId], [RoleId]),
    CONSTRAINT [FK_AccountRole_Account_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Account] ([AccountId]) ON DELETE CASCADE,
    CONSTRAINT [FK_AccountRole_Role_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Role] ([RoleId]) ON DELETE CASCADE
);

CREATE TABLE [Comment] (
    [CommentId] int NOT NULL IDENTITY,
    [RoomTypeId] int NULL,
    [ReplyId] int NULL,
    [AccountId] int NULL,
    [Content] nvarchar(max) NULL,
    [Rating] int NULL,
    [CreatedDate] datetime2 NULL,
    [CreatedTime] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    [Status] nvarchar(max) NULL,
    CONSTRAINT [PK_Comment] PRIMARY KEY ([CommentId]),
    CONSTRAINT [FK_Comment_Account_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Account] ([AccountId]),
    CONSTRAINT [FK_Comment_Comment_ReplyId] FOREIGN KEY ([ReplyId]) REFERENCES [Comment] ([CommentId]),
    CONSTRAINT [FK_Comment_RoomType_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [RoomType] ([RoomTypeId])
);

CREATE TABLE [Room] (
    [RoomId] int NOT NULL IDENTITY,
    [RoomName] nvarchar(100) NOT NULL,
    [RoomTypeId] int NOT NULL,
    [StatusId] int NOT NULL,
    [Description] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_Room] PRIMARY KEY ([RoomId]),
    CONSTRAINT [FK_Room_CommonCode_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [CommonCode] ([CodeId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Room_RoomType_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [RoomType] ([RoomTypeId]) ON DELETE CASCADE
);

CREATE TABLE [ChatMessage] (
    [MessageId] uniqueidentifier NOT NULL,
    [SessionId] uniqueidentifier NOT NULL,
    [Role] nvarchar(10) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [Metadata] nvarchar(max) NULL,
    [TokenCount] int NULL,
    CONSTRAINT [PK_ChatMessage] PRIMARY KEY ([MessageId]),
    CONSTRAINT [FK_ChatMessage_ChatSession_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [ChatSession] ([SessionId]) ON DELETE CASCADE
);

CREATE TABLE [Attendance] (
    [AttendanceId] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [DeviceEmployeeId] nvarchar(100) NULL,
    [CheckIn] datetime2 NOT NULL,
    [CheckOut] datetime2 NULL,
    [OvertimeHours] decimal(18,2) NULL,
    [Notes] nvarchar(255) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [Status] nvarchar(255) NULL,
    [IsApproved] nvarchar(255) NULL,
    CONSTRAINT [PK_Attendance] PRIMARY KEY ([AttendanceId]),
    CONSTRAINT [FK_Attendance_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([EmployeeId]) ON DELETE CASCADE
);

CREATE TABLE [EmpAttendInfo] (
    [AttendInfoId] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [Year] int NOT NULL,
    [TotalLeaveRequest] int NULL,
    [RemainLeaveRequest] int NULL,
    [UsedLeaveRequest] int NULL,
    [OverLeaveDay] int NULL,
    CONSTRAINT [PK_EmpAttendInfo] PRIMARY KEY ([AttendInfoId]),
    CONSTRAINT [FK_EmpAttendInfo_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([EmployeeId]) ON DELETE CASCADE
);

CREATE TABLE [EmployeeSchedule] (
    [ScheduleId] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [ShiftDate] date NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [Notes] nvarchar(255) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_EmployeeSchedule] PRIMARY KEY ([ScheduleId]),
    CONSTRAINT [FK_EmployeeSchedule_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([EmployeeId]) ON DELETE CASCADE
);

CREATE TABLE [PayrollDisbursement] (
    [PayrollDisbursementId] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [PayrollMonth] int NOT NULL,
    [PayrollYear] int NOT NULL,
    [BaseSalary] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [DisbursedAmount] decimal(18,2) NOT NULL,
    [StatusId] int NOT NULL,
    [DisbursedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_PayrollDisbursement] PRIMARY KEY ([PayrollDisbursementId]),
    CONSTRAINT [FK_PayrollDisbursement_CommonCode_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [CommonCode] ([CodeId]) ON DELETE CASCADE,
    CONSTRAINT [FK_PayrollDisbursement_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([EmployeeId]) ON DELETE NO ACTION
);

CREATE TABLE [SalaryInfo] (
    [SalaryInfoId] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [Year] int NOT NULL,
    [BaseSalary] decimal(18,2) NOT NULL,
    [YearBonus] decimal(18,2) NULL,
    [Allowance] decimal(18,2) NULL,
    [CreatedAt] datetime2 NULL DEFAULT ((getdate())),
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_SalaryInfo] PRIMARY KEY ([SalaryInfoId]),
    CONSTRAINT [FK_SalaryInfo_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([EmployeeId]) ON DELETE CASCADE
);

CREATE TABLE [SalaryRecord] (
    [SalaryRecordId] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [Month] int NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [StatusId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_SalaryRecord] PRIMARY KEY ([SalaryRecordId]),
    CONSTRAINT [FK_SalaryRecord_CommonCode_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [CommonCode] ([CodeId]) ON DELETE CASCADE,
    CONSTRAINT [FK_SalaryRecord_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([EmployeeId]) ON DELETE NO ACTION
);

CREATE TABLE [HolidayPricings] (
    [HolidayPricingId] int NOT NULL IDENTITY,
    [HolidayId] int NOT NULL,
    [RoomId] int NULL,
    [ServiceId] int NULL,
    [PriceAdjustment] decimal(18,2) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_HolidayPricings] PRIMARY KEY ([HolidayPricingId]),
    CONSTRAINT [FK_HolidayPricings_Holiday_HolidayId] FOREIGN KEY ([HolidayId]) REFERENCES [Holiday] ([HolidayId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_HolidayPricings_Room_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Room] ([RoomId]),
    CONSTRAINT [FK_HolidayPricings_Service_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Service] ([ServiceId])
);

CREATE TABLE [HousekeepingTask] (
    [TaskId] int NOT NULL IDENTITY,
    [RoomId] int NOT NULL,
    [JanitorId] int NULL,
    [TaskTypeId] int NOT NULL,
    [StatusId] int NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [Notes] nvarchar(255) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_HousekeepingTask] PRIMARY KEY ([TaskId]),
    CONSTRAINT [FK_HousekeepingTask_CommonCode_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [CommonCode] ([CodeId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_HousekeepingTask_CommonCode_TaskTypeId] FOREIGN KEY ([TaskTypeId]) REFERENCES [CommonCode] ([CodeId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_HousekeepingTask_Employee_JanitorId] FOREIGN KEY ([JanitorId]) REFERENCES [Employee] ([EmployeeId]),
    CONSTRAINT [FK_HousekeepingTask_Room_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Room] ([RoomId]) ON DELETE CASCADE
);

CREATE TABLE [Medium] (
    [MediaId] int NOT NULL IDENTITY,
    [PublishId] nvarchar(100) NULL,
    [FilePath] nvarchar(255) NOT NULL,
    [Description] nvarchar(255) NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [ReferenceTable] nvarchar(50) NOT NULL,
    [ReferenceKey] nvarchar(50) NOT NULL,
    [IsActive] bit NOT NULL,
    [RoomId] int NULL,
    [RoomTypeId] int NULL,
    [ServiceId] int NULL,
    CONSTRAINT [PK_Medium] PRIMARY KEY ([MediaId]),
    CONSTRAINT [FK_Medium_RoomType_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [RoomType] ([RoomTypeId]),
    CONSTRAINT [FK_Medium_Room_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Room] ([RoomId]),
    CONSTRAINT [FK_Medium_Service_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Service] ([ServiceId])
);

CREATE TABLE [RoomAmenity] (
    [RoomId] int NOT NULL,
    [AmenityId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    CONSTRAINT [PK_RoomAmenity] PRIMARY KEY ([RoomId], [AmenityId]),
    CONSTRAINT [FK_RoomAmenity_Amenity_AmenityId] FOREIGN KEY ([AmenityId]) REFERENCES [Amenity] ([AmenityId]) ON DELETE CASCADE,
    CONSTRAINT [FK_RoomAmenity_Room_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Room] ([RoomId]) ON DELETE CASCADE
);

CREATE TABLE [Customer] (
    [CustomerId] int NOT NULL IDENTITY,
    [AccountId] int NULL,
    [FullName] nvarchar(100) NOT NULL,
    [IdentityCard] nvarchar(20) NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [Address] nvarchar(255) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [AvatarMediaId] int NULL,
    CONSTRAINT [PK_Customer] PRIMARY KEY ([CustomerId]),
    CONSTRAINT [FK_Customer_Account_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Account] ([AccountId]),
    CONSTRAINT [FK_Customer_Medium_AvatarMediaId] FOREIGN KEY ([AvatarMediaId]) REFERENCES [Medium] ([MediaId])
);

CREATE TABLE [Booking] (
    [BookingId] int NOT NULL IDENTITY,
    [CustomerId] int NOT NULL,
    [CheckInDate] datetime2 NOT NULL,
    [CheckOutDate] datetime2 NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [DepositAmount] decimal(18,2) NOT NULL,
    [StatusId] int NULL,
    [BookingTypeId] int NULL,
    [SpecialRequests] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RoomId] int NULL,
    CONSTRAINT [PK_Booking] PRIMARY KEY ([BookingId]),
    CONSTRAINT [FK_Booking_CommonCode_BookingTypeId] FOREIGN KEY ([BookingTypeId]) REFERENCES [CommonCode] ([CodeId]),
    CONSTRAINT [FK_Booking_CommonCode_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [CommonCode] ([CodeId]),
    CONSTRAINT [FK_Booking_Customer_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customer] ([CustomerId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Booking_Room_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Room] ([RoomId])
);

CREATE TABLE [BookingRoom] (
    [BookingRoomId] int NOT NULL IDENTITY,
    [BookingId] int NOT NULL,
    [RoomId] int NOT NULL,
    [PricePerNight] decimal(18,2) NOT NULL,
    [NumberOfNights] int NOT NULL,
    [SubTotal] decimal(18,2) NOT NULL,
    [CheckInDate] datetime2 NOT NULL,
    [CheckOutDate] datetime2 NOT NULL,
    CONSTRAINT [PK_BookingRoom] PRIMARY KEY ([BookingRoomId]),
    CONSTRAINT [FK_BookingRoom_Booking_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Booking] ([BookingId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BookingRoom_Room_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Room] ([RoomId]) ON DELETE NO ACTION
);

CREATE TABLE [BookingService] (
    [BookingServiceId] int NOT NULL IDENTITY,
    [BookingId] int NOT NULL,
    [ServiceId] int NOT NULL,
    [Quantity] int NOT NULL,
    [PriceAtTime] decimal(18,2) NOT NULL,
    [TotalPrice] decimal(18,2) NOT NULL,
    [ServiceDate] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    CONSTRAINT [PK_BookingService] PRIMARY KEY ([BookingServiceId]),
    CONSTRAINT [FK_BookingService_Booking_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Booking] ([BookingId]) ON DELETE CASCADE,
    CONSTRAINT [FK_BookingService_Service_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Service] ([ServiceId]) ON DELETE CASCADE
);

CREATE TABLE [Transaction] (
    [TransactionId] int NOT NULL IDENTITY,
    [BookingId] int NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaymentMethodId] int NOT NULL,
    [PaymentStatusId] int NOT NULL,
    [TransactionRef] nvarchar(100) NULL,
    [OrderCode] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [DepositAmount] decimal(18,2) NULL,
    [DepositStatusId] int NULL,
    [DepositDate] datetime2 NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [TransactionStatusId] int NOT NULL,
    CONSTRAINT [PK_Transaction] PRIMARY KEY ([TransactionId]),
    CONSTRAINT [FK_Transaction_Booking_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Booking] ([BookingId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Transaction_CommonCode_DepositStatusId] FOREIGN KEY ([DepositStatusId]) REFERENCES [CommonCode] ([CodeId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Transaction_CommonCode_PaymentMethodId] FOREIGN KEY ([PaymentMethodId]) REFERENCES [CommonCode] ([CodeId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Transaction_CommonCode_PaymentStatusId] FOREIGN KEY ([PaymentStatusId]) REFERENCES [CommonCode] ([CodeId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Transaction_CommonCode_TransactionStatusId] FOREIGN KEY ([TransactionStatusId]) REFERENCES [CommonCode] ([CodeId]) ON DELETE NO ACTION
);

CREATE TABLE [BookingRoomService] (
    [BookingRoomServiceId] int NOT NULL IDENTITY,
    [BookingRoomId] int NOT NULL,
    [ServiceId] int NOT NULL,
    [PriceAtTime] decimal(18,2) NOT NULL,
    [Quantity] int NOT NULL,
    CONSTRAINT [PK_BookingRoomService] PRIMARY KEY ([BookingRoomServiceId]),
    CONSTRAINT [FK_BookingRoomService_BookingRoom_BookingRoomId] FOREIGN KEY ([BookingRoomId]) REFERENCES [BookingRoom] ([BookingRoomId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BookingRoomService_Service_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Service] ([ServiceId]) ON DELETE NO ACTION
);

CREATE INDEX [IX_AccountRole_RoleId] ON [AccountRole] ([RoleId]);

CREATE INDEX [IX_Attendance_EmployeeId] ON [Attendance] ([EmployeeId]);

CREATE INDEX [IX_Booking_BookingTypeId] ON [Booking] ([BookingTypeId]);

CREATE INDEX [IX_Booking_CustomerId] ON [Booking] ([CustomerId]);

CREATE INDEX [IX_Booking_RoomId] ON [Booking] ([RoomId]);

CREATE INDEX [IX_Booking_StatusId] ON [Booking] ([StatusId]);

CREATE INDEX [IX_BookingRoom_BookingId] ON [BookingRoom] ([BookingId]);

CREATE INDEX [IX_BookingRoom_RoomId] ON [BookingRoom] ([RoomId]);

CREATE INDEX [IX_BookingRoomService_BookingRoomId] ON [BookingRoomService] ([BookingRoomId]);

CREATE INDEX [IX_BookingRoomService_ServiceId] ON [BookingRoomService] ([ServiceId]);

CREATE INDEX [IX_BookingService_BookingId] ON [BookingService] ([BookingId]);

CREATE INDEX [IX_BookingService_ServiceId] ON [BookingService] ([ServiceId]);

CREATE INDEX [IX_ChatMessage_SessionId] ON [ChatMessage] ([SessionId]);

CREATE INDEX [IX_ChatSession_AccountId] ON [ChatSession] ([AccountId]);

CREATE INDEX [IX_Comment_AccountId] ON [Comment] ([AccountId]);

CREATE INDEX [IX_Comment_ReplyId] ON [Comment] ([ReplyId]);

CREATE INDEX [IX_Comment_RoomTypeId] ON [Comment] ([RoomTypeId]);

CREATE UNIQUE INDEX [IX_CommonCode_CodeType_CodeValue] ON [CommonCode] ([CodeType], [CodeValue]);

CREATE UNIQUE INDEX [IX_Customer_AccountId] ON [Customer] ([AccountId]) WHERE [AccountId] IS NOT NULL;

CREATE INDEX [IX_Customer_AvatarMediaId] ON [Customer] ([AvatarMediaId]);

CREATE INDEX [IX_EmpAttendInfo_EmployeeId] ON [EmpAttendInfo] ([EmployeeId]);

CREATE UNIQUE INDEX [IX_Employee_AccountId] ON [Employee] ([AccountId]);

CREATE INDEX [IX_Employee_EmployeeTypeId] ON [Employee] ([EmployeeTypeId]);

CREATE INDEX [IX_EmployeeSchedule_EmployeeId] ON [EmployeeSchedule] ([EmployeeId]);

CREATE INDEX [IX_HolidayPricings_HolidayId] ON [HolidayPricings] ([HolidayId]);

CREATE INDEX [IX_HolidayPricings_RoomId] ON [HolidayPricings] ([RoomId]);

CREATE INDEX [IX_HolidayPricings_ServiceId] ON [HolidayPricings] ([ServiceId]);

CREATE INDEX [IX_HousekeepingTask_JanitorId] ON [HousekeepingTask] ([JanitorId]);

CREATE INDEX [IX_HousekeepingTask_RoomId] ON [HousekeepingTask] ([RoomId]);

CREATE INDEX [IX_HousekeepingTask_StatusId] ON [HousekeepingTask] ([StatusId]);

CREATE INDEX [IX_HousekeepingTask_TaskTypeId] ON [HousekeepingTask] ([TaskTypeId]);

CREATE INDEX [IX_Medium_RoomId] ON [Medium] ([RoomId]);

CREATE INDEX [IX_Medium_RoomTypeId] ON [Medium] ([RoomTypeId]);

CREATE INDEX [IX_Medium_ServiceId] ON [Medium] ([ServiceId]);

CREATE INDEX [IX_Notification_AccountId] ON [Notification] ([AccountId]);

CREATE INDEX [IX_Notification_NotificationTypeId] ON [Notification] ([NotificationTypeId]);

CREATE INDEX [IX_PayrollDisbursement_EmployeeId] ON [PayrollDisbursement] ([EmployeeId]);

CREATE INDEX [IX_PayrollDisbursement_StatusId] ON [PayrollDisbursement] ([StatusId]);

CREATE INDEX [IX_Room_RoomTypeId] ON [Room] ([RoomTypeId]);

CREATE INDEX [IX_Room_StatusId] ON [Room] ([StatusId]);

CREATE INDEX [IX_RoomAmenity_AmenityId] ON [RoomAmenity] ([AmenityId]);

CREATE INDEX [IX_SalaryInfo_EmployeeId] ON [SalaryInfo] ([EmployeeId]);

CREATE INDEX [IX_SalaryRecord_EmployeeId] ON [SalaryRecord] ([EmployeeId]);

CREATE INDEX [IX_SalaryRecord_StatusId] ON [SalaryRecord] ([StatusId]);

CREATE INDEX [IX_Transaction_BookingId] ON [Transaction] ([BookingId]);

CREATE INDEX [IX_Transaction_DepositStatusId] ON [Transaction] ([DepositStatusId]);

CREATE INDEX [IX_Transaction_PaymentMethodId] ON [Transaction] ([PaymentMethodId]);

CREATE INDEX [IX_Transaction_PaymentStatusId] ON [Transaction] ([PaymentStatusId]);

CREATE INDEX [IX_Transaction_TransactionStatusId] ON [Transaction] ([TransactionStatusId]);

CREATE UNIQUE INDEX [IX_Voucher_Code] ON [Voucher] ([Code]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251212175039_Initial', N'9.0.0');

COMMIT;
GO

