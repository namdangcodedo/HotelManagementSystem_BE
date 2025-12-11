# Room Type Search API - Hướng Dẫn Chi Tiết

## 🎯 Tổng Quan

API tìm kiếm loại phòng theo ngày check-in/out và hiển thị số lượng phòng khả dụng cho mỗi loại.

**Endpoint:** `GET /api/room/types/search`

**Quyền truy cập:** Public (AllowAnonymous)

---

## 📋 Query Parameters

| Parameter | Type | Required | Mô tả | Ví dụ |
|-----------|------|----------|-------|-------|
| `checkInDate` | DateTime (yyyy-MM-dd) | ✅ **YES** | Ngày nhận phòng | `2025-12-20` |
| `checkOutDate` | DateTime (yyyy-MM-dd) | ✅ **YES** | Ngày trả phòng | `2025-12-22` |
| `numberOfGuests` | int? | ❌ | Số lượng khách (lọc phòng có sức chứa >= con số này) | `2` |
| `minPrice` | decimal? | ❌ | Giá tối thiểu/đêm (VND) | `500000` |
| `maxPrice` | decimal? | ❌ | Giá tối đa/đêm (VND) | `2000000` |
| `bedType` | string | ❌ | Loại giường | `King` hoặc `Queen` hoặc `Twin` |
| `minRoomSize` | decimal? | ❌ | Diện tích tối thiểu (m²) | `30` |
| `onlyActive` | bool | ❌ | Chỉ hiển thị phòng active (mặc định: true) | `true` |

---

## 🔄 Ví Dụ Request

### 1️⃣ Tìm tất cả phòng khả dụng (đơn giản)
```
GET /api/room/types/search?checkInDate=2025-12-20&checkOutDate=2025-12-22
```

**Kết quả:** Hiển thị tất cả loại phòng có phòng trống trong khoảng 20/12 - 22/12

---

### 2️⃣ Tìm phòng cho 2 khách, giá 500k-2M
```
GET /api/room/types/search?checkInDate=2025-12-20&checkOutDate=2025-12-22&numberOfGuests=2&minPrice=500000&maxPrice=2000000
```

**Điều kiện lọc:**
- Ngày: 20/12 - 22/12
- Sức chứa tối thiểu: 2 khách
- Giá: 500k - 2M/đêm

---

### 3️⃣ Tìm phòng King giá 1-2M
```
GET /api/room/types/search?checkInDate=2025-12-20&checkOutDate=2025-12-23&bedType=King&minPrice=1000000&maxPrice=2000000
```

**Điều kiện lọc:**
- Ngày: 20/12 - 23/12
- Loại giường: King
- Giá: 1M - 2M/đêm

---

### 4️⃣ Tìm phòng cho 3+ khách, diện tích 40m²
```
GET /api/room/types/search?checkInDate=2025-12-20&checkOutDate=2025-12-25&numberOfGuests=3&minRoomSize=40
```

**Điều kiện lọc:**
- Ngày: 20/12 - 25/12 (5 đêm)
- Sức chứa tối thiểu: 3 khách
- Diện tích tối thiểu: 40m²

---

## 📤 Response Success (Status 200)

