# 📋 HƯỚNG DẪN TÍCH HỢP API BOOKING TẠI QUẦY (OFFLINE BOOKING)

> **Dành cho:** Frontend Developer  
> **Ngày cập nhật:** 07/12/2024  
> **API Base URL:** `http://localhost:8080/api/BookingManagement`

---

## 📚 MỤC LỤC

1. [Tổng quan luồng Booking tại quầy](#1-tổng-quan-luồng-booking-tại-quầy)
2. [API 1: Tìm kiếm nhanh khách hàng](#2-api-1-tìm-kiếm-nhanh-khách-hàng)
3. [API 2: Tìm kiếm phòng available](#3-api-2-tìm-kiếm-phòng-available)
4. [API 3: Tạo booking tại quầy](#4-api-3-tạo-booking-tại-quầy)
5. [API 4: Cập nhật thông tin booking](#5-api-4-cập-nhật-thông-tin-booking)
6. [UI/UX Flow chi tiết](#6-uiux-flow-chi-tiết)
7. [Error Handling](#7-error-handling)
8. [Code Examples (React/Vue)](#8-code-examples-reactvue)

---

## 1. TỔNG QUAN LUỒNG BOOKING TẠI QUẦY

### 🎯 Mục đích
Lễ tân tạo booking cho khách đến quầy, khách cung cấp thông tin → Lễ tân chọn phòng → Click "Xác nhận" → **Booking thành công ngay lập tức** (không cần chờ thanh toán).

### 📊 Luồng hoạt động

```
┌─────────────────────────────────────────────────────────────────┐
│                    KHÁCH ĐẾN QUẦY                               │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│  BƯỚC 1: LỄ TÂN HỎI SỐ ĐIỆN THOẠI / EMAIL                       │
│  → Gọi API Quick Search Customer                                │
│     • Nếu tìm thấy: Fill sẵn thông tin vào form                 │
│     • Nếu không: Nhập thông tin mới                             │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│  BƯỚC 2: CHỌN NGÀY CHECK-IN / CHECK-OUT                         │
│  → Gọi API Search Available Rooms                               │
│     • Hiển thị danh sách phòng trống                            │
│     • Lễ tân chọn phòng cụ thể (có thể chọn nhiều phòng)        │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│  BƯỚC 3: NHẬP THÔNG TIN BỔ SUNG (Optional)                      │
│     • Special Requests                                          │
│     • Payment Method: Cash / Card / Transfer                    │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│  BƯỚC 4: CLICK "XÁC NHẬN BOOKING" (Button)                      │
│  → Gọi API Create Offline Booking                               │
│     ✅ Booking thành công NGAY LẬP TỨC                          │
│     ✅ Status = "CheckedIn" (đã nhận phòng)                     │
│     ✅ Email xác nhận được gửi tự động                          │
│     ✅ Hiển thị QR Code (nếu khách muốn chuyển khoản sau)       │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│  [OPTIONAL] BƯỚC 5: CẬP NHẬT THÔNG TIN                          │
│  Nếu khách muốn thay đổi thông tin:                             │
│  → Gọi API Update Offline Booking                               │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. API 1: TÌM KIẾM NHANH KHÁCH HÀNG

### 🔍 Mục đích
Tìm kiếm khách hàng theo số điện thoại / email / tên để **tự động fill thông tin** vào form, giúp tăng tốc độ booking.

### 📡 Endpoint
```
GET /api/BookingManagement/customers/quick-search
```

### 🔑 Authorization
```
Bearer Token (Role: Receptionist, Manager, Admin)
```

### 📥 Request Parameters

| Parameter | Type   | Required | Description                           |
|-----------|--------|----------|---------------------------------------|
| searchKey | string | ✅ Yes   | Số điện thoại / Email / Tên khách hàng |

### 📤 Response Success

#### Tìm thấy khách hàng:
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Tìm thấy 2 khách hàng",
  "data": [
    {
      "customerId": 123,
      "fullName": "Nguyễn Văn A",
      "phoneNumber": "0901234567",
      "email": "nguyenvana@gmail.com",
      "identityCard": "001234567890",
      "address": "123 Đường ABC, TP.HCM",
      "totalBookings": 5,              // Số lần đã đặt phòng
      "lastBookingDate": "2024-11-20T10:30:00Z",
      "matchedBy": "Phone"             // "Phone" | "Email" | "Name"
    }
  ]
}
```

#### Không tìm thấy:
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Không tìm thấy khách hàng. Vui lòng nhập thông tin mới để tạo booking.",
  "data": []
}
```

### 💡 Cách sử dụng trong UI

**Khi tìm thấy khách hàng:**
```javascript
// Auto-fill form
if (response.data.length > 0) {
  const customer = response.data[0]; // Hoặc cho user chọn nếu có nhiều kết quả
  
  setFormData({
    customerId: customer.customerId,     // ⚠️ QUAN TRỌNG: Lưu customerId để update thay vì tạo mới
    fullName: customer.fullName,
    phoneNumber: customer.phoneNumber,
    email: customer.email,
    identityCard: customer.identityCard,
    address: customer.address
  });
  
  // Hiển thị thông tin khách quen
  showCustomerInfo(`Khách quen - Đã đặt ${customer.totalBookings} lần`);
}
```

**Khi không tìm thấy:**
```javascript
// Để trống form để lễ tân nhập thông tin mới
setFormData({
  customerId: null,           // ⚠️ Để null - Backend sẽ tạo account + customer mới
  fullName: "",
  phoneNumber: searchKey,     // Pre-fill số điện thoại đã search
  email: "",
  identityCard: "",
  address: ""
});
```

### 📝 Ví dụ cURL
```bash
# Tìm theo số điện thoại
curl -X GET "http://localhost:8080/api/BookingManagement/customers/quick-search?searchKey=0901234567" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Tìm theo email
curl -X GET "http://localhost:8080/api/BookingManagement/customers/quick-search?searchKey=customer@gmail.com" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## 3. API 2: TÌM KIẾM PHÒNG AVAILABLE

### 🔍 Mục đích
Tìm các phòng trống theo ngày check-in/check-out và các tiêu chí filter khác.

### 📡 Endpoint
```
GET /api/BookingManagement/rooms/search
```

### 🔑 Authorization
```
Bearer Token (Role: Receptionist, Manager, Admin)
```

### 📥 Request Parameters

| Parameter     | Type     | Required | Description                           |
|---------------|----------|----------|---------------------------------------|
| checkInDate   | DateTime | ✅ Yes   | Ngày check-in (ISO 8601)              |
| checkOutDate  | DateTime | ✅ Yes   | Ngày check-out (ISO 8601)             |
| roomTypeId    | int      | ❌ No    | Filter theo loại phòng                |
| minPrice      | decimal  | ❌ No    | Giá tối thiểu                         |
| maxPrice      | decimal  | ❌ No    | Giá tối đa                            |
| maxOccupancy  | int      | ❌ No    | Số người tối đa                       |
| searchTerm    | string   | ❌ No    | Tìm theo tên phòng                    |
| pageNumber    | int      | ❌ No    | Trang hiện tại (default: 1)           |
| pageSize      | int      | ❌ No    | Số phòng mỗi trang (default: 20)      |

### 📤 Response Success

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Tìm thấy 5 phòng phù hợp",
  "data": {
    "rooms": [
      {
        "roomId": 101,
        "roomName": "Phòng 101",
        "roomTypeId": 1,
        "roomTypeName": "Deluxe",
        "roomTypeCode": "DLX",
        "pricePerNight": 1500000,
        "maxOccupancy": 2,
        "roomSize": 25.5,
        "numberOfBeds": 1,
        "bedType": "King",
        "description": "Phòng sang trọng với view biển",
        "status": "Available",
        "amenities": ["WiFi", "TV", "Minibar", "Balcony"],
        "images": [
          "https://example.com/room101-1.jpg",
          "https://example.com/room101-2.jpg"
        ]
      }
    ],
    "totalCount": 5,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 1
  }
}
```

### 💡 Cách sử dụng trong UI

```javascript
// Hiển thị danh sách phòng dạng card/grid
rooms.forEach(room => {
  displayRoomCard({
    id: room.roomId,
    name: room.roomName,
    type: room.roomTypeName,
    price: room.pricePerNight.toLocaleString('vi-VN') + ' VNĐ/đêm',
    capacity: `${room.maxOccupancy} người`,
    image: room.images[0],
    amenities: room.amenities,
    onSelect: () => addToSelectedRooms(room.roomId)
  });
});
```

### 📝 Ví dụ cURL
```bash
curl -X GET "http://localhost:8080/api/BookingManagement/rooms/search?checkInDate=2024-12-10T14:00:00Z&checkOutDate=2024-12-12T12:00:00Z&maxOccupancy=2" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## 4. API 3: TẠO BOOKING TẠI QUẦY

### 🎯 Mục đích
**Tạo booking và XÁC NHẬN THÀNH CÔNG NGAY LẬP TỨC** khi lễ tân click nút "Xác nhận".

### 📡 Endpoint
```
POST /api/BookingManagement/offline
```

### 🔑 Authorization
```
Bearer Token (Role: Receptionist, Manager, Admin)
```

### 📥 Request Body

```json
{
  "customerId": 123,                    // ⚠️ NULL nếu khách mới, có giá trị nếu khách quen
  "fullName": "Nguyễn Văn A",
  "email": "nguyenvana@gmail.com",      // ⚠️ REQUIRED - Dùng để tạo account nếu chưa có
  "phoneNumber": "0901234567",
  "identityCard": "001234567890",
  "address": "123 Đường ABC, TP.HCM",
  "roomIds": [101, 102, 201],           // ⚠️ REQUIRED - Danh sách phòng đã chọn
  "checkInDate": "2024-12-10T14:00:00Z",
  "checkOutDate": "2024-12-12T12:00:00Z",
  "specialRequests": "Phòng tầng cao, view đẹp",
  "paymentMethod": "Cash",              // "Cash" | "Card" | "Transfer"
  "paymentNote": "Đã thanh toán tiền mặt"
}
```

### 📤 Response Success

```json
{
  "isSuccess": true,
  "statusCode": 201,
  "message": "Tạo booking tại quầy thành công!",
  "data": {
    "booking": {
      "bookingId": 456,
      "customerId": 123,
      "customerName": "Nguyễn Văn A",
      "roomIds": [101, 102, 201],
      "roomNames": ["Phòng 101", "Phòng 102", "Phòng 201"],
      "roomTypeDetails": [
        {
          "roomTypeId": 1,
          "roomTypeName": "Deluxe",
          "roomTypeCode": "DLX",
          "quantity": 2,
          "pricePerNight": 1500000,
          "subTotal": 3000000
        },
        {
          "roomTypeId": 2,
          "roomTypeName": "Suite",
          "roomTypeCode": "SUI",
          "quantity": 1,
          "pricePerNight": 2500000,
          "subTotal": 2500000
        }
      ],
      "checkInDate": "2024-12-10T14:00:00Z",
      "checkOutDate": "2024-12-12T12:00:00Z",
      "totalAmount": 5500000,
      "depositAmount": 1650000,            // 30% của tổng tiền
      "paymentStatus": "CheckedIn",        // ✅ Đã xác nhận thành công
      "bookingType": "WalkIn",
      "specialRequests": "Phòng tầng cao, view đẹp",
      "createdAt": "2024-12-07T15:30:00Z"
    },
    "qrPayment": {                         // ⚠️ Có thể null nếu không có bank config
      "qrCodeUrl": "https://img.vietqr.io/image/VCB-1234567890-compact.png?amount=5500000&addInfo=Thanh%20toan%20booking%20456",
      "bankName": "Vietcombank",
      "bankCode": "VCB",
      "accountNumber": "1234567890",
      "accountName": "CONG TY KHACH SAN ABC",
      "amount": 5500000,
      "description": "Thanh toan booking 456",
      "transactionRef": "WALKIN-456-20241207153025",
      "qrDataText": "Chuyển khoản đến: CONG TY KHACH SAN ABC\nSố TK: 1234567890\nNgân hàng: Vietcombank\nSố tiền: 5,500,000 VNĐ\nNội dung: Thanh toan booking 456"
    }
  }
}
```

### 💡 Cách xử lý Response

```javascript
if (response.isSuccess) {
  const { booking, qrPayment } = response.data;
  
  // 1. Hiển thị thông báo thành công
  showSuccessMessage(`✅ Đặt phòng thành công! Mã booking: #${booking.bookingId}`);
  
  // 2. In hóa đơn (optional)
  printInvoice({
    bookingId: booking.bookingId,
    customerName: booking.customerName,
    rooms: booking.roomNames,
    checkIn: formatDate(booking.checkInDate),
    checkOut: formatDate(booking.checkOutDate),
    totalAmount: booking.totalAmount,
    paymentStatus: "Đã thanh toán"
  });
  
  // 3. Hiển thị QR Code nếu có (cho khách muốn chuyển khoản sau)
  if (qrPayment) {
    showQRCodeModal({
      qrUrl: qrPayment.qrCodeUrl,
      bankInfo: `${qrPayment.bankName} - ${qrPayment.accountNumber}`,
      amount: qrPayment.amount,
      note: qrPayment.description
    });
  }
  
  // 4. Reset form và quay về trang chủ
  resetForm();
  navigateToBookingList();
}
```

### ⚠️ LƯU Ý QUAN TRỌNG

#### 🔄 Logic tạo Customer tự động:

| Trường hợp | customerId | email | Hành động Backend |
|------------|-----------|-------|-------------------|
| Khách quen (đã Quick Search) | ✅ Có giá trị | Có | **Update** thông tin Customer hiện tại |
| Khách cũ (email trùng) | ❌ null | Có & trùng | **Update** Customer linked với Account đó |
| Khách mới (email mới) | ❌ null | Có & mới | **Tạo mới** Account + Customer + Gán User role |

**→ Frontend KHÔNG cần quan tâm logic này, chỉ cần truyền đúng `customerId` và `email`!**

### 📝 Ví dụ cURL

#### Khách quen (có customerId):
```bash
curl -X POST "http://localhost:8080/api/BookingManagement/offline" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": 123,
    "fullName": "Nguyễn Văn A",
    "email": "nguyenvana@gmail.com",
    "phoneNumber": "0901234567",
    "identityCard": "001234567890",
    "address": "123 Đường ABC, TP.HCM",
    "roomIds": [101, 102],
    "checkInDate": "2024-12-10T14:00:00Z",
    "checkOutDate": "2024-12-12T12:00:00Z",
    "specialRequests": "Phòng tầng cao",
    "paymentMethod": "Cash"
  }'
```

#### Khách mới (không có customerId):
```bash
curl -X POST "http://localhost:8080/api/BookingManagement/offline" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": null,
    "fullName": "Trần Thị B",
    "email": "tranthib@gmail.com",
    "phoneNumber": "0907654321",
    "identityCard": "009876543210",
    "address": "456 Đường XYZ, Hà Nội",
    "roomIds": [103],
    "checkInDate": "2024-12-15T14:00:00Z",
    "checkOutDate": "2024-12-17T12:00:00Z",
    "paymentMethod": "Card"
  }'
```

---

## 5. API 4: CẬP NHẬT THÔNG TIN BOOKING

### 🎯 Mục đích
Cập nhật thông tin khách hàng hoặc booking sau khi đã tạo (nếu khách yêu cầu thay đổi).

### 📡 Endpoint
```
PUT /api/BookingManagement/offline/{bookingId}
```

### 🔑 Authorization
```
Bearer Token (Role: Receptionist, Manager, Admin)
```

### 📥 Request Body

```json
{
  "fullName": "Nguyễn Văn A (Updated)",
  "phoneNumber": "0901234567",
  "identityCard": "001234567890",
  "address": "123 Đường ABC, TP.HCM (Updated)",
  "checkInDate": "2024-12-11T14:00:00Z",     // Optional - cập nhật ngày nếu cần
  "checkOutDate": "2024-12-13T12:00:00Z",
  "specialRequests": "Thêm giường phụ"
}
```

⚠️ **Lưu ý:** Chỉ truyền các field cần update. Field nào không muốn thay đổi thì không cần gửi.

### 📤 Response Success

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Cập nhật booking thành công"
}
```

### 💡 Cách sử dụng trong UI

```javascript
// Khi khách yêu cầu thay đổi thông tin
const updateBooking = async (bookingId, changes) => {
  try {
    const response = await fetch(`/api/BookingManagement/offline/${bookingId}`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(changes)
    });
    
    if (response.ok) {
      showSuccessMessage('✅ Cập nhật thông tin thành công!');
      refreshBookingDetail(bookingId);
    }
  } catch (error) {
    showErrorMessage('❌ Không thể cập nhật. Vui lòng thử lại.');
  }
};
```

### 📝 Ví dụ cURL

```bash
curl -X PUT "http://localhost:8080/api/BookingManagement/offline/456" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Nguyễn Văn A (Updated)",
    "specialRequests": "Thêm giường phụ"
  }'
```

---

## 6. UI/UX FLOW CHI TIẾT

### 📱 Màn hình 1: Form Tạo Booking

```
┌───────────────────────────────────────────────────────────────┐
│  📝 ĐẶT PHÒNG TẠI QUẦY                                        │
├───────────────────────────────────────────────────────────────┤
│                                                               │
│  🔍 THÔNG TIN KHÁCH HÀNG                                      │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ Số điện thoại / Email:  [_____________] [🔍 Tìm kiếm]  │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                               │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ Họ tên:           [Nguyễn Văn A                    ]    │ │
│  │ Email:            [nguyenvana@gmail.com             ]    │ │
│  │ Số điện thoại:    [0901234567                       ]    │ │
│  │ CMND/CCCD:        [001234567890                     ]    │ │
│  │ Địa chỉ:          [123 Đường ABC, TP.HCM            ]    │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                               │
│  📅 THÔNG TIN ĐẶT PHÒNG                                       │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ Check-in:   [📅 10/12/2024 14:00]                       │ │
│  │ Check-out:  [📅 12/12/2024 12:00]                       │ │
│  │                                         [🔍 Tìm phòng]   │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                               │
│  🛏️ DANH SÁCH PHÒNG ĐÃ CHỌN                                   │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ ☑️ Phòng 101 - Deluxe        1,500,000 VNĐ/đêm    [X]   │ │
│  │ ☑️ Phòng 102 - Deluxe        1,500,000 VNĐ/đêm    [X]   │ │
│  │ ☑️ Phòng 201 - Suite         2,500,000 VNĐ/đêm    [X]   │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                               │
│  💰 TỔNG TIỀN                                                 │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ Tổng cộng (2 đêm):           5,500,000 VNĐ              │ │
│  │ Tiền cọc (30%):              1,650,000 VNĐ              │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                               │
│  💳 PHƯƠNG THỨC THANH TOÁN                                    │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ ⚪ Tiền mặt  ⚪ Thẻ  ⚪ Chuyển khoản                      │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                               │
│  📝 Ghi chú đặc biệt: [_________________________]            │
│                                                               │
│  [❌ Hủy]                            [✅ XÁC NHẬN BOOKING]   │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

### 🎬 Hành động khi Click "XÁC NHẬN BOOKING"

```javascript
const handleConfirmBooking = async () => {
  // 1. Validate form
  if (!validateForm()) {
    showError('Vui lòng điền đầy đủ thông tin!');
    return;
  }
  
  // 2. Hiển thị loading
  setLoading(true);
  
  // 3. Gọi API Create Offline Booking
  try {
    const response = await createOfflineBooking({
      customerId: formData.customerId,  // null nếu khách mới
      fullName: formData.fullName,
      email: formData.email,
      phoneNumber: formData.phoneNumber,
      identityCard: formData.identityCard,
      address: formData.address,
      roomIds: selectedRooms.map(r => r.id),
      checkInDate: formData.checkInDate,
      checkOutDate: formData.checkOutDate,
      specialRequests: formData.specialRequests,
      paymentMethod: formData.paymentMethod
    });
    
    if (response.isSuccess) {
      // 4. Hiển thị modal thành công
      showSuccessModal({
        bookingId: response.data.booking.bookingId,
        customerName: response.data.booking.customerName,
        rooms: response.data.booking.roomNames,
        totalAmount: response.data.booking.totalAmount,
        qrCode: response.data.qrPayment?.qrCodeUrl
      });
      
      // 5. In hóa đơn (optional)
      if (confirm('In hóa đơn?')) {
        printInvoice(response.data.booking);
      }
      
      // 6. Reset form
      resetForm();
    }
  } catch (error) {
    showError('Đặt phòng thất bại: ' + error.message);
  } finally {
    setLoading(false);
  }
};
```

### 📱 Màn hình 2: Modal Thành Công

```
┌───────────────────────────────────────────────────────────────┐
│  ✅ ĐẶT PHÒNG THÀNH CÔNG                           [X]        │
├───────────────────────────────────────────────────────────────┤
│                                                               │
│     Mã booking: #456                                          │
│     Khách hàng: Nguyễn Văn A                                  │
│     Phòng: 101, 102, 201                                      │
│     Check-in: 10/12/2024 14:00                                │
│     Check-out: 12/12/2024 12:00                               │
│     Tổng tiền: 5,500,000 VNĐ                                  │
│     Trạng thái: ✅ Đã nhận phòng                              │
│                                                               │
│  📧 Email xác nhận đã được gửi đến:                           │
│     nguyenvana@gmail.com                                      │
│                                                               │
│  ┌──────────────────────────────────────────────────────┐    │
│  │  [QR CODE IMAGE]                                     │    │
│  │  Quét mã để chuyển khoản                             │    │
│  │  Vietcombank - 1234567890                            │    │
│  │  Số tiền: 5,500,000 VNĐ                              │    │
│  └──────────────────────────────────────────────────────┘    │
│                                                               │
│  [🖨️ In hóa đơn]  [📧 Gửi email]  [✅ Hoàn tất]            │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

---

## 7. ERROR HANDLING

### ❌ Các lỗi thường gặp

#### 1. Không tìm thấy phòng
```json
{
  "isSuccess": false,
  "statusCode": 404,
  "message": "Không tìm thấy phòng ID: 101"
}
```
**→ Hiển thị:** "Phòng không tồn tại. Vui lòng chọn lại."

#### 2. Phòng không còn trống
```json
{
  "isSuccess": false,
  "statusCode": 409,
  "message": "Phòng 101 không còn trống trong thời gian này"
}
```
**→ Hiển thị:** "Phòng đã được đặt. Vui lòng chọn phòng khác."

#### 3. Ngày không hợp lệ
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Ngày check-out phải sau ngày check-in"
}
```
**→ Hiển thị:** "Ngày check-out phải sau ngày check-in."

#### 4. Thiếu thông tin bắt buộc
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Vui lòng chọn ít nhất một phòng"
}
```
**→ Hiển thị:** "Vui lòng chọn phòng trước khi đặt."

### 💡 Code xử lý Error

```javascript
const handleError = (error) => {
  const errorMessages = {
    404: 'Không tìm thấy thông tin',
    400: 'Thông tin không hợp lệ',
    409: 'Phòng đã được đặt',
    401: 'Vui lòng đăng nhập lại',
    500: 'Lỗi hệ thống. Vui lòng thử lại sau'
  };
  
  const statusCode = error.statusCode || 500;
  const message = error.message || errorMessages[statusCode];
  
  showErrorNotification({
    title: 'Đặt phòng thất bại',
    message: message,
    type: 'error',
    duration: 5000
  });
};
```

---

## 8. CODE EXAMPLES (REACT/VUE)

### ⚛️ React Example

```jsx
import React, { useState, useEffect } from 'react';
import { api } from './api';

const OfflineBookingForm = () => {
  const [formData, setFormData] = useState({
    customerId: null,
    fullName: '',
    email: '',
    phoneNumber: '',
    identityCard: '',
    address: '',
    checkInDate: '',
    checkOutDate: '',
    roomIds: [],
    specialRequests: '',
    paymentMethod: 'Cash'
  });
  
  const [searchKey, setSearchKey] = useState('');
  const [availableRooms, setAvailableRooms] = useState([]);
  const [loading, setLoading] = useState(false);

  // 1. Quick Search Customer
  const handleQuickSearch = async () => {
    if (!searchKey) return;
    
    try {
      const response = await api.get('/BookingManagement/customers/quick-search', {
        params: { searchKey }
      });
      
      if (response.data.data.length > 0) {
        const customer = response.data.data[0];
        setFormData(prev => ({
          ...prev,
          customerId: customer.customerId,
          fullName: customer.fullName,
          email: customer.email,
          phoneNumber: customer.phoneNumber,
          identityCard: customer.identityCard,
          address: customer.address
        }));
        alert(`✅ Tìm thấy khách quen: ${customer.fullName} (Đã đặt ${customer.totalBookings} lần)`);
      } else {
        alert('Không tìm thấy khách hàng. Vui lòng nhập thông tin mới.');
      }
    } catch (error) {
      console.error('Search error:', error);
    }
  };

  // 2. Search Available Rooms
  const searchRooms = async () => {
    if (!formData.checkInDate || !formData.checkOutDate) {
      alert('Vui lòng chọn ngày check-in và check-out');
      return;
    }
    
    try {
      const response = await api.get('/BookingManagement/rooms/search', {
        params: {
          checkInDate: formData.checkInDate,
          checkOutDate: formData.checkOutDate,
          pageSize: 50
        }
      });
      
      setAvailableRooms(response.data.data.rooms);
    } catch (error) {
      console.error('Search rooms error:', error);
    }
  };

  // 3. Create Booking
  const handleConfirmBooking = async () => {
    if (!formData.fullName || !formData.email || formData.roomIds.length === 0) {
      alert('Vui lòng điền đầy đủ thông tin và chọn phòng');
      return;
    }
    
    setLoading(true);
    
    try {
      const response = await api.post('/BookingManagement/offline', formData);
      
      if (response.data.isSuccess) {
        const { booking, qrPayment } = response.data.data;
        
        alert(`✅ Đặt phòng thành công!\nMã booking: #${booking.bookingId}\nKhách hàng: ${booking.customerName}`);
        
        // Hiển thị QR Code nếu có
        if (qrPayment) {
          window.open(qrPayment.qrCodeUrl, '_blank');
        }
        
        // Reset form
        setFormData({
          customerId: null,
          fullName: '',
          email: '',
          phoneNumber: '',
          identityCard: '',
          address: '',
          checkInDate: '',
          checkOutDate: '',
          roomIds: [],
          specialRequests: '',
          paymentMethod: 'Cash'
        });
      }
    } catch (error) {
      alert('❌ Đặt phòng thất bại: ' + (error.response?.data?.message || error.message));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="booking-form">
      <h2>📝 Đặt Phòng Tại Quầy</h2>
      
      {/* Quick Search */}
      <div className="search-section">
        <h3>🔍 Tìm kiếm khách hàng</h3>
        <input
          type="text"
          placeholder="Số điện thoại / Email / Tên"
          value={searchKey}
          onChange={(e) => setSearchKey(e.target.value)}
        />
        <button onClick={handleQuickSearch}>Tìm kiếm</button>
      </div>
      
      {/* Customer Info */}
      <div className="customer-info">
        <h3>👤 Thông tin khách hàng</h3>
        <input
          type="text"
          placeholder="Họ tên *"
          value={formData.fullName}
          onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
          required
        />
        <input
          type="email"
          placeholder="Email *"
          value={formData.email}
          onChange={(e) => setFormData({ ...formData, email: e.target.value })}
          required
        />
        <input
          type="tel"
          placeholder="Số điện thoại *"
          value={formData.phoneNumber}
          onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
          required
        />
        <input
          type="text"
          placeholder="CMND/CCCD"
          value={formData.identityCard}
          onChange={(e) => setFormData({ ...formData, identityCard: e.target.value })}
        />
        <input
          type="text"
          placeholder="Địa chỉ"
          value={formData.address}
          onChange={(e) => setFormData({ ...formData, address: e.target.value })}
        />
      </div>
      
      {/* Booking Dates */}
      <div className="booking-dates">
        <h3>📅 Thông tin đặt phòng</h3>
        <input
          type="datetime-local"
          value={formData.checkInDate}
          onChange={(e) => setFormData({ ...formData, checkInDate: e.target.value })}
        />
        <input
          type="datetime-local"
          value={formData.checkOutDate}
          onChange={(e) => setFormData({ ...formData, checkOutDate: e.target.value })}
        />
        <button onClick={searchRooms}>🔍 Tìm phòng</button>
      </div>
      
      {/* Available Rooms */}
      <div className="available-rooms">
        <h3>🛏️ Chọn phòng</h3>
        {availableRooms.map(room => (
          <div key={room.roomId} className="room-card">
            <input
              type="checkbox"
              checked={formData.roomIds.includes(room.roomId)}
              onChange={(e) => {
                if (e.target.checked) {
                  setFormData({ ...formData, roomIds: [...formData.roomIds, room.roomId] });
                } else {
                  setFormData({ ...formData, roomIds: formData.roomIds.filter(id => id !== room.roomId) });
                }
              }}
            />
            <label>{room.roomName} - {room.roomTypeName} - {room.pricePerNight.toLocaleString()} VNĐ/đêm</label>
          </div>
        ))}
      </div>
      
      {/* Special Requests */}
      <div className="special-requests">
        <h3>📝 Ghi chú đặc biệt</h3>
        <textarea
          placeholder="Yêu cầu đặc biệt..."
          value={formData.specialRequests}
          onChange={(e) => setFormData({ ...formData, specialRequests: e.target.value })}
        />
      </div>
      
      {/* Payment Method */}
      <div className="payment-method">
        <h3>💳 Phương thức thanh toán</h3>
        <label>
          <input
            type="radio"
            value="Cash"
            checked={formData.paymentMethod === 'Cash'}
            onChange={(e) => setFormData({ ...formData, paymentMethod: e.target.value })}
          />
          Tiền mặt
        </label>
        <label>
          <input
            type="radio"
            value="Card"
            checked={formData.paymentMethod === 'Card'}
            onChange={(e) => setFormData({ ...formData, paymentMethod: e.target.value })}
          />
          Thẻ
        </label>
        <label>
          <input
            type="radio"
            value="Transfer"
            checked={formData.paymentMethod === 'Transfer'}
            onChange={(e) => setFormData({ ...formData, paymentMethod: e.target.value })}
          />
          Chuyển khoản
        </label>
      </div>
      
      {/* Confirm Button */}
      <button
        className="confirm-button"
        onClick={handleConfirmBooking}
        disabled={loading}
      >
        {loading ? '⏳ Đang xử lý...' : '✅ XÁC NHẬN BOOKING'}
      </button>
    </div>
  );
};

