# 🤖 Chatbot API - Conversation Flow Guide

## 📋 Tổng quan

Chatbot AI sử dụng Gemini API với Semantic Kernel để xử lý hội thoại tự nhiên và tự động gọi các function để tìm kiếm phòng.

## 🔑 Vấn đề đã sửa

### ❌ Vấn đề trước đây:
1. **Mỗi request tạo session mới** → AI không nhớ cuộc hội thoại
2. **Client không truyền sessionId** → Không duy trì context
3. **System prompt chưa rõ ràng** → AI không biết khi nào gọi function
4. **Thiếu logging** → Khó debug khi có lỗi

### ✅ Đã sửa:
1. **Cải thiện System Prompt** - Hướng dẫn AI rõ ràng hơn về:
   - Khi nào cần gọi `search_available_rooms`
   - Cách parse ngày tháng (DD/MM/YYYY, YYYY-MM-DD, "ngày 1/12")
   - Format response tốt hơn

2. **Thêm Logging chi tiết**:
   - Log khi function được gọi (🔧)
   - Log parameters truyền vào
   - Log kết quả trả về (✅/❌)

3. **Tạo test file** - `test-chatbot-api.http` với nhiều scenarios

## 🔄 Cách sử dụng đúng (QUAN TRỌNG)

### 1️⃣ Request đầu tiên (tạo session mới):

```json
POST /api/ChatBot/chat
{
  "message": "Tôi muốn tìm phòng",
  "sessionId": null,  // NULL để tạo session mới
  "accountId": null,
  "guestIdentifier": "guest-unique-id"
}
```

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "sessionId": "abc-123-def-456",  // ⚠️ LƯU LẠI ID NÀY
    "message": "Chào bạn! Tôi có thể giúp bạn tìm phòng...",
    "isNewSession": true
  }
}
```

### 2️⃣ Request tiếp theo (dùng session cũ):

```json
POST /api/ChatBot/chat
{
  "message": "Tôi cần phòng cho 2 người từ 1/12/2027 đến 5/12/2027",
  "sessionId": "abc-123-def-456",  // ✅ TRUYỀN LẠI sessionId
  "accountId": null,
  "guestIdentifier": "guest-unique-id"  // GIỮ NGUYÊN
}
```

**AI sẽ tự động:**
1. Gọi function `search_available_rooms(checkInDate="2027-12-01", checkOutDate="2027-12-05", guestCount=2)`
2. Nhận kết quả từ API
3. Trả lời bằng tiếng Việt với danh sách phòng

### 3️⃣ Request tiếp theo trong cùng hội thoại:

```json
POST /api/ChatBot/chat
{
  "message": "Phòng nào có giá dưới 1 triệu?",
  "sessionId": "abc-123-def-456",  // ✅ TIẾP TỤC DÙNG sessionId CŨ
  "guestIdentifier": "guest-unique-id"
}
```

**AI nhớ context** từ câu hỏi trước và filter theo giá.

## 🎯 Function Calling - Cách hoạt động

### Các function có sẵn:

#### 1. `search_available_rooms`
**Khi AI gọi:**
- User hỏi về "phòng trống", "available rooms", "tìm phòng"
- User cung cấp ngày check-in/check-out

**Parameters:**
- `checkInDate`: YYYY-MM-DD (required)
- `checkOutDate`: YYYY-MM-DD (required)
- `guestCount`: number (optional)
- `minPrice`, `maxPrice`: decimal (optional)

**Example log:**
```
🔧 FUNCTION CALLED: search_available_rooms
  CheckIn: 2027-12-01, CheckOut: 2027-12-05
  Location: N/A, Guests: 2, PriceRange: null-null
