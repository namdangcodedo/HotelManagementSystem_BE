CHATBOT IMPLEMENTATION FLOW
==========================

Mục đích
--------
Tài liệu này mô tả luồng hội thoại (conversation flow) cho chatbot trong dự án, cách hệ thống lưu và tái sử dụng context để model trả lời chính xác, và hướng dẫn chi tiết về kiến trúc prompt để "search / đặt phòng" (kể cả function schema và ví dụ). Tập trung vào các quy tắc chuẩn hoá, xác thực và chiến lược hỏi rõ ràng (clarification).

Tổng quan luồng (high-level)
---------------------------
1. Client gửi message kèm metadata (SessionId, AccountId, GuestIdentifier, locale...).
2. API controller validate request, lưu user message tạm thời nếu cần, lấy/khởi tạo ChatSession từ DB.
3. Tải lịch sử message cho Session (có paging / tóm tắt nếu quá dài).
4. Xây prompt / context theo quy tắc (system + conversation summary + recent messages + user message + instruction suffix).
5. Gọi model (Gemini via Semantic Kernel) với AutoInvokeKernelFunctions / function schemas đã đăng ký.
6. Nếu model trả function_call: thực hiện function (backend), lưu kết quả (role=function) và (nếu cần) gọi model lần 2 để render final assistant message.
7. Lưu tất cả messages (user, assistant, function result) vào DB, cập nhật ChatSession.LastActivityAt.
8. Trả response cho client.

Key concepts
------------
- Session: đại diện cho một cuộc hội thoại liên tục; fields quan trọng: SessionId, AccountId, GuestIdentifier, ConversationSummary, IsSummarized, LastActivityAt, IsActive.
- Message: lưu Content, Role (user/system/assistant/function), Metadata (JSON), TokenCount, CreatedAt.
- ConversationSummary: tóm tắt ngắn những thông tin then chốt để giảm token khi lịch sử dài.
- Prompt assembly: thứ tự và nội dung gửi model phải được chuẩn hoá để đảm bảo tính nhất quán.

Vấn đề thực tế quan sát
-----------------------
- Nhiều session mới được tạo không mong muốn => cần kiểm tra mapping SessionId/GuestIdentifier.
- Lịch sử trả về chỉ chứa 1 message => phải đảm bảo lưu user message trước khi gọi model và nạp lại lịch sử để model có context.

Phần chính: Kiến trúc prompt chi tiết cho "search / đặt phòng"
-------------------------------------------------------------
Mục tiêu của phần này: cung cấp một hướng dẫn cụ thể để xây dựng prompt/flow nhằm trích xuất thông tin chính xác, gọi function tìm phòng, và render kết quả phù hợp với UX.

1) Contract (inputs / outputs / lỗi)
- Inputs:
  - userMessage: string
  - sessionContext: { conversationSummary?: string, recentMessages?: Message[] }
  - accountMeta: { accountId?, guestIdentifier?, locale? }
- Outputs:
  - function_call to `search_available_rooms` with normalized payload (guests:int, checkIn:ISO date, checkOut:ISO date, ...)
  - OR assistant clarification question (single concise question) if required slots missing/ambiguous
  - OR assistant direct answer if no function needed
- Error modes:
  - Missing required slots (guests, checkIn, checkOut) => ask clarification.
  - Ambiguous dates => ask confirm or apply normalization rules then confirm if necessary.
  - Validation fails (checkOut <= checkIn) => ask user to correct.

Success criteria:
- If function_call occurs, payload must contain ISO dates and guests >= 1.

2) Các slot cần trích xuất (priority)
- guests (int) — bắt buộc
- checkIn (yyyy-MM-dd) — bắt buộc
- checkOut (yyyy-MM-dd) — bắt buộc
- roomType (string, optional)
- bedType (string, optional)
- priceMin / priceMax (number, optional)
- amenities (array[string], optional)
- flexibleDatesDays (int, optional)
- specialRequests (string, optional)
- currency (string, optional)