export default OfflineBookingForm;
```

### 🖖 Vue 3 Example

```vue
<template>
  <div class="booking-form">
    <h2>📝 Đặt Phòng Tại Quầy</h2>
    
    <!-- Quick Search -->
    <div class="search-section">
      <h3>🔍 Tìm kiếm khách hàng</h3>
      <input
        v-model="searchKey"
        type="text"
        placeholder="Số điện thoại / Email / Tên"
      />
      <button @click="handleQuickSearch">Tìm kiếm</button>
    </div>
    
    <!-- Customer Info -->
    <div class="customer-info">
      <h3>👤 Thông tin khách hàng</h3>
      <input v-model="formData.fullName" type="text" placeholder="Họ tên *" required />
      <input v-model="formData.email" type="email" placeholder="Email *" required />
      <input v-model="formData.phoneNumber" type="tel" placeholder="Số điện thoại *" required />
      <input v-model="formData.identityCard" type="text" placeholder="CMND/CCCD" />
      <input v-model="formData.address" type="text" placeholder="Địa chỉ" />
    </div>
    
    <!-- Booking Dates -->
    <div class="booking-dates">
      <h3>📅 Thông tin đặt phòng</h3>
      <input v-model="formData.checkInDate" type="datetime-local" />
      <input v-model="formData.checkOutDate" type="datetime-local" />
      <button @click="searchRooms">🔍 Tìm phòng</button>
    </div>
    
    <!-- Available Rooms -->
    <div class="available-rooms">
      <h3>🛏️ Chọn phòng</h3>
      <div v-for="room in availableRooms" :key="room.roomId" class="room-card">
        <input
          type="checkbox"
          :value="room.roomId"
          v-model="formData.roomIds"
        />
        <label>{{ room.roomName }} - {{ room.roomTypeName }} - {{ room.pricePerNight.toLocaleString() }} VNĐ/đêm</label>
      </div>
    </div>
    
    <!-- Special Requests -->
    <div class="special-requests">
      <h3>📝 Ghi chú đặc biệt</h3>
      <textarea v-model="formData.specialRequests" placeholder="Yêu cầu đặc biệt..."></textarea>
    </div>
    
    <!-- Payment Method -->
    <div class="payment-method">
      <h3>💳 Phương thức thanh toán</h3>
      <label><input type="radio" value="Cash" v-model="formData.paymentMethod" /> Tiền mặt</label>
      <label><input type="radio" value="Card" v-model="formData.paymentMethod" /> Thẻ</label>
      <label><input type="radio" value="Transfer" v-model="formData.paymentMethod" /> Chuyển khoản</label>
    </div>
    
    <!-- Confirm Button -->
    <button
      class="confirm-button"
      @click="handleConfirmBooking"
      :disabled="loading"
    >
      {{ loading ? '⏳ Đang xử lý...' : '✅ XÁC NHẬN BOOKING' }}
    </button>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue';
