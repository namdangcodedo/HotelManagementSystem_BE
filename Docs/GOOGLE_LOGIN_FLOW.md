# Google Login OAuth2 Flow - Hướng dẫn chi tiết

## 📋 Tổng quan

Hệ thống sử dụng **Exchange Flow** - luồng OAuth2 chuẩn cho SPA/Frontend:

**🔄 Luồng hoạt động:**
1. User click "Login with Google" trên Frontend
2. Frontend tạo Google OAuth URL và redirect user đến Google
3. User đăng nhập Google
4. **Google redirect về Frontend** với authorization code trong URL
5. Frontend gửi code lên Backend qua API `/api/Authentication/exchange-google`
6. Backend exchange code với Google, tạo/đăng nhập user, và trả về JWT token
7. Frontend lưu token và chuyển user vào app

---

## ⚙️ Cấu hình Google Console

### Bước 1: Tạo OAuth 2.0 Client
1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Chọn project hoặc tạo mới
3. Vào **APIs & Services** → **Credentials**
4. Click **Create Credentials** → **OAuth client ID**
5. Chọn **Application type: Web application**

### Bước 2: Cấu hình Authorized redirect URIs
```
⚠️ QUAN TRỌNG: Redirect URI phải là URL của FRONTEND (không phải Backend)

Development:
  - http://localhost:3000/auth/google/callback

Production:
  - https://your-frontend-domain.com/auth/google/callback
```

### Bước 3: Cấu hình Authorized JavaScript origins
```
Development:
  - http://localhost:3000
  - http://localhost:8080 (Backend, nếu cần gọi API từ browser)

Production:
  - https://your-frontend-domain.com
  - https://your-api-domain.com (Backend)
```

### Bước 4: Copy Client ID và Client Secret
Sau khi tạo xong, copy:
- **Client ID**: `166370023031-xxxxx.apps.googleusercontent.com`
- **Client Secret**: `GOCSPX-xxxxx`

Lưu vào Backend `appsettings.json`

### Bước 5: Chia sẻ Client ID cho Frontend

-**⚠️ QUAN TRỌNG:** Frontend cần **Client ID** để tạo Google OAuth URL
-
-**Client ID là PUBLIC - an toàn khi để ở frontend:**
-```javascript
-// Frontend .env hoặc config file
-REACT_APP_GOOGLE_CLIENT_ID=166370023031-5fb6unqprsf9f020f1n0cvhk333kdbj4.apps.googleusercontent.com
-```
-
-**KHÔNG BAO GIỜ để Client Secret ở frontend!**
-```
-✅ Client ID → Public → Có thể dùng ở frontend
-❌ Client Secret → Private → Chỉ ở backend
-```
-
-**Cách lấy Client ID:**
-
-**Option 1: Hardcode trong frontend (đơn giản nhất)**
-```javascript
-const CLIENT_ID = '166370023031-5fb6unqprsf9f020f1n0cvhk333kdbj4.apps.googleusercontent.com';
-```
-
-**Option 2: Tạo API để backend trả về Client ID** (khuyến nghị cho flexibility)
-```javascript
-// Frontend gọi API này để lấy Client ID
-const response = await fetch('http://localhost:8080/api/Authentication/google-config');
-const { clientId } = await response.json();
-```
-
-Backend API mẫu:
-```csharp
-[HttpGet("google-config")]
-public IActionResult GetGoogleConfig()
-{
-    return Ok(new { 
-        clientId = _settings.ClientId,
-        // KHÔNG trả về ClientSecret
-    });
-}
-```
+**⚠️ QUAN TRỌNG:** Frontend cần lấy Google OAuth URL từ Backend
+
+**✅ KHUYẾN NGHỊ: Sử dụng API `/api/Authentication/google-login-url`**
+
+Frontend **KHÔNG CẦN** biết Client ID, chỉ cần gọi API để lấy URL:
+
+```javascript
+// Frontend code - CÁCH KHUYẾN NGHỊ
+const response = await fetch('http://localhost:8080/api/Authentication/google-login-url');
+const data = await response.json();
+
+if (data.isSuccess) {
+  // Redirect user to Google
+  window.location.href = data.data.url;
+}
+```
+
+**Ưu điểm:**
+- ✅ Frontend không cần biết Client ID
+- ✅ Tất cả config tập trung ở Backend
+- ✅ Dễ thay đổi cho nhiều môi trường (dev/staging/prod)
+- ✅ Backend control hoàn toàn OAuth flow
+
+**Response từ API:**
+```json
+{
+  "isSuccess": true,
+  "data": {
+    "url": "https://accounts.google.com/o/oauth2/v2/auth?client_id=...&redirect_uri=...&response_type=code&scope=openid%20email%20profile",
+    "redirectUri": "http://localhost:3000/auth/google/callback",
+    "scopes": ["openid", "email", "profile"]
+  }
+}
+```
+
+---
+
+**Alternative: Hardcode Client ID (không khuyến nghị)**
+
+Nếu bạn muốn Frontend tự tạo URL (không phụ thuộc Backend API call):
+
+```javascript
+// Frontend .env
+REACT_APP_GOOGLE_CLIENT_ID=166370023031-5fb6unqprsf9f020f1n0cvhk333kdbj4.apps.googleusercontent.com
+
+// Frontend code
+const CLIENT_ID = process.env.REACT_APP_GOOGLE_CLIENT_ID;
+const googleAuthUrl = 
+  `https://accounts.google.com/o/oauth2/v2/auth?` +
+  `client_id=${CLIENT_ID}&` +
+  `redirect_uri=${encodeURIComponent('http://localhost:3000/auth/google/callback')}&` +
+  `response_type=code&` +
+  `scope=openid%20email%20profile`;
+window.location.href = googleAuthUrl;
+```
+
+**⚠️ Lưu ý:**
+- Client ID là **PUBLIC** - an toàn khi để ở frontend
+- Client Secret là **PRIVATE** - **KHÔNG BAO GIỜ** để ở frontend

---

## 💻 Implementation - Frontend

### Bước 1: Tạo Google Login Button

```javascript
// File: components/GoogleLoginButton.jsx (React example)
import React from 'react';

const GoogleLoginButton = () => {
  const handleGoogleLogin = async () => {
-    // ⚠️ Thay CLIENT_ID bằng Client ID thực từ Google Console
-    const CLIENT_ID = '166370023031-xxxxx.apps.googleusercontent.com';
-    const REDIRECT_URI = 'http://localhost:3000/auth/google/callback';
-    const SCOPE = 'openid email profile';
-
-    const googleAuthUrl = 
-      `https://accounts.google.com/o/oauth2/v2/auth?` +
-      `client_id=${CLIENT_ID}&` +
-      `redirect_uri=${encodeURIComponent(REDIRECT_URI)}&` +
-      `response_type=code&` +
-      `scope=${encodeURIComponent(SCOPE)}`;
-
-    // Redirect user
-    window.location.href = googleAuthUrl;
+    try {
+      // Gọi API để lấy Google login URL
+      const response = await fetch('http://localhost:8080/api/Authentication/google-login-url');
+      const data = await response.json();
+      
+      if (data.isSuccess) {
+        // Redirect user đến Google
+        window.location.href = data.data.url;
+      } else {
+        console.error('Failed to get Google login URL:', data.message);
+      }
+    } catch (error) {
+      console.error('Error getting Google login URL:', error);
+    }
  };

  return (
    <button onClick={handleGoogleLogin} className="google-login-btn">
      <img src="/google-icon.png" alt="Google" />
      Sign in with Google
    </button>
  );
};

export default GoogleLoginButton;
```