### Full Response Example
```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "message": "Tìm thấy 5 loại phòng khả dụng từ 2025-12-20 đến 2025-12-22",
  "statusCode": 200,
  "data": [
    {
      "roomTypeId": 1,
      "typeName": "Deluxe Room",
      "typeCode": "DLX",
      "description": "Phòng hướng biển với view tuyệt đẹp",
      "basePriceNight": 1500000,
      "maxOccupancy": 2,
      "roomSize": 35.5,
      "numberOfBeds": 1,
      "bedType": "King",
      "isActive": true,
      "images": [
        {
          "mediumId": 1,
          "filePath": "https://cdn.example.com/deluxe-1.jpg",
          "description": "Deluxe Room Main View",
          "displayOrder": 0
        },
        {
          "mediumId": 2,
          "filePath": "https://cdn.example.com/deluxe-2.jpg",
          "description": "Deluxe Room Bathroom",
          "displayOrder": 1
        }
      ],
      "amenities": [
        {
          "amenityId": 1,
          "amenityName": "Tivi 55 inch",
          "description": "Smart TV",
          "amenityType": "Entertainment",
          "isActive": true
        },
        {
          "amenityId": 2,
          "amenityName": "Điều hòa",
          "description": "AC 2 chiều",
          "amenityType": "Climate",
          "isActive": true
        }
      ],
      "comments": [
        {
          "commentId": 1,
          "customerId": 100,
          "customerName": "Nguyễn Văn A",
          "rating": 5,
          "commentText": "Phòng rất đẹp, view tuyệt vời!",
          "createdAt": "2025-12-10T08:00:00Z"
        }
      ],
      "totalRoomCount": 5,
      "availableRoomCount": 3
    },
    {
      "roomTypeId": 2,
      "typeName": "Standard Room",
      "typeCode": "STD",
      "description": "Phòng tiêu chuẩn thoải mái",
      "basePriceNight": 800000,
      "maxOccupancy": 2,
      "roomSize": 25.0,
      "numberOfBeds": 1,
      "bedType": "Double",
      "isActive": true,
      "images": [],
      "amenities": [],
      "comments": [],
      "totalRoomCount": 8,
      "availableRoomCount": 5
    },
    {
      "roomTypeId": 3,
      "typeName": "Economy Room",
      "typeCode": "ECO",
      "description": "Phòng kinh tế tiết kiệm",
      "basePriceNight": 400000,
      "maxOccupancy": 1,
      "roomSize": 20.0,
      "numberOfBeds": 1,
      "bedType": "Single",
      "isActive": true,
      "images": [],
      "amenities": [],
      "comments": [],
      "totalRoomCount": 10,
      "availableRoomCount": 0
    }
  ]
}
```

---

## 🔑 Giải Thích Fields Response

### Thông tin chính
| Field | Loại | Mô tả |
|-------|------|-------|
| `roomTypeId` | int | ID loại phòng |
| `typeName` | string | Tên loại phòng (VD: "Deluxe Room") |
| `typeCode` | string | Mã loại phòng (VD: "DLX") |
| `description` | string | Mô tả chi tiết |

### Thông tin phòng
| Field | Loại | Mô tả |
|-------|------|-------|
| `basePriceNight` | decimal | **Giá/đêm** (tính cho 1 phòng) |
| `maxOccupancy` | int | Sức chứa tối đa (số khách) |
| `roomSize` | decimal | Diện tích phòng (m²) |
| `numberOfBeds` | int | Số giường |
| `bedType` | string | Loại giường (King, Queen, Twin, Single...) |

### **Thông tin khả dụng** (QUAN TRỌNG)
| Field | Loại | Mô tả |
|-------|------|-------|
| `totalRoomCount` | int | **Tổng số phòng** của loại này trong hệ thống |
| `availableRoomCount` | int | **SỐ PHÒNG KHẢ DỤNG** trong khoảng CheckIn-CheckOut |

### Media & Amenities
| Field | Loại | Mô tả |
|-------|------|-------|
| `images` | array | Danh sách ảnh của loại phòng |
| `amenities` | array | Danh sách tiện nghi (Tivi, Điều hòa, WiFi...) |
| `comments` | array | Danh sách bình luận từ khách hàng |

---

## 💰 Tính Toán Giá

### Công thức
```
Tổng giá = basePriceNight × số đêm lưu trú
```

### Ví dụ
**Scenario:** Check-in 20/12, Check-out 22/12 = **2 đêm**

| Loại phòng | Giá/đêm | Số đêm | Tổng giá |
|-----------|---------|--------|----------|
| Deluxe | 1.500.000 | 2 | **3.000.000 VND** |
| Standard | 800.000 | 2 | **1.600.000 VND** |
| Economy | 400.000 | 2 | **800.000 VND** |

**Lưu ý:** FE cần tính toán tổng giá dựa trên số đêm. Backend chỉ cung cấp `basePriceNight`.

---

## ❌ Response Error

### Error 400 - Bad Request (Ngày không hợp lệ)
```json
{
  "isSuccess": false,
  "responseCode": "INVALID_INPUT",
  "message": "CheckInDate phải nhỏ hơn CheckOutDate",
  "statusCode": 400,
  "errors": ["Ngày check-in phải nhỏ hơn ngày check-out"]
}
```

**Nguyên nhân:**
- CheckOutDate ≤ CheckInDate
- Format ngày không đúng (không phải yyyy-MM-dd)

---

### Error 404 - Not Found (Không tìm thấy)
```json
{
  "isSuccess": false,
  "responseCode": "NOT_FOUND",
  "message": "Không tìm thấy loại phòng nào khả dụng",
  "statusCode": 404
}
```

