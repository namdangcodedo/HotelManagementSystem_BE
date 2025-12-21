# 🤖 CHATBOT MODULE DOCUMENTATION

## 1. Tổng quan

Module ChatBot sử dụng **Google Gemini AI** kết hợp với **Microsoft Semantic Kernel** để tạo trợ lý ảo thông minh giúp khách hàng:
- Tìm kiếm phòng trống
- Xem thông tin chi tiết loại phòng
- Hỏi đáp về dịch vụ khách sạn
- Hướng dẫn đặt phòng

---

## 2. Kiến trúc tổng quan

```
┌─────────────────────────────────────────────────────────────────┐
│                        FRONTEND                                  │
│                   (React/Next.js)                               │
└─────────────────────┬───────────────────────────────────────────┘
                      │ POST /api/ChatBot/message
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                   ChatBotController                              │
│                   (API Gateway)                                  │
└─────────────────────┬───────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                     ChatService                                  │
│              (Orchestrator chính)                               │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  1. Quản lý Session (ChatHistoryService)                │   │
│  │  2. Gọi Gemini AI (Semantic Kernel)                     │   │
│  │  3. Xử lý Function Calling (HotelBookingPlugin)         │   │
│  │  4. Quản lý API Keys (GeminiKeyManager)                 │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────┬───────────────────────────────────────────┘
                      │
          ┌───────────┼───────────┐
          ▼           ▼           ▼
┌─────────────┐ ┌───────────┐ ┌─────────────┐
│  Gemini AI  │ │  Database │ │ RoomService │
│  (Google)   │ │ (Sessions)│ │ (Search)    │
└─────────────┘ └───────────┘ └─────────────┘
```

---

## 3. Các thành phần chính

### 3.1 ChatBotController
**File:** `Controllers/ChatBotController.cs`

| Endpoint | Method | Mô tả |
|----------|--------|-------|
| `/api/ChatBot/message` | POST | Gửi tin nhắn cho AI |
| `/api/ChatBot/history/{sessionId}` | GET | Lấy lịch sử chat |
| `/api/ChatBot/session/{sessionId}` | DELETE | Xóa session |
| `/api/ChatBot/health` | GET | Health check |

### 3.2 ChatService
**File:** `Services/AI/ChatService.cs`

Orchestrator chính xử lý:
- Tạo/lấy session
- Gọi Gemini AI
- Xử lý retry khi bị rate limit (429)
- Lưu lịch sử chat

### 3.3 ChatHistoryService
**File:** `Services/AI/ChatHistoryService.cs`

Quản lý lịch sử chat:
- Tạo session mới cho guest/user
- Lưu tin nhắn vào database
- Smart summarization (tóm tắt khi quá nhiều tin nhắn)

### 3.4 HotelBookingPlugin ⭐
**File:** `Services/AI/HotelBookingPlugin.cs`

**Đây là phần quan trọng nhất** - Cho phép AI gọi các function thực tế:

| Function | Mô tả |
|----------|-------|
| `search_available_rooms` | Tìm phòng trống theo ngày, số khách, giá |
| `get_room_details` | Lấy chi tiết loại phòng |
| `get_current_date` | Lấy ngày giờ hiện tại |
| `search_room_type_statistics` | Thống kê loại phòng |

---

## 4. Luồng xử lý chi tiết

### 4.1 Luồng tổng quan

