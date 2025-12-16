# Checkout API Response Update - CodeName & CodeValue

## 📋 Summary

Updated Checkout API responses to include **both `codeName` and `codeValue`** for better frontend integration:
- **`codeValue`** (hiển thị): For displaying to users (e.g., "Phòng Tiêu Chuẩn", "Đặt tại quầy")
- **`codeName/Code`** (logic): For programmatic logic (e.g., "Standard", "WalkIn")

---

## 🔄 Changes Made

### 1️⃣ Updated DTOs (CheckoutApiModels.cs)

#### **CheckoutResponse & PreviewCheckoutResponse**
```csharp
public class CheckoutResponse
{
    public string BookingType { get; set; }      // "Online", "Đặt tại quầy"
    public string BookingTypeCode { get; set; }  // "Online", "WalkIn" (NEW)
    // ... other fields
}
```

#### **RoomChargeDetail**
```csharp
public class RoomChargeDetail
{
    public string RoomTypeName { get; set; }  // "Phòng Tiêu Chuẩn"
    public string RoomTypeCode { get; set; }  // "Standard" (NEW)
    // ... other fields
}
```

#### **ServiceChargeDetail**
```csharp
public class ServiceChargeDetail
{
    public string ServiceName { get; set; }  // "Giặt ủi"
    public string ServiceCode { get; set; }  // "Laundry" (NEW)
    // Note: Service model không có code riêng, dùng ServiceName cho cả 2
    // ... other fields
}
```

### 2️⃣ Updated Service Logic (CheckoutService.cs)

#### **PreviewCheckoutAsync()**
```csharp
var response = new PreviewCheckoutResponse
{
    BookingId = booking.BookingId,
    BookingType = booking.BookingType?.CodeValue ?? "Unknown",     // Hiển thị
    BookingTypeCode = booking.BookingType?.CodeName ?? "Unknown", // Logic (NEW)
    // ...
};
```

#### **ProcessCheckoutAsync()**
```csharp
var response = new CheckoutResponse
{
    BookingId = booking.BookingId,
    BookingType = booking.BookingType?.CodeValue ?? "Unknown",     // Hiển thị
    BookingTypeCode = booking.BookingType?.CodeName ?? "Unknown", // Logic (NEW)
    // ...
};
```

#### **CalculateRoomChargesAsync()**
```csharp
roomCharges.Add(new RoomChargeDetail
{
    RoomTypeName = bookingRoom.Room.RoomType.TypeName,  // "Phòng Tiêu Chuẩn"
    RoomTypeCode = bookingRoom.Room.RoomType.TypeCode,  // "Standard" (NEW)
    // ...
});
```

#### **CalculateServiceChargesAsync()**
```csharp
serviceCharges.Add(new ServiceChargeDetail
{
    ServiceName = roomService.Service.ServiceName,  // "Giặt ủi"
    ServiceCode = roomService.Service.ServiceName,  // "Giặt ủi" (NEW - Service không có code riêng)
    // ...
});
```

---

## 📊 New Response Format

### GET /api/Checkout/preview/7

**Updated Response:**
```json
{
  "isSuccess": true,
  "responseCode": null,
  "statusCode": 200,
  "data": {
    "bookingId": 7,
    "bookingType": "Đặt tại quầy",          // ✅ CodeValue - Hiển thị
    "bookingTypeCode": "WalkIn",            // ✨ NEW - CodeName - Logic
    "customer": {
      "customerId": 1,
      "fullName": "nam ",
      "email": "namdnhe176906@fpt.edu.vn",
      "phoneNumber": "0987654321",
      "identityCard": "011203000070"
    },
    "checkInDate": "2025-12-15T13:10:00",
    "checkOutDate": "2025-12-20T13:10:00",
    "totalNights": 5,
    "estimatedCheckOutDate": "2025-12-20T13:10:00",
    "estimatedNights": 5,
    "roomCharges": [
      {
        "bookingRoomId": 30,
        "roomId": 6,
        "roomName": "106",
        "roomTypeName": "Phòng Tiêu Chuẩn",  // ✅ TypeName - Hiển thị
        "roomTypeCode": "Standard",          // ✨ NEW - TypeCode - Logic
        "pricePerNight": 800000.00,
        "plannedNights": 5,
        "actualNights": 5,
        "subTotal": 4000000.00,
        "checkInDate": "2025-12-15T13:10:00",
        "checkOutDate": "2025-12-20T13:10:00"
      },
      {
        "bookingRoomId": 31,
        "roomId": 5,
        "roomName": "105",
        "roomTypeName": "Phòng Tiêu Chuẩn",  // ✅ TypeName - Hiển thị
        "roomTypeCode": "Standard",          // ✨ NEW - TypeCode - Logic
        "pricePerNight": 800000.00,
        "plannedNights": 5,
        "actualNights": 5,
        "subTotal": 4000000.00,
        "checkInDate": "2025-12-15T13:10:00",
        "checkOutDate": "2025-12-20T13:10:00"
      },
      {
        "bookingRoomId": 32,
        "roomId": 12,
        "roomName": "202",
        "roomTypeName": "Phòng Cao Cấp",     // ✅ TypeName - Hiển thị
        "roomTypeCode": "Deluxe",            // ✨ NEW - TypeCode - Logic
        "pricePerNight": 1500000.00,
        "plannedNights": 5,
        "actualNights": 5,
        "subTotal": 7500000.00,
        "checkInDate": "2025-12-15T13:10:00",
        "checkOutDate": "2025-12-20T13:10:00"
      },
      {
        "bookingRoomId": 33,
        "roomId": 11,
        "roomName": "201",
        "roomTypeName": "Phòng Cao Cấp",     // ✅ TypeName - Hiển thị
        "roomTypeCode": "Deluxe",            // ✨ NEW - TypeCode - Logic
        "pricePerNight": 1500000.00,
        "plannedNights": 5,
        "actualNights": 5,
        "subTotal": 7500000.00,
        "checkInDate": "2025-12-15T13:10:00",
        "checkOutDate": "2025-12-20T13:10:00"
      }
    ],
    "totalRoomCharges": 23000000.00,
    "serviceCharges": [
      // If services exist:
      {
        "serviceId": 1,
        "serviceName": "Massage",         // ✅ ServiceName - Hiển thị
        "serviceCode": "Massage",         // ✨ NEW - ServiceCode - Logic
        "pricePerUnit": 300000,
        "quantity": 2,
        "subTotal": 600000,
        "serviceDate": "2024-01-16T10:00:00",
        "serviceType": "RoomService",
        "roomName": "P101"
      }
    ],
    "totalServiceCharges": 0,
    "subTotal": 23000000.00,
    "depositPaid": 0,
    "totalAmount": 23000000.00,
    "amountDue": 23000000.00,
    "message": null
  },
  "message": "Preview checkout thành công"
}
```

