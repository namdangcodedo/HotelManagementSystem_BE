# 🔑 Gemini API Key Management System

## 📋 Tổng quan

Hệ thống quản lý API keys của Gemini với:
- **Retry logic** tự động khi gặp lỗi 429 (Too Many Requests)
- **Auto-switch API key** khi key hiện tại hết quota
- **Blacklist cache** để đánh dấu key đã hết quota
- **Auto-reset blacklist** vào 7h sáng mỗi ngày (theo quota reset của Gemini)

## 🎯 Tính năng chính

### 1. **Multiple API Keys Load Balancing**
```json
{
  "GeminiSettings": {
    "ApiKeys": [
      "AIzaSyBjdW7xdhMXk9sUf40MFLbyFiRqL24mceM",  // Key 1
      "AIzaSyAic4Wem2qrZg0NkKa8VtVN7jyJ5aaKx6k",  // Key 2
      "AIzaSyAvQYsPSYFtnjkTQClaKsq6P-kEYYUDERM"   // Key 3
    ],
    "ModelId": "gemini-2.5-flash",
    "MaxTokens": 1000000,
    "Temperature": 0.7
  }
}
```

### 2. **Retry Logic với Exponential Backoff**

Khi gặp lỗi 429:
1. **Retry lần 1** → Đợi 2 giây → Thử key khác
2. **Retry lần 2** → Đợi 4 giây → Thử key khác
3. **Retry lần 3** → Đợi 6 giây → Thử key khác
4. **Fail** → Trả về lỗi cho user

### 3. **Blacklist Cache**

Khi một API key hết quota (429), nó sẽ được thêm vào **blacklist cache**:
- **Cache key:** `GeminiApiKey_Blacklist`
- **Expiration:** 1 ngày
- **Storage:** `IMemoryCache` (in-memory)

### 4. **Auto-Reset vào 7h sáng**

Gemini free tier reset quota vào **7:00 AM UTC** mỗi ngày, nên hệ thống tự động:
- Kiểm tra thời gian mỗi khi gọi API
- Nếu đã qua 7h sáng → Reset blacklist
- Tất cả keys lại available

## 🔄 Flow hoạt động

### Flow bình thường:
```
User Request → GetAvailableKey() → Random key from available list → Call Gemini API → Success
```

### Flow khi gặp 429:
```
User Request
  ↓
GetAvailableKey() → Key 1
  ↓
Call Gemini API → 429 Error
  ↓
MarkKeyAsExhausted(Key 1) → Add to blacklist
  ↓
Retry: GetAvailableKey() → Key 2 (from remaining keys)
  ↓
Call Gemini API → Success ✅
```

### Flow khi TẤT CẢ keys hết quota:
```
User Request
  ↓
GetAvailableKey() → All keys in blacklist
  ↓
Auto-reset blacklist (emergency)
  ↓
GetAvailableKey() → Try Key 1 again
  ↓
If still 429 → Return error to user
```

## 📊 Logs mẫu

### ✅ Request thành công:
```
info: Using Gemini Model: gemini-2.5-flash
info: API Key (first 10 chars): AIzaSyAvQY...
info: Available API Keys: 3/3
info: Calling Gemini API with AutoInvokeKernelFunctions enabled...
info: Gemini API responded successfully
```

### ⚠️ Retry khi gặp 429:
```
info: Available API Keys: 3/3
warn: ⚠️ HTTP 429: Too Many Requests (Retry 1/3)
warn: ⚠️ API Key marked as exhausted: AIzaSyBjdW7xdh... (1/3 keys exhausted)
warn: 🔄 API key exhausted. Remaining keys: 2/3
info: ⏳ Waiting 2 seconds before retry...
info: 🔄 Retry attempt 1/3
info: Available API Keys: 2/3
info: API Key (first 10 chars): AIzaSyAic4...
info: Gemini API responded successfully ✅
```

### 🔄 Auto-reset vào 7h sáng:
```
info: 🔄 Auto-resetting API key blacklist at 11/30/2025 07:00:05 (Gemini quota refreshes daily at 7 AM)
info: ✅ Blacklist reset successfully. All 3 API keys are now available
```

### ❌ Tất cả keys đều hết:
```
warn: ⚠️ All API keys are exhausted! Resetting blacklist...
info: ✅ Blacklist reset successfully. All 3 API keys are now available
info: Selected API key: AIzaSyBjdW7xdh... (Available: 3/3)
```

## 🔧 API Methods

### `IGeminiKeyManager` Interface

#### 1. `GetAvailableKey()`
Lấy một API key còn available (không trong blacklist)

```csharp
var apiKey = _keyManager.GetAvailableKey();
// Returns: "AIzaSyBjdW7xdhMXk9sUf40MFLbyFiRqL24mceM"
```

#### 2. `MarkKeyAsExhausted(string apiKey)`
Đánh dấu một key đã hết quota

```csharp
_keyManager.MarkKeyAsExhausted(apiKey);
// Key được thêm vào blacklist cache
```

#### 3. `GetAvailableKeyCount()`
Đếm số key còn available

```csharp
var available = _keyManager.GetAvailableKeyCount();
// Returns: 2 (nếu có 1 key trong blacklist)
```