```
User: "Tôi muốn tìm phòng cho 2 người từ 25/12 đến 27/12"
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ STEP 1: ChatBotController nhận request                      │
│ - Validate message (không rỗng, < 2000 ký tự)              │
│ - Log request info                                          │
└─────────────────────────┬───────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ STEP 2: ChatService.SendMessageAsync()                      │
│ - Get/Create Session từ ChatHistoryService                  │
│ - Lấy API Key từ GeminiKeyManager                          │
└─────────────────────────┬───────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ STEP 3: Build Semantic Kernel                               │
│ - Add Gemini Chat Completion                                │
│ - Add HotelBookingPlugin (các function có thể gọi)         │
└─────────────────────────┬───────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ STEP 4: Load Chat History                                   │
│ - Lấy lịch sử chat từ database                             │
│ - Add System Prompt (hướng dẫn AI cách xử lý)              │
│ - Add tin nhắn user mới                                     │
└─────────────────────────┬───────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ STEP 5: Gọi Gemini AI                                       │
│ - Gửi chat history + user message                          │
│ - AI phân tích và quyết định:                              │
│   → Trả lời trực tiếp? hoặc                                │
│   → Gọi function (search_available_rooms)?                 │
└─────────────────────────┬───────────────────────────────────┘
                          │
              ┌───────────┴───────────┐
              ▼                       ▼
┌─────────────────────┐   ┌─────────────────────────────────┐
│ Trả lời trực tiếp   │   │ Gọi Function (Auto-Invoke)      │
│ (câu hỏi đơn giản)  │   │                                 │
└─────────────────────┘   │ AI tự động gọi:                 │
                          │ search_available_rooms(         │
                          │   checkIn: "2025-12-25",        │
                          │   checkOut: "2025-12-27",       │
                          │   guestCount: 2                 │
                          │ )                               │
                          └─────────────┬───────────────────┘
                                        │
                                        ▼
                          ┌─────────────────────────────────┐
                          │ HotelBookingPlugin xử lý        │
                          │ - Gọi RoomService.SearchRooms() │
                          │ - Trả về JSON kết quả           │
                          └─────────────┬───────────────────┘
                                        │
                                        ▼
                          ┌─────────────────────────────────┐
                          │ AI nhận kết quả function        │
                          │ → Format thành câu trả lời      │
                          │ → Trả về cho user               │
                          └─────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────┐
│ STEP 6: Lưu lịch sử & Trả response                         │
│ - Lưu user message vào DB                                   │
│ - Lưu AI response vào DB                                    │
│ - Return ChatResponse cho client                            │
└─────────────────────────────────────────────────────────────┘
```

---

## 5. Function Calling - Chi tiết

### 5.1 search_available_rooms

**Khi nào AI gọi?**
- User hỏi về phòng trống
- User cung cấp ngày check-in/check-out
- User hỏi giá phòng

**Parameters:**

| Param | Type | Required | Mô tả |
|-------|------|----------|-------|
| checkInDate | string | ✅ | Ngày check-in (YYYY-MM-DD) |
| checkOutDate | string | ✅ | Ngày check-out (YYYY-MM-DD) |
| location | string | ❌ | Địa điểm |
| guestCount | int | ❌ | Số khách |
| minPrice | decimal | ❌ | Giá tối thiểu |
| maxPrice | decimal | ❌ | Giá tối đa |

**Ví dụ AI gọi:**
```
User: "Tìm phòng cho 2 người từ 25/12 đến 27/12, giá dưới 2 triệu"

AI tự động gọi:
search_available_rooms(
  checkInDate: "2025-12-25",
  checkOutDate: "2025-12-27", 
  guestCount: 2,
  maxPrice: 2000000
)
```

**Response từ function:**
```json
{
  "success": true,
  "message": "Found 5 available room types",
  "totalCount": 5,
  "data": {
    "rooms": [
      {
        "roomTypeId": 1,
        "typeName": "Deluxe Double",
        "basePriceNight": 1500000,
        "maxOccupancy": 2,
        "availableRoomCount": 3
      },
      ...
    ]
  }
}
```

### 5.2 get_room_details

**Khi nào AI gọi?**
- User hỏi chi tiết về một loại phòng cụ thể
- User muốn biết tiện nghi, hình ảnh

**Parameters:**

| Param | Type | Required | Mô tả |
|-------|------|----------|-------|
| roomTypeId | int | ✅ | ID loại phòng |
| checkInDate | string | ❌ | Ngày check-in |
| checkOutDate | string | ❌ | Ngày check-out |

