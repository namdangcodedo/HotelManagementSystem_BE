RoomName# CommonCode Usage and Recent Model Changes

## CommonCode Foreign Key Relationships

CommonCode is used as a lookup table for various types and statuses throughout the system. Below is a complete list of all models that reference CommonCode:

### 1. **Room Model**
- `StatusId` → CommonCode (Room status: Available, Occupied, Maintenance, etc.)

### 2. **Employee Model**
- `EmployeeTypeId` → CommonCode (Employee types: Full-time, Part-time, Contract, etc.)

### 3. **Booking Model**
- `PaymentStatusId` → CommonCode (Payment status: Pending, Paid, Refunded, etc.)
- `DepositStatusId` → CommonCode (Deposit status: Not Paid, Partial, Full, etc.)
- `BookingTypeId` → CommonCode (Booking type: Online, Walk-in, Phone, etc.)

### 4. **Transaction Model**
- `PaymentMethodId` → CommonCode (Payment methods: Cash, Credit Card, Bank Transfer, PayOS, etc.)
- `PaymentStatusId` → CommonCode (Payment status)
- `TransactionStatusId` → CommonCode (Transaction status: Pending, Completed, Failed, etc.)
- `DepositStatusId` → CommonCode (Deposit status)

### 5. **HousekeepingTask Model**
- `TaskTypeId` → CommonCode (Task types: Cleaning, Maintenance, Inspection, etc.)
- `StatusId` → CommonCode (Task status: Pending, In Progress, Completed, etc.)

### 6. **Feedback Model**
- `FeedbackTypeId` → CommonCode (Feedback types: Complaint, Suggestion, Praise, etc.)
- `StatusId` → CommonCode (Feedback status: New, Under Review, Resolved, etc.)

### 7. **Notification Model**
- `NotificationTypeId` → CommonCode (Notification types: Booking, Payment, System, etc.)

### 8. **Salary Model**
- `StatusId` → CommonCode (Salary status: Pending, Paid, Cancelled, etc.)

## Recent Model Changes (October 22, 2025)

### 1. **Room Model Changes**
**Changes:**
- ✅ Replaced `RoomNumber` (string, max 20 chars) with `RoomName` (string, max 100 chars)
- ✅ Removed `FloorNumber` property
- ✅ Kept `StatusId` FK to CommonCode
- ✅ Kept `RoomTypeId` FK to RoomType entity

**Rationale:**
- More flexible naming convention for rooms
- FloorNumber was redundant and can be part of RoomName if needed

### 2. **Holiday Model Changes**
**Changes:**
- ✅ Removed `ExpiredDate` property
- ✅ Kept `StartDate` and `EndDate` for the holiday period

**Rationale:**
- `ExpiredDate` was redundant since `EndDate` already defines when the holiday ends
- Simplified the model structure

### 3. **HolidayPricing Model Changes**
**Changes:**
- ✅ Confirmed pricing is **ONLY FOR NIGHT RATES** (per-night pricing)
- ✅ Does NOT support hourly pricing
- ✅ Comment clarifies: `PriceAdjustment` is added to `BasePriceNight` to get the holiday rate

**Important Notes:**
- HolidayPricing applies to overnight bookings only
- Formula: `Holiday Night Price = Room.RoomType.BasePriceNight + HolidayPricing.PriceAdjustment`
- Example: Base price 800,000 VNĐ + adjustment 200,000 VNĐ = 1,000,000 VNĐ per night

### 4. **DbContext Changes**
**Changes:**
- ✅ Added missing FK configuration for `Salary.Status` → `CommonCode`
- ✅ All CommonCode relationships use `DeleteBehavior.Restrict` to prevent cascading deletes
- ✅ Verified all type/status FKs are properly configured

## Database Context Configuration Summary

All CommonCode foreign keys are configured with:
- `OnDelete(DeleteBehavior.Restrict)` - Prevents accidental deletion of CommonCode entries that are in use
- One-to-many relationships where CommonCode is the "one" side
- No navigation property back from CommonCode to referencing entities (to keep CommonCode clean)

## Migration Required

After these changes, you need to create and apply a new migration:

```bash
# Navigate to the project directory
cd AppBackend.ApiCore

# Create migration
dotnet ef migrations add RemoveFloorNumberAndExpiredDate --project ../AppBackend.BusinessObjects

# Apply migration
dotnet ef database update --project ../AppBackend.BusinessObjects
```

## Notes for Development Team

1. **Room Naming**: Update any UI/forms to use RoomName instead of RoomNumber
2. **Holiday Pricing**: Only implement night-based pricing calculations, no hourly rates
3. **CommonCode**: Be careful when deleting CommonCode entries - ensure they're not referenced by any active records
4. **Data Migration**: Existing RoomNumber data should be migrated to RoomName before removing the column

