-- Transaction Module CommonCode Setup
-- This script ensures all required CommonCode entries exist for the Transaction Module

USE hotel_management;
GO

-- ============================================
-- 1. PaymentStatus CommonCodes
-- ============================================
IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'PaymentStatus' AND CodeValue = 'Unpaid')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('PaymentStatus', 'Unpaid', 'Chưa thanh toán', 'Payment has not been made yet', 1, 1, GETDATE());
    PRINT 'Added PaymentStatus: Unpaid';
END

IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'PaymentStatus' AND CodeValue = 'Paid')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('PaymentStatus', 'Paid', 'Đã thanh toán', 'Payment has been completed', 2, 1, GETDATE());
    PRINT 'Added PaymentStatus: Paid';
END

IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'PaymentStatus' AND CodeValue = 'Pending')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('PaymentStatus', 'Pending', 'Đang chờ', 'Payment is pending', 3, 1, GETDATE());
    PRINT 'Added PaymentStatus: Pending';
END

IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'PaymentStatus' AND CodeValue = 'Refunded')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('PaymentStatus', 'Refunded', 'Đã hoàn tiền', 'Payment has been refunded', 4, 1, GETDATE());
    PRINT 'Added PaymentStatus: Refunded';
END

IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'PaymentStatus' AND CodeValue = 'PartiallyPaid')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('PaymentStatus', 'PartiallyPaid', 'Thanh toán một phần', 'Payment has been partially completed', 5, 1, GETDATE());
    PRINT 'Added PaymentStatus: PartiallyPaid';
END

-- ============================================
-- 2. TransactionStatus CommonCodes
-- ============================================
IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'TransactionStatus' AND CodeValue = 'Pending')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('TransactionStatus', 'Pending', 'Đang chờ', 'Transaction is pending', 1, 1, GETDATE());
    PRINT 'Added TransactionStatus: Pending';
END

IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'TransactionStatus' AND CodeValue = 'Completed')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('TransactionStatus', 'Completed', 'Hoàn thành', 'Transaction is completed', 2, 1, GETDATE());
    PRINT 'Added TransactionStatus: Completed';
END

IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'TransactionStatus' AND CodeValue = 'Cancelled')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('TransactionStatus', 'Cancelled', 'Đã hủy', 'Transaction has been cancelled', 3, 1, GETDATE());
    PRINT 'Added TransactionStatus: Cancelled';
END

IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'TransactionStatus' AND CodeValue = 'Failed')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('TransactionStatus', 'Failed', 'Thất bại', 'Transaction has failed', 4, 1, GETDATE());
    PRINT 'Added TransactionStatus: Failed';
END

-- ============================================
-- 3. PaymentMethod CommonCodes (for at-reception payments)
-- ============================================
IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'PaymentMethod' AND CodeValue = 'Cash')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('PaymentMethod', 'Cash', 'Tiền mặt', 'Cash payment at reception', 1, 1, GETDATE());
    PRINT 'Added PaymentMethod: Cash';
END

IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'PaymentMethod' AND CodeValue = 'Card')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('PaymentMethod', 'Card', 'Thẻ', 'Card payment (Credit/Debit)', 2, 1, GETDATE());
    PRINT 'Added PaymentMethod: Card';
END

IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'PaymentMethod' AND CodeValue = 'QR')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('PaymentMethod', 'QR', 'QR Code', 'QR Bank payment', 3, 1, GETDATE());
    PRINT 'Added PaymentMethod: QR';
END

IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'PaymentMethod' AND CodeValue = 'Bank')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('PaymentMethod', 'Bank', 'Chuyển khoản', 'Bank transfer', 4, 1, GETDATE());
    PRINT 'Added PaymentMethod: Bank';
END

IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'PaymentMethod' AND CodeValue = 'PayOS')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('PaymentMethod', 'PayOS', 'PayOS', 'PayOS online payment gateway', 5, 1, GETDATE());
    PRINT 'Added PaymentMethod: PayOS';
END

IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'PaymentMethod' AND CodeValue = 'EWallet')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('PaymentMethod', 'EWallet', 'Ví điện tử', 'E-Wallet payment (Momo, ZaloPay, etc.)', 6, 1, GETDATE());
    PRINT 'Added PaymentMethod: EWallet';
END

-- ============================================
-- 4. DepositStatus CommonCodes
-- ============================================
IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'DepositStatus' AND CodeValue = 'Unpaid')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('DepositStatus', 'Unpaid', 'Chưa đặt cọc', 'Deposit has not been paid', 1, 1, GETDATE());
    PRINT 'Added DepositStatus: Unpaid';
END

IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'DepositStatus' AND CodeValue = 'Paid')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('DepositStatus', 'Paid', 'Đã đặt cọc', 'Deposit has been paid', 2, 1, GETDATE());
    PRINT 'Added DepositStatus: Paid';
END

IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'DepositStatus' AND CodeValue = 'Refunded')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('DepositStatus', 'Refunded', 'Đã hoàn cọc', 'Deposit has been refunded', 3, 1, GETDATE());
    PRINT 'Added DepositStatus: Refunded';
END

IF NOT EXISTS (SELECT 1 FROM CommonCode WHERE CodeType = 'DepositStatus' AND CodeValue = 'Forfeited')
BEGIN
    INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES ('DepositStatus', 'Forfeited', 'Mất cọc', 'Deposit has been forfeited', 4, 1, GETDATE());
    PRINT 'Added DepositStatus: Forfeited';
END

-- ============================================
-- Verification: Display all Transaction-related CommonCodes
-- ============================================
PRINT '';
PRINT '==========================================';
PRINT 'Transaction Module CommonCodes Summary:';
PRINT '==========================================';

SELECT CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive
FROM CommonCode
WHERE CodeType IN ('PaymentStatus', 'TransactionStatus', 'PaymentMethod', 'DepositStatus')
ORDER BY CodeType, DisplayOrder;

PRINT '';
PRINT 'CommonCode setup completed successfully!';
GO