### 5.3 search_room_type_statistics

**Khi nào AI gọi?**
- User hỏi thống kê
- "Có bao nhiêu loại phòng?"
- "Phòng nào được đặt nhiều nhất?"

**statisticType options:**

| Type | Mô tả |
|------|-------|
| `overview` | Tổng quan tất cả loại phòng |
| `most_booked` | Top phòng được đặt nhiều |
| `by_price` | Lọc theo giá |
| `by_occupancy` | Lọc theo sức chứa |
| `booking_stats` | Thống kê booking |

---

## 6. System Prompt - Hướng dẫn AI

AI được cấu hình với System Prompt chi tiết:

```
Bạn là trợ lý lễ tân khách sạn chuyên nghiệp.

**Vai trò:**
- Giúp khách tìm phòng trống
- Cung cấp thông tin chi tiết về phòng
- Trả lời câu hỏi về dịch vụ
- Hướng dẫn đặt phòng

**Ngữ cảnh:**
- Ngày hôm nay: 2025-12-22
- Website đặt phòng: http://localhost:3000

**Quy tắc quan trọng:**
1. NHỚ thông tin từ tin nhắn trước (số khách, ngày, loại phòng)
2. Khi user hỏi về phòng → GỌI search_available_rooms
3. Chỉ hiển thị tối đa 5 phòng
4. Trả lời ngắn gọn, đúng trọng tâm
5. Dùng ngôn ngữ giống user (Việt/Anh)
```

---

## 7. Session Management

### 7.1 Database Schema

**ChatSession:**
```sql
CREATE TABLE ChatSession (
    SessionId UNIQUEIDENTIFIER PRIMARY KEY,
    AccountId INT NULL,           -- NULL = Guest user
    GuestIdentifier NVARCHAR(255),
    LastActivityAt DATETIME,
    IsActive BIT,
    IsSummarized BIT
)
```

**ChatMessage:**
```sql
CREATE TABLE ChatMessage (
    MessageId INT PRIMARY KEY,
    SessionId UNIQUEIDENTIFIER FK,
    Role NVARCHAR(50),           -- 'user' | 'assistant' | 'system'
    Content NVARCHAR(MAX),
    CreatedAt DATETIME
)
```

