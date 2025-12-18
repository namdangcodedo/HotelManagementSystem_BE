-- Insert sample employee schedules for testing
-- Week: 2025-12-08 to 2025-12-14

-- Note: Make sure you have employees in the database first
-- This script assumes EmployeeId 1, 2, 3 exist

-- Ca Sáng (06:00 - 14:00)
INSERT INTO [EmployeeSchedule] ([EmployeeId], [ShiftDate], [StartTime], [EndTime], [Notes], [CreatedAt], [CreatedBy])
VALUES 
    (1, '2025-12-08', '06:00:00', '14:00:00', 'Ca sáng thứ 2', GETUTCDATE(), 1),
    (2, '2025-12-08', '06:00:00', '14:00:00', 'Ca sáng thứ 2', GETUTCDATE(), 1),
    (1, '2025-12-09', '06:00:00', '14:00:00', 'Ca sáng thứ 3', GETUTCDATE(), 1),
    (3, '2025-12-09', '06:00:00', '14:00:00', 'Ca sáng thứ 3', GETUTCDATE(), 1),
    (2, '2025-12-10', '06:00:00', '14:00:00', 'Ca sáng thứ 4', GETUTCDATE(), 1);

-- Ca Chiều (14:00 - 22:00)
INSERT INTO [EmployeeSchedule] ([EmployeeId], [ShiftDate], [StartTime], [EndTime], [Notes], [CreatedAt], [CreatedBy])
VALUES 
    (3, '2025-12-08', '14:00:00', '22:00:00', 'Ca chiều thứ 2', GETUTCDATE(), 1),
    (2, '2025-12-09', '14:00:00', '22:00:00', 'Ca chiều thứ 3', GETUTCDATE(), 1),
    (1, '2025-12-10', '14:00:00', '22:00:00', 'Ca chiều thứ 4', GETUTCDATE(), 1),
    (3, '2025-12-10', '14:00:00', '22:00:00', 'Ca chiều thứ 4', GETUTCDATE(), 1);

-- Ca Đêm (22:00 - 06:00)
INSERT INTO [EmployeeSchedule] ([EmployeeId], [ShiftDate], [StartTime], [EndTime], [Notes], [CreatedAt], [CreatedBy])
VALUES 
    (1, '2025-12-11', '22:00:00', '06:00:00', 'Ca đêm thứ 5', GETUTCDATE(), 1),
    (2, '2025-12-12', '22:00:00', '06:00:00', 'Ca đêm thứ 6', GETUTCDATE(), 1);

-- Ca Sáng (08:00 - 16:00) - Một ca sáng khác với giờ khác nhau
INSERT INTO [EmployeeSchedule] ([EmployeeId], [ShiftDate], [StartTime], [EndTime], [Notes], [CreatedAt], [CreatedBy])
VALUES 
    (3, '2025-12-11', '08:00:00', '16:00:00', 'Ca sáng thứ 5 (8h-16h)', GETUTCDATE(), 1),
    (1, '2025-12-12', '08:00:00', '16:00:00', 'Ca sáng thứ 6 (8h-16h)', GETUTCDATE(), 1),
    (2, '2025-12-13', '08:00:00', '16:00:00', 'Ca sáng thứ 7 (8h-16h)', GETUTCDATE(), 1);

GO

-- Verify the inserted data
SELECT 
    es.ScheduleId,
    e.FullName as EmployeeName,
    es.ShiftDate,
    es.StartTime,
    es.EndTime,
    es.Notes
FROM [EmployeeSchedule] es
INNER JOIN [Employee] e ON es.EmployeeId = e.EmployeeId
WHERE es.ShiftDate BETWEEN '2025-12-08' AND '2025-12-14'
ORDER BY es.ShiftDate, es.StartTime, e.FullName;