3) Quy tắc chuẩn hoá ngày (date normalization)
- Hỗ trợ định dạng: DD/MM/YYYY, D/M, DD/MM, YYYY-MM-DD, "12/12", "ngày 12", "từ 12/12 đến 14/12", "2 đêm từ 12/12".
- Nếu thiếu năm: giả định current year; nếu kết quả là ngày trong quá khứ => giả định next year.
- Nếu chỉ có 1 ngày (chỉ checkIn) => KHÔNG tự động tạo checkOut; hỏi người dùng "Bạn sẽ ở bao nhiêu đêm?" (tránh sai khớp).
- Sử dụng timezone server (UTC) kết hợp locale của user để chuyển tương đối (ví dụ "cuối tuần này").
- Kết quả chuẩn hoá phải ở ISO yyyy-MM-dd trong payload function.

4) System prompt (business rules) — mẫu
- Mỗi lần build prompt, chèn 1 system message ngắn mô tả role & rules. Thay thế các biến cấu hình:
  - HOTEL_NAME và BASE_URL từ config.

Mẫu (tiếng Việt):
"Bạn là trợ lý đặt phòng cho [HOTEL_NAME]. Khi user hỏi về khả năng đặt phòng hoặc cung cấp ngày & số khách, cố gắng gọi function `search_available_rooms` với payload đã chuẩn hoá (ISO dates, guests int). Các trường bắt buộc: guests, checkIn, checkOut. Nếu thiếu hoặc mơ hồ, hỏi 1 câu rõ ràng để bổ sung. Không bịa thông tin. Trả lời bằng ngôn ngữ của user. Đừng gửi toàn bộ lịch sử; chỉ gửi summary + recent messages. Base booking link: [BASE_URL]."

5) Instruction suffix / few-shot (đi kèm)
- Sau system + context, đính kèm instruction suffix: một câu ngắn chỉ dẫn khi nào phải gọi function và định dạng trả về (function_call JSON). Ví dụ:
  "If the user is asking to find available rooms, call `search_available_rooms` with normalized ISO dates and guest count. Required fields: guests, checkIn, checkOut. If any required field is missing or ambiguous, ask one concise clarifying question. When calling the function, return a `function_call` object following the schema provided."
- Thêm 1–2 few-shot examples (user -> function_call) để model học cách map natural language -> structured payload.

6) Prompt assembly order (strict)
1. System (business rules + current date + BaseUrl)
2. Conversation summary (if exists) as system message
3. Recent messages (chronological, last N messages; prefer N=6 or until token budget)
4. Few-shot examples (optional; short)
5. User message (latest)
6. Instruction suffix (call function if appropriate)

7) Function schema (JSON) — `search_available_rooms`
- Sử dụng schema để model trả về `function_call` có payload hợp lệ.

Mẫu schema:
```json
{
  "name": "search_available_rooms",
  "description": "Search for available rooms given date range and constraints",
  "parameters": {
    "type": "object",
    "properties": {
      "guests": { "type": "integer", "minimum": 1 },
      "checkIn": { "type": "string", "format": "date" },
      "checkOut": { "type": "string", "format": "date" },
      "roomType": { "type": ["string", "null"] },
      "bedType": { "type": ["string", "null"] },
      "priceMin": { "type": ["number", "null"] },
      "priceMax": { "type": ["number", "null"] },
      "amenities": { "type": "array", "items": { "type": "string" } },
      "flexibleDatesDays": { "type": "integer", "minimum": 0 },
      "specialRequests": { "type": ["string", "null"] },
      "currency": { "type": ["string", "null"] }
    },
    "required": ["guests","checkIn","checkOut"]
  }
}
```

8) Ví dụ mapping từ user -> payload
- User: "Mình muốn đặt phòng cho 2 người từ 12/12 đến 14/12, cần view biển, giá dưới 2 triệu/đêm"
  - Payload normalized:
```json
{
  "guests": 2,
  "checkIn": "2025-12-12",
  "checkOut": "2025-12-14",
  "amenities": ["sea view"],
  "priceMax": 2000000,
  "currency": "VND"
}
```
- User: "Tôi cần phòng 1 người ngày 20/12"
  - Assistant should ask: "Bạn nhận phòng ngày 20/12 và trả phòng ngày nào ạ?" (clarify checkOut)