### 7.2 Session Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    GUEST USER (Chưa đăng nhập)              │
├─────────────────────────────────────────────────────────────┤
│ Request 1: sessionId = NULL                                 │
│ → Tạo session mới với GUID random                          │
│ → Response: sessionId = "abc-123-..."                       │
│                                                             │
│ Request 2: sessionId = "abc-123-..."                        │
│ → Tìm session existing → Load lịch sử chat                 │
│ → AI nhớ context từ tin nhắn trước                         │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                 LOGGED-IN USER (Đã đăng nhập)               │
├─────────────────────────────────────────────────────────────┤
│ Request: accountId = 123                                    │
│ → Session được link với account                            │
│ → Có thể xem lại lịch sử chat sau khi đăng nhập lại       │
└─────────────────────────────────────────────────────────────┘
```

---

## 8. API Rate Limiting & Retry

### 8.1 Vấn đề
Gemini Free Tier giới hạn **15 requests/minute/API key**

### 8.2 Giải pháp: Multi-Key Rotation

```
┌─────────────────────────────────────────────────────────────┐
│                  GeminiKeyManager                            │
├─────────────────────────────────────────────────────────────┤
│ API Keys Pool:                                              │
│   Key 1: AIza...abc ✅ Available                            │
│   Key 2: AIza...def ✅ Available                            │
│   Key 3: AIza...ghi ❌ Exhausted (rate limited)            │
│                                                             │
│ Khi gọi API:                                                │
│ 1. Lấy key available đầu tiên                              │
│ 2. Nếu bị 429 → Mark key exhausted                         │
│ 3. Retry với key tiếp theo                                 │
│ 4. Max 3 retries với exponential backoff                   │
└─────────────────────────────────────────────────────────────┘
```

### 8.3 Retry Logic

```csharp
while (retryCount <= 3) {
    try {
        // Gọi Gemini API
        return response;
    }
    catch (429 TooManyRequests) {
        // Mark key exhausted
        keyManager.MarkKeyAsExhausted(currentKey);
        
        // Wait với exponential backoff: 3s, 6s, 9s
        await Task.Delay(retryCount * 3 seconds);
        
        // Retry với key mới
        retryCount++;
    }
}
```

---

## 9. Request/Response Models

### 9.1 ChatRequest

```json
{
    "message": "Tìm phòng cho 2 người từ 25/12 đến 27/12",
    "sessionId": "abc123-...",     // Optional - null = new session
    "accountId": 1,                 // Optional - null = guest
    "guestIdentifier": "guest-xyz"  // Optional
}
```

### 9.2 ChatResponse

```json
{
    "isSuccess": true,
    "statusCode": 200,
    "data": {
        "sessionId": "abc123-...",
        "message": "Dạ, tìm thấy 5 phòng phù hợp từ 25/12 đến 27/12:\n\n1. Deluxe Double...",
        "isNewSession": false,
        "timestamp": "2025-12-22T10:30:00Z",
        "metadata": {
            "FinishReason": "Stop"
        }
    }
}
```

---

## 10. Ví dụ hội thoại thực tế

### Conversation Flow:

```
👤 User: "Xin chào"
🤖 AI: "Xin chào! Tôi là trợ lý đặt phòng của khách sạn. 
        Tôi có thể giúp bạn tìm phòng phù hợp. 
        Bạn muốn đặt phòng cho ngày nào và bao nhiêu người ạ?"

👤 User: "Tôi cần phòng cho 2 người"
🤖 AI: "Dạ, 2 người. Bạn muốn check-in và check-out ngày nào ạ?"
        [AI nhớ: guestCount = 2]

👤 User: "Từ 25/12 đến 27/12"
🤖 AI: [Gọi function: search_available_rooms(
          checkIn="2025-12-25", 
          checkOut="2025-12-27", 
          guestCount=2  ← Nhớ từ tin nhắn trước!
        )]
        
        "Dạ, tìm thấy 5 phòng phù hợp từ 25/12 đến 27/12:

        1. Deluxe Double (DLX)
           - Giá: 1,500,000₫/đêm
           - Sức chứa: 2 người
           - Còn 3 phòng trống
           👉 http://localhost:3000/rooms/1

        2. Superior Twin (SUP)
           - Giá: 1,200,000₫/đêm
           - Sức chứa: 2 người
           - Còn 5 phòng trống
           👉 http://localhost:3000/rooms/2

        Bạn muốn biết thêm về phòng nào?"

👤 User: "Cho tôi xem chi tiết phòng Deluxe"
🤖 AI: [Gọi function: get_room_details(roomTypeId=1)]
        
        "Phòng Deluxe Double:
        - Diện tích: 35m²
        - Giường: 1 King size
        - View: Thành phố
        - Tiện nghi: WiFi, TV, Minibar, Bồn tắm
        
        Giá 2 đêm (25-27/12): 3,000,000₫
        
        👉 Đặt ngay: http://localhost:3000/rooms/1"