✅ Function returned 3 rooms
```

#### 2. `get_room_details`
**Khi AI gọi:**
- User hỏi chi tiết về phòng cụ thể
- User nói "phòng số X", "room type Y"

**Parameters:**
- `roomTypeId`: int (required)
- `checkInDate`, `checkOutDate`: string (optional)

#### 3. `get_current_date`
**Khi AI gọi:**
- User nói "hôm nay", "ngày mai", "tuần sau"
- AI cần xác định ngày hiện tại

## 📊 Logging - Cách debug

### Logs bạn sẽ thấy:

#### ✅ Request thành công với function calling:
```
info: === ChatBot Request Started ===
info: Incoming SessionId: abc-123-def-456 (hoặc NULL)
info: Session is EXISTING (hoặc NEW)
info: Semantic Kernel built successfully with 1 plugins
info: Chat history loaded. Message count: 5
info: 🔧 FUNCTION CALLED: search_available_rooms
info:   CheckIn: 2027-12-01, CheckOut: 2027-12-05
info:   Location: N/A, Guests: 2, PriceRange: null-null
info: === SearchRoomTypesAsync CALLED ===
info: CheckInDate: 2027-12-01, CheckOutDate: 2027-12-05
info: NumberOfGuests: 2, MinPrice: null, MaxPrice: null
info: OnlyActive: True, PageIndex: 0, PageSize: 10
info: ✅ Function returned 3 rooms
info: AI Response length: 250 characters
info: AI Response preview: Dạ, chúng tôi có 3 loại phòng phù hợp...
info: === ChatBot Request Completed Successfully ===
```

#### ⚠️ Function được gọi nhưng không có kết quả:
```
info: 🔧 FUNCTION CALLED: search_available_rooms
info:   CheckIn: 2027-12-01, CheckOut: 2027-12-05
info: === SearchRoomTypesAsync CALLED ===
info: CheckInDate: 2027-12-01, CheckOutDate: 2027-12-05
info: NumberOfGuests: null, MinPrice: null, MaxPrice: null
info: ⚠️ Function found no rooms: No available rooms
```
**Kiểm tra:**
- Database có phòng không? `SELECT * FROM RoomType WHERE IsActive = 1`
- Có phòng trống trong khoảng thời gian đó không?
- CheckInDate/CheckOutDate có đúng format không?

#### ❌ Lỗi 429 (Rate Limit):
```
fail: Response status code does not indicate success: 429 (Too Many Requests)
```
→ **Giải pháp:** Hệ thống tự động chuyển sang API key khác trong `appsettings.json`

#### ❌ Không gọi function:
Nếu AI không gọi function dù user hỏi về phòng:
1. Check system prompt có được load không
2. Check model có hỗ trợ function calling không (gemini-2.0-flash-exp, gemini-2.5-flash)
3. Check `ToolCallBehavior = AutoInvokeKernelFunctions`
4. Check logs có dòng `Semantic Kernel built successfully with X plugins` - X phải >= 1

### 🔍 Debug Checklist khi AI không trả về phòng:

1. **Kiểm tra logs có dòng `🔧 FUNCTION CALLED`?**
   - ✅ Có: Function được gọi, check bước 2
   - ❌ Không: AI không hiểu cần gọi function, check system prompt

2. **Kiểm tra logs `=== SearchRoomTypesAsync CALLED ===`**
   - Check parameters: CheckInDate, CheckOutDate, NumberOfGuests
   - Verify dates có đúng format YYYY-MM-DD không

3. **Kiểm tra database:**
   ```sql
   -- Check RoomType có active không
   SELECT * FROM RoomType WHERE IsActive = 1;
   
   -- Check phòng trống
   SELECT r.RoomId, r.RoomName, rt.TypeName
   FROM Room r
   JOIN RoomType rt ON r.RoomTypeId = rt.RoomTypeId
   WHERE r.RoomId NOT IN (
       SELECT br.RoomId FROM BookingRoom br
       JOIN Booking b ON br.BookingId = b.BookingId
       WHERE b.CheckInDate < '2027-12-05'
         AND b.CheckOutDate > '2027-12-01'
   );
   ```

4. **Check AvailableRoomCount:**
   - Logs sẽ cho biết có bao nhiêu phòng available
   - Nếu = 0: Tất cả phòng đã được book trong khoảng thời gian đó

## 🧪 Testing

Sử dụng file `test-chatbot-api.http`:

### Test 1: Conversation Flow
```http
### 1. Start conversation
POST {{baseUrl}}/api/ChatBot/chat
{ "message": "Hello", "sessionId": null }

### 2. Continue conversation (PASTE sessionId from response above)
POST {{baseUrl}}/api/ChatBot/chat
{ "message": "I need a room", "sessionId": "PASTE_HERE" }
```

### Test 2: Automated Flow
```http
# @name step1
POST {{baseUrl}}/api/ChatBot/chat
{ "message": "Hello", "sessionId": null }

# @name step2
POST {{baseUrl}}/api/ChatBot/chat
{ 
  "message": "I need a room for 2 guests",
  "sessionId": "{{step1.response.body.$.data.sessionId}}"  // ✅ Auto extract
}
```

## 🔧 Frontend Implementation

### React/Next.js Example:

```typescript
// chatService.ts
export class ChatService {
  private sessionId: string | null = null;
  private guestId: string = `guest-${Date.now()}`;