---

## 💡 Frontend Usage Guide

### 1. Hiển thị cho người dùng
```typescript
// Sử dụng các field *Name hoặc không có suffix "Code"
<div>Loại booking: {data.bookingType}</div>          // "Đặt tại quầy"
<div>Loại phòng: {room.roomTypeName}</div>           // "Phòng Tiêu Chuẩn"
<div>Dịch vụ: {service.serviceName}</div>            // "Giặt ủi"
```

### 2. Logic xử lý
```typescript
// Sử dụng các field *Code
if (data.bookingTypeCode === 'Online') {
  // Handle online booking logic
  showDepositInfo();
} else if (data.bookingTypeCode === 'WalkIn') {
  // Handle walk-in booking logic
  hideDepositInfo();
}

// Filter by room type code
const standardRooms = rooms.filter(r => r.roomTypeCode === 'Standard');
const deluxeRooms = rooms.filter(r => r.roomTypeCode === 'Deluxe');

// Compare service codes
if (service.serviceCode === 'Laundry') {
  applyLaundryDiscount();
}
```

### 3. TypeScript Interfaces

```typescript
interface PreviewCheckoutResponse {
  bookingId: number;
  bookingType: string;        // "Online", "Đặt tại quầy"
  bookingTypeCode: string;    // "Online", "WalkIn"
  customer: CustomerCheckoutInfo;
  checkInDate: string;
  checkOutDate: string;
  totalNights: number;
  estimatedCheckOutDate?: string;
  estimatedNights?: number;
  roomCharges: RoomChargeDetail[];
  totalRoomCharges: number;
  serviceCharges: ServiceChargeDetail[];
  totalServiceCharges: number;
  subTotal: number;
  depositPaid: number;
  totalAmount: number;
  amountDue: number;
  message?: string;
}

interface RoomChargeDetail {
  bookingRoomId: number;
  roomId: number;
  roomName: string;
  roomTypeName: string;      // "Phòng Tiêu Chuẩn"
  roomTypeCode: string;      // "Standard"
  pricePerNight: number;
  plannedNights: number;
  actualNights: number;
  subTotal: number;
  checkInDate: string;
  checkOutDate: string;
}

interface ServiceChargeDetail {
  serviceId: number;
  serviceName: string;       // "Giặt ủi"
  serviceCode: string;       // "Laundry" (or same as serviceName)
  pricePerUnit: number;
  quantity: number;
  subTotal: number;
  serviceDate: string;
  serviceType: 'RoomService' | 'BookingService';
  roomName?: string;
}

interface CheckoutResponse extends PreviewCheckoutResponse {
  actualCheckOutDate: string;
  actualNights: number;
  paymentMethod: string;
  transactionId: number;
  checkoutProcessedAt: string;
  processedBy: string;
}
```

---

## 🎯 Field Mapping Reference

| Display Field (CodeValue) | Logic Field (CodeName) | Source |
|---------------------------|------------------------|--------|
| `bookingType` | `bookingTypeCode` | `CommonCode.BookingType` |
| `roomTypeName` | `roomTypeCode` | `RoomType.TypeCode` |
| `serviceName` | `serviceCode` | `Service.ServiceName` (no separate code) |

---

## ✅ Testing

### Test the updated API:

```bash
curl -X GET "http://localhost:8080/api/Checkout/preview/7" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json"
```

### Expected changes:
1. ✅ `bookingTypeCode` field added
2. ✅ `roomTypeCode` field added to each room charge
3. ✅ `serviceCode` field added to each service charge

---

## 📝 Notes

1. **Service Code**: Service model không có field `ServiceCode` riêng trong database, nên chúng ta dùng `ServiceName` cho cả hiển thị và logic. Nếu cần distinguish logic code trong tương lai, cần add `ServiceCode` vào Service model.

2. **Backward Compatibility**: Các field cũ (`bookingType`, `roomTypeName`, `serviceName`) vẫn giữ nguyên để không break existing code.

3. **Null Safety**: Tất cả code fields đều có null-coalescing operator (`??`) để đảm bảo không bị null.

---

## 🚀 Next Steps

1. ✅ Build project successfully
2. 🔄 Test API endpoint `/api/Checkout/preview/{bookingId}`
3. 🔄 Update frontend to use new code fields for logic
4. 🔄 Keep display fields for UI

---

**Last Updated:** 2024-12-16
**Backend:** ASP.NET Core 9.0
**API Version:** 1.0.1
