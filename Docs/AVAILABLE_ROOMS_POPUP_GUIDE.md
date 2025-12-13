# Hướng dẫn lấy danh sách phòng trống cho popup chọn phòng (offline booking)

## Tóm tắt
- `POST /api/BookingManagement/available-rooms` (đã cập nhật) trả thêm `availableRooms` cho từng `roomType` gồm `roomId`, tên phòng, giá, amenities, hình ảnh. Dùng trực tiếp để render popup chọn phòng.
- Nếu cần search/ lọc sâu hơn, vẫn có thể gọi `GET /api/BookingManagement/rooms/search` kèm `checkInDate`, `checkOutDate` (và `roomTypeId`) để lấy danh sách phòng trống.

## Quy trình đề xuất cho UI
1. Lễ tân nhập ngày check-in/check-out + số lượng từng loại phòng.
2. Gọi `POST /api/BookingManagement/available-rooms` với payload:
   ```json
   {
     "roomTypes": [
       { "roomTypeId": 1, "quantity": 1 },
       { "roomTypeId": 2, "quantity": 1 }
     ],
     "checkInDate": "2025-12-14T15:19:00Z",
     "checkOutDate": "2026-01-03T15:19:00Z"
   }
   ```
3. Nếu response `isAllAvailable = true` (hoặc từng roomType `isAvailable = true`), hiển thị popup với `roomType.availableRooms` để tick chọn phòng.
4. Nếu cần filter sâu hơn (ví dụ chỉ phòng có amenity/giá), dùng thêm `GET /api/BookingManagement/rooms/search` (như phụ lục bên dưới) và vẫn gửi `roomIds` vào `POST /api/BookingManagement/offline`.
5. Gửi `POST /api/BookingManagement/offline` với `roomIds` đã chọn:
   ```json
   {
     "customerId": 1,
     "fullName": "ĐÀO NGỌC NAM",
     "email": "daonam@gmail.com",
     "phoneNumber": "0382720127",
     "identityCard": "011203000070",
     "address": "10000",
     "roomIds": [101, 205],
     "checkInDate": "2025-12-14T15:19:00Z",
     "checkOutDate": "2026-01-03T15:19:00Z",
     "specialRequests": "",
     "paymentMethod": "Cash",
     "paymentNote": ""
   }
   ```

## Ghi chú
- `availableRooms` đã có sẵn trong `POST /available-rooms` (không cần gọi thêm API nếu chỉ hiển thị phòng theo từng loại đã chọn).
- Nếu muốn filter nâng cao, vẫn có `GET /rooms/search` (trả `data.rooms`), truyền `checkInDate`, `checkOutDate`, `roomTypeId` và `pageSize` để lấy nhiều phòng hơn.
- Nhớ truyền timezone dạng UTC (`...Z`) để kiểm tra availability chính xác. 