#### 4. `GetSettings()`
Lấy settings configuration

```csharp
var settings = _keyManager.GetSettings();
// Returns: GeminiSettings object
```

## 🎯 Best Practices

### 1. **Thêm nhiều API keys**
Càng nhiều keys, càng ít bị downtime:
```json
{
  "GeminiSettings": {
    "ApiKeys": [
      "key1",  // Primary
      "key2",  // Fallback 1
      "key3",  // Fallback 2
      "key4",  // Fallback 3
      "key5"   // Emergency
    ]
  }
}
```

### 2. **Monitor logs**
Setup alerts khi:
- Available keys < 2
- Tất cả keys đều trong blacklist
- Retry rate cao

### 3. **Quota management**
Gemini Free Tier limits (PER API KEY):
- **15 RPM** (Requests Per Minute) - Rất thấp!
- **1,500 RPD** (Requests Per Day)
- **1M TPM** (Tokens Per Minute)

⚠️ **QUAN TRỌNG:** Với 15 RPM, bạn chỉ có thể gọi **1 request mỗi 4 giây**!

**Giải pháp:**
→ Với 3 keys: **45 RPM** (1 request mỗi 1.3 giây)
→ Với 5 keys: **75 RPM** (1 request mỗi 0.8 giây)
→ Với 10 keys: **150 RPM** (2 requests/giây)

**Khuyến nghị:**
- **Development:** 3-5 keys (đủ để test)
- **Production với ít traffic:** 5-10 keys
- **Production với nhiều traffic:** Upgrade sang **Gemini Pro (Paid)** để có unlimited quota

### 3.1 **Tránh bị 429 - Best Practices**

#### Option 1: Thêm nhiều API keys (Miễn phí)
Tạo nhiều Google accounts và lấy API keys:
```json
{
  "GeminiSettings": {
    "ApiKeys": [
      "key1",   // Account 1
      "key2",   // Account 2
      "key3",   // Account 3
      "key4",   // Account 4
      "key5",   // Account 5
      "key6",   // Account 6
      "key7",   // Account 7
      "key8",   // Account 8
      "key9",   // Account 9
      "key10"   // Account 10
    ]
  }
}
```

Với 10 keys → **150 requests/minute** → Đủ cho small-medium traffic

#### Option 2: Implement Rate Limiting ở Backend
Thêm delay giữa các requests từ cùng user:
```csharp
// Trong ChatBotController
[RateLimit(
    Name = "ChatBot",
    PermitLimit = 5,           // 5 requests
    Window = 60,               // per 60 seconds
    QueueLimit = 2             // Queue 2 requests
)]
public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
{
    // ...
}
```

#### Option 3: Implement Client-side Throttling
Frontend nên:
- Disable send button khi đang process
- Hiển thị "AI is thinking..." message
- Debounce input (đợi user ngừng typing mới gửi)

#### Option 4: Upgrade sang Gemini Pro (Recommended cho Production)
Pricing: ~$0.00025 per 1K input tokens (~$0.25/1M tokens)
Benefits:
- **No rate limits** (fair usage policy)
- Better quality responses
- Priority support
- SLA guaranteed

## 🐛 Troubleshooting

### Vấn đề: "All API keys are currently rate-limited"
**Nguyên nhân:** Tất cả keys đã hết quota
**Giải pháp:**
1. Đợi đến 7h sáng (quota reset)
2. Hoặc thêm keys mới vào config
3. Hoặc upgrade sang Gemini Pro (paid)

### Vấn đề: Blacklist không reset vào 7h sáng
**Nguyên nhân:** Server timezone khác UTC
**Giải pháp:**
- Check timezone: `date`
- Adjust RESET_TIME trong code nếu cần
- Hoặc chạy manual reset: Clear cache

### Vấn đề: Memory cache bị clear khi restart server
**Nguyên nhân:** IMemoryCache là in-memory, mất khi restart
**Giải pháp:**
- Normal behavior, blacklist sẽ rebuild
- Nếu cần persistent: Chuyển sang Redis

## 📈 Monitoring Metrics

Các metrics nên track:

1. **API Key Health**
   - Available keys count
   - Blacklisted keys count
   - Keys in rotation

2. **Request Success Rate**
   - Success rate per key
   - Retry rate
   - 429 error rate

3. **Response Time**
   - Average response time
   - P95, P99 latency
   - Timeout rate

## 🔒 Security Notes

1. **API Keys trong logs**
   - Chỉ log 10-15 ký tự đầu
   - Không log full key
   - Redact trong production logs

2. **Environment Variables**
   - Nên dùng env vars thay vì hardcode trong appsettings.json
   - Dùng Azure Key Vault hoặc AWS Secrets Manager

3. **Rate Limiting**
   - Implement rate limiting ở application level
   - Protect API khỏi abuse
   - Use `RateLimitAttribute`

## 🚀 Future Enhancements

Các tính năng có thể thêm:

1. **Redis Cache** thay vì IMemoryCache
2. **Health Check Endpoint** để monitor key status
3. **Auto-scale keys** dựa trên load
4. **Metrics Dashboard** với Prometheus/Grafana
5. **Smart key selection** dựa trên latency history

---
**Updated:** 2025-11-29
**Author:** System Documentation