import { api } from './api';

const searchKey = ref('');
const availableRooms = ref([]);
const loading = ref(false);

const formData = reactive({
  customerId: null,
  fullName: '',
  email: '',
  phoneNumber: '',
  identityCard: '',
  address: '',
  checkInDate: '',
  checkOutDate: '',
  roomIds: [],
  specialRequests: '',
  paymentMethod: 'Cash'
});

// 1. Quick Search Customer
const handleQuickSearch = async () => {
  if (!searchKey.value) return;
  
  try {
    const response = await api.get('/BookingManagement/customers/quick-search', {
      params: { searchKey: searchKey.value }
    });
    
    if (response.data.data.length > 0) {
      const customer = response.data.data[0];
      Object.assign(formData, {
        customerId: customer.customerId,
        fullName: customer.fullName,
        email: customer.email,
        phoneNumber: customer.phoneNumber,
        identityCard: customer.identityCard,
        address: customer.address
      });
      alert(`✅ Tìm thấy khách quen: ${customer.fullName} (Đã đặt ${customer.totalBookings} lần)`);
    } else {
      alert('Không tìm thấy khách hàng. Vui lòng nhập thông tin mới.');
    }
  } catch (error) {
    console.error('Search error:', error);
  }
};

// 2. Search Available Rooms
const searchRooms = async () => {
  if (!formData.checkInDate || !formData.checkOutDate) {
    alert('Vui lòng chọn ngày check-in và check-out');
    return;
  }
  
  try {
    const response = await api.get('/BookingManagement/rooms/search', {
      params: {
        checkInDate: formData.checkInDate,
        checkOutDate: formData.checkOutDate,
        pageSize: 50
      }
    });
    
    availableRooms.value = response.data.data.rooms;
  } catch (error) {
    console.error('Search rooms error:', error);
  }
};