```

---

## 11. Cấu hình

### appsettings.json

```json
{
  "GeminiSettings": {
    "ApiKeys": [
      "AIzaSy...key1",
      "AIzaSy...key2",
      "AIzaSy...key3"
    ],
    "ModelId": "gemini-1.5-flash",
    "MaxTokens": 2048,
    "Temperature": 0.7
  },
  "FrontendSettings": {
    "BaseUrl": "http://localhost:3000"
  }
}
```

---

## 12. Lưu ý quan trọng

| # | Lưu ý |
|---|-------|
| 1 | **Session phải được lưu ở client** - Frontend cần lưu `sessionId` để duy trì context |
| 2 | **AI có thể gọi nhiều function** - Một request có thể trigger nhiều function calls |
| 3 | **Rate limit** - Free tier giới hạn 15 req/min, cần nhiều API keys |
| 4 | **Ngôn ngữ** - AI tự động detect và trả lời cùng ngôn ngữ với user |
| 5 | **Memory** - AI nhớ context trong cùng session, nhưng có giới hạn token |
| 6 | **Summarization** - Khi history quá dài, hệ thống tự động tóm tắt |

---

## 13. Troubleshooting

| Lỗi | Nguyên nhân | Giải pháp |
|-----|-------------|-----------|
| 429 Too Many Requests | Hết quota API key | Thêm nhiều API keys, chờ 1 phút |
| 403 Forbidden | API key không hợp lệ | Kiểm tra key trong Google AI Studio |
| AI không gọi function | Prompt không rõ ràng | User cần cung cấp đủ thông tin (ngày, số khách) |
| Mất context | SessionId không được gửi | Frontend phải lưu và gửi lại sessionId |

---

## 14. 🧠 Cách AI biết gọi Function nào?

### Trả lời ngắn gọn:

```
┌────────────────────────────────────────────────────────────────┐
│  AI biết gọi function nào nhờ 2 thứ:                           │
│                                                                │
│  1. [Description] attribute trên mỗi function                 │
│  2. Gemini AI so khớp ý định user với description             │
└────────────────────────────────────────────────────────────────┘
```

---

### 14.1 Cơ chế hoạt động

```csharp
// Developer định nghĩa function với [Description]
[KernelFunction("search_available_rooms")]
[Description("Search for available hotel rooms based on dates and guest count")]
//           ↑ AI ĐỌC CÁI NÀY để hiểu function làm gì
public async Task<string> SearchAvailableRoomsAsync(
    [Description("Check-in date (YYYY-MM-DD)")] string checkInDate,
    [Description("Number of guests")] int? guestCount
)
```

---

### 14.2 Luồng xử lý

```
User: "Tìm phòng cho 2 người"
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│ Semantic Kernel gửi cho Gemini:                             │
│ {                                                           │
│   "message": "Tìm phòng cho 2 người",                      │
│   "tools": [                                                │
│     { "name": "search_available_rooms",                     │
│       "description": "Search for available hotel rooms..." │
│     },                                                      │
│     { "name": "get_room_details", ... }                    │
│   ]                                                         │
│ }                                                           │
└─────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│ Gemini AI phân tích:                                        │
│                                                             │
│ "Tìm phòng" → match với "Search for available hotel rooms" │
│ "2 người"   → guestCount = 2                               │
│                                                             │
│ → Quyết định: Gọi search_available_rooms(guestCount=2)     │
└─────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│ Semantic Kernel tự động thực thi:                           │
│                                                             │
│ await _bookingPlugin.SearchAvailableRoomsAsync(guestCount=2)│
│ → Gọi RoomService.SearchRoomTypesAsync()                   │
│ → Query database → Trả về kết quả                          │
└─────────────────────────────────────────────────────────────┘
```

---

### 14.3 Bảng mapping

| User nói | Description match | Function gọi |
|----------|-------------------|--------------|
| "Tìm phòng" | "Search for available rooms" | `search_available_rooms` |
| "Xem phòng Deluxe" | "Get detailed information" | `get_room_details` |
| "Hôm nay ngày mấy" | "Get current date" | `get_current_date` |

---

### 14.4 Tóm tắt 1 câu

> **`[Description]` = Hướng dẫn sử dụng cho AI. AI đọc description → so khớp với ý định user → tự động gọi function phù hợp.**

---

*Cập nhật: 22/12/2025*