9) Clarification strategy (rules)
- ALWAYS ask only 1 concise question per turn.
- Prefer closed questions to collect missing slot(s) (e.g., "Bạn đặt cho bao nhiêu người?", "Bạn nhận phòng ngày nào và trả phòng ngày nào?").
- If date ambiguous (no year) and that date would be in the past with current year, propose next year: "Bạn có ý là 12/12/2026 không?".
- If user gives contradictory info, restate extracted values and ask to confirm: "Bạn trước đó nói 2 người, bây giờ là 3 người, bạn xác nhận 3 người chứ?"

10) Validation rules (server-side & client-side)
- Guests >= 1
- checkIn and checkOut must be valid ISO dates; checkOut > checkIn
- priceMin <= priceMax
- If flexibleDatesDays provided, it must be small (e.g., <= 7)
- If validation fails: DO NOT call search API; return assistant prompt to clarify.

11) Post-function flow
- Backend receives function payload -> validate -> call search engine / room repository -> create structured result (rooms array)
- Save function result into DB as role = "function" with metadata containing raw API response
- Optionally call model again to render final assistant message (pass system prompt + conversation summary + last messages + function result as function role) -> assistant final text
- Save final assistant message to DB and return to client

12) Result presentation guidelines (assistant output)
- Short intro: "Dạ, chúng tôi có X loại phòng phù hợp từ [checkIn] đến [checkOut]:"
- Present up to top 3 rooms in concise cards each containing:
  - Name, price/night, capacity, key amenities, booking link: [BASE_URL]/rooms/[roomTypeId]
- Offer next actions: "Bạn muốn đặt phòng nào (1/2/3), xem thêm phòng, hoặc sửa tiêu chí?"
- If zero results: suggest alternatives with clear options (change dates, increase price, remove amenity)

13) Token & history management
- Sequence:
  - Save user message immediately
  - Load conversation summary (if exists) + last N messages (N chosen to respect token budget, e.g., 6)
  - Build prompt as described
- If total tokens > limit => use ConversationSummary + last N messages only
- Summarize older messages offline or via model when threshold reached; save summary to ChatSession.ConversationSummary

14) Tests & QA
- Unit tests:
  - dateParser tests for many formats
  - slot extraction tests
  - function payload validation
- Integration tests:
  - Full conversation simulation: user provides guests -> dates -> assistant calls function -> backend returns rooms -> assistant final message
  - Ambiguous date test: user says "12/12" and system asks to confirm
- Manual scenarios: Vietnamese colloquial phrases, relative dates, multiple ways to express guests

15) Monitoring & metrics
- Track: number of function_calls (`search_available_rooms`) per session, avg latency, % of clarifying questions, validation failures, zero-result rates
- Log normalized payload (avoid PII), function response sizes, and any validation rejections

16) Implementation checklist (developer tasks)
- [ ] Ensure `AddMessageAsync` is called to persist user message before calling model
- [ ] Build system prompt with runtime config values (BaseUrl, HotelName)
- [ ] Register `search_available_rooms` function schema in Semantic Kernel integration
- [ ] Server-side payload validator for function_call inputs
- [ ] Implement backend handler that queries room service/repository and returns structured result
- [ ] Save function responses and final assistant message into DB
- [ ] Add unit + integration tests as described

17) Example conversation (end-to-end)
- User: "Tôi muốn đặt phòng cho 2 người"
  - Assistant: "Dạ, bạn muốn nhận phòng ngày nào và trả phòng ngày nào ạ?"
- User: "12/12 đến 14/12"
  - Assistant (function_call): search_available_rooms with payload {guests:2, checkIn:"2025-12-12", checkOut:"2025-12-14"}
  - Backend: runs search, returns rooms
  - Assistant (final): lists top 3 rooms + booking links + next steps

18) Next steps
- Nếu bạn muốn, tôi có thể:
  - A. Bổ sung few-shot examples thêm vào `Docs/` (một file riêng `CHATBOT_PROMPT_TEMPLATES.md`)
  - B. Triển khai code: register function schema, server-side validator và handler for `search_available_rooms` trong backend
  - C. Thêm unit & integration tests trong `AppBackend.Tests`.

Ghi chú
------
Tài liệu này nhằm mục đích hướng dẫn đội dev/QA để chatbot có hành vi nhất quán khi tìm phòng và đặt phòng. Nếu cần, tôi sẽ tách phần "prompt templates" sang file riêng để dễ maintain và viết unit tests cho prompt builder.