// 3. Create Booking
const handleConfirmBooking = async () => {
  if (!formData.fullName || !formData.email || formData.roomIds.length === 0) {
    alert('Vui lòng điền đầy đủ thông tin và chọn phòng');
    return;
  }
  
  loading.value = true;
  
  try {
    const response = await api.post('/BookingManagement/offline', formData);
    
    if (response.data.isSuccess) {
      const { booking, qrPayment } = response.data.data;
      
      alert(`✅ Đặt phòng thành công!\nMã booking: #${booking.bookingId}\nKhách hàng: ${booking.customerName}`);
      
      // Hiển thị QR Code nếu có
      if (qrPayment) {
        window.open(qrPayment.qrCodeUrl, '_blank');
      }
      
      // Reset form
      Object.assign(formData, {
        customerId: null,
        fullName: '',
        email: '',
        phoneNumber: '',
        identityCard: '',
        address: '',
        checkInDate: '',
        checkOutDate: '',
        roomIds: [],
        specialRequests: '',
        paymentMethod: 'Cash'
      });
    }
  } catch (error) {
    alert('❌ Đặt phòng thất bại: ' + (error.response?.data?.message || error.message));
  } finally {
    loading.value = false;
  }
};
</script>
```

---

## 📝 CHECKLIST TÍCH HỢP

### ✅ Frontend Developer Checklist:

- [ ] **API 1:** Tích hợp Quick Search Customer
  - [ ] Input tìm kiếm với debounce (300ms)
  - [ ] Hiển thị dropdown kết quả tìm kiếm
  - [ ] Auto-fill form khi chọn khách hàng
  - [ ] Lưu `customerId` vào state

- [ ] **API 2:** Tích hợp Search Available Rooms
  - [ ] DatePicker cho check-in/check-out
  - [ ] Hiển thị danh sách phòng dạng card/grid
  - [ ] Checkbox chọn nhiều phòng
  - [ ] Tính tổng tiền realtime

- [ ] **API 3:** Tích hợp Create Offline Booking
  - [ ] Validate form đầy đủ
  - [ ] Hiển thị loading khi submit
  - [ ] Modal thành công với thông tin booking
  - [ ] Hiển thị QR Code (nếu có)
  - [ ] In hóa đơn (optional)

- [ ] **API 4:** Tích hợp Update Booking
  - [ ] Button "Chỉnh sửa" trên booking detail
  - [ ] Modal edit form
  - [ ] Xác nhận trước khi update

- [ ] **Error Handling:**
  - [ ] Toast/Notification cho lỗi
  - [ ] Retry logic cho network error
  - [ ] Form validation messages

- [ ] **UI/UX:**
  - [ ] Responsive design
  - [ ] Loading states
  - [ ] Success/Error states
  - [ ] Confirmation dialogs

---

## 🆘 HỖ TRỢ & LIÊN HỆ

**Backend Developer:**
- Email: backend@hotel.com
- Slack: #backend-support

**API Documentation:**
- Swagger UI: http://localhost:8080/swagger

**Postman Collection:**
- [Download Postman Collection](./postman/offline-booking.json)

---

**📅 Cập nhật cuối:** 07/12/2024  
**📝 Version:** 1.0  
**✍️ Người viết:** Backend Team