  async sendMessage(message: string): Promise<ChatResponse> {
    const response = await fetch('/api/ChatBot/chat', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        message,
        sessionId: this.sessionId,  // ✅ Truyền session
        guestIdentifier: this.guestId
      })
    });

    const data = await response.json();
    
    // ⚠️ LƯU sessionId từ response
    if (data.data?.sessionId) {
      this.sessionId = data.data.sessionId;
    }

    return data;
  }

  clearSession() {
    this.sessionId = null;
    this.guestId = `guest-${Date.now()}`;
  }
}
```

### Vue/Nuxt Example:

```typescript
// composables/useChatbot.ts
export const useChatbot = () => {
  const sessionId = ref<string | null>(null);
  const guestId = ref(`guest-${Date.now()}`);

  const sendMessage = async (message: string) => {
    const { data } = await $fetch('/api/ChatBot/chat', {
      method: 'POST',
      body: {
        message,
        sessionId: sessionId.value,  // ✅ Reactive session
        guestIdentifier: guestId.value
      }
    });

    // ⚠️ Cập nhật sessionId
    if (data?.sessionId) {
      sessionId.value = data.sessionId;
    }

    return data;
  };

  return { sendMessage, sessionId };
};
```

## ⚙️ Configuration

### appsettings.json:

```json
{
  "GeminiSettings": {
    "ApiKeys": [
      "key1",  // Primary
      "key2",  // Fallback khi key1 bị rate limit
      "key3"   // Fallback thứ 2
    ],
    "ModelId": "gemini-2.5-flash",
    "MaxTokens": 1000000,
    "Temperature": 0.7,
    "MaxConversationMessages": 20,      // Giữ 20 tin nhắn gần nhất
    "SummarizationThreshold": 10        // Summarize khi > 10 messages
  }
}
```

## 🐛 Troubleshooting

### Vấn đề: AI không nhớ cuộc hội thoại
**Nguyên nhân:** Client không truyền `sessionId`
**Giải pháp:** 
- Lưu sessionId từ response đầu tiên
- Truyền lại trong tất cả requests tiếp theo
- Kiểm tra logs: `info: Session is EXISTING` (không phải NEW)

### Vấn đề: AI không gọi function
**Nguyên nhân:** 
- Model không hỗ trợ function calling
- System prompt không rõ ràng

**Giải pháp:**
- Dùng model `gemini-2.5-flash` hoặc `gemini-2.0-flash-exp`
- Kiểm tra logs có dòng `🔧 FUNCTION CALLED` không

### Vấn đề: Lỗi 429 (Too Many Requests)
**Nguyên nhân:** API key bị rate limit
**Giải pháp:** Hệ thống tự động chuyển sang key khác, nhưng nên:
- Thêm nhiều API keys vào config
- Implement retry logic ở client

## 📝 Best Practices

1. **Luôn lưu sessionId** - Đây là key để duy trì conversation
2. **Sử dụng unique guestIdentifier** - Tránh conflict giữa users
3. **Clear session khi user logout** - Gọi `/api/ChatBot/clear/{sessionId}`
4. **Handle errors gracefully** - Show friendly message cho user
5. **Monitor logs** - Xem function có được gọi không

## 🎓 Examples

### Vietnamese Conversation:
```
User: "Tôi muốn đặt phòng"
AI: "Chào bạn! Bạn muốn đặt phòng cho bao nhiêu người và khi nào?"

User: "2 người, từ 1/12 đến 5/12 năm 2027"
AI: [Calls search_available_rooms]
    "Dạ, chúng tôi có 3 loại phòng phù hợp:
     • Phòng Standard - 800,000 VNĐ/đêm
     • Phòng Deluxe - 1,200,000 VNĐ/đêm  
     • Phòng Suite - 2,000,000 VNĐ/đêm"

User: "Phòng Standard có gì?"
AI: [Calls get_room_details(roomTypeId=1)]
    "Phòng Standard bao gồm: 1 giường đôi, WiFi, điều hòa..."
```

## 📞 Support

Nếu còn vấn đề, kiểm tra:
1. Logs trong console
2. Database `ChatSession` và `ChatMessage` tables
3. Gemini API quotas

---
**Updated:** 2025-11-29
**Author:** System Documentation