**Nguyên nhân:**
- Tất cả phòng đã được đặt trong khoảng thời gian
- Filter quá khắt (giá, diện tích, sức chứa...)

---

## 📊 Hiệu Ứng Frontend

### Hiển thị Phòng Trống
```javascript
if (room.availableRoomCount > 0) {
  // Hiển thị: "3 phòng khả dụng"
  // Cho phép đặt phòng
} else {
  // Hiển thị: "Hết phòng"
  // Vô hiệu hóa nút đặt
}
```

### Tính Tổng Giá
```javascript
const numberOfNights = (checkOutDate - checkInDate) / (1000 * 60 * 60 * 24);
const totalPrice = room.basePriceNight * numberOfNights;
// Hiển thị: "3.000.000 VND cho 2 đêm"
```

### Điều Kiện Hiển Thị
```javascript
const roomsToDisplay = response.data.filter(room => {
  // Chỉ hiển thị nếu có phòng trống
  return room.availableRoomCount > 0;
});
```

---

## 🧪 Test Cases

### Test 1: Phòng hết slot
**Request:**
```
GET /api/room/types/search?checkInDate=2025-12-25&checkOutDate=2025-12-26
```

**Kỳ vọng:** Trả về các loại phòng với `availableRoomCount = 0` → Hiển thị "Hết phòng"

---

### Test 2: Lọc theo giá
**Request:**
```
GET /api/room/types/search?checkInDate=2025-12-20&checkOutDate=2025-12-22&minPrice=1000000&maxPrice=1500000
```

**Kỳ vọng:** Chỉ hiển thị phòng có giá trong khoảng 1M-1.5M

---

### Test 3: Lọc theo sức chứa
**Request:**
```
GET /api/room/types/search?checkInDate=2025-12-20&checkOutDate=2025-12-22&numberOfGuests=4
```

**Kỳ vọng:** Chỉ hiển thị phòng có `maxOccupancy >= 4`

---

### Test 4: Ngày không hợp lệ
**Request:**
```
GET /api/room/types/search?checkInDate=2025-12-22&checkOutDate=2025-12-20
```

**Kỳ vọng:** Trả về lỗi 400, message "CheckInDate phải nhỏ hơn CheckOutDate"

---

## 🔗 Liên Quan API Khác

### 1. Lấy chi tiết 1 loại phòng
```
GET /api/room/types/search/{id}?checkInDate=2025-12-20&checkOutDate=2025-12-22
```
Chi tiết hơn về 1 loại phòng cụ thể

---

### 2. Tìm kiếm phòng cụ thể (admin)
```
GET /api/RoomManagement/search?roomName=101&statusId=1
```
Tìm phòng cụ thể, không phải loại phòng

---

## 💡 Best Practices

### ✅ DO
- ✅ Luôn gửi cả `checkInDate` và `checkOutDate`
- ✅ Format ngày đúng: `yyyy-MM-dd`
- ✅ Kiểm tra `availableRoomCount` trước khi hiển thị
- ✅ Tính số đêm = (CheckOut - CheckIn) / 86400000 (milliseconds)
- ✅ Cache kết quả tìm kiếm để tránh load lại liên tục

### ❌ DON'T
- ❌ Không gửi CheckInDate = CheckOutDate (số đêm = 0)
- ❌ Không để CheckOutDate < CheckInDate
- ❌ Không giả sử giá là tổng (giá là/đêm)
- ❌ Không sử dụng format ngày khác (MM-dd-yyyy, dd/MM/yyyy...)
- ❌ Không quên thêm `?` trước parameters

---

## 🔐 Authorization

**Endpoint này không cần authorization** - Public API

Tuy nhiên nếu integrate với booking system, cần:
```
Authorization: Bearer {token}
```

---

## 📝 Integration Checklist

- [ ] Nhập CheckInDate/CheckOutDate từ DatePicker
- [ ] Format ngày thành yyyy-MM-dd
- [ ] Validate CheckOutDate > CheckInDate
- [ ] Gọi API với parameters đúng
- [ ] Parse response, kiểm tra `isSuccess`
- [ ] Hiển thị danh sách loại phòng
- [ ] Hiển thị `availableRoomCount` cho mỗi loại
- [ ] Tính tổng giá dựa trên số đêm
- [ ] Vô hiệu hóa loại phòng nếu `availableRoomCount = 0`
- [ ] Handle error responses (400, 404)
- [ ] Thêm loading state khi gọi API
- [ ] Hiển thị images, amenities, comments
