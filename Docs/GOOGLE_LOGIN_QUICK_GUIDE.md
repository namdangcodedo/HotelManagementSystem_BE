# Google Login - Quick Start Guide

## 🚀 Cách sử dụng nhanh nhất (2 bước)

### Bước 1: Tạo button Login

```javascript
// Login button handler
async function handleGoogleLogin() {
  // Gọi API để lấy Google URL
  const response = await fetch('http://localhost:8080/api/Authentication/google-login-url');
  const data = await response.json();
  
  // Redirect đến Google
  window.location.href = data.data.url;
}
```

### Bước 2: Xử lý callback (trang `/auth/google/callback`)

```javascript
// Khi Google redirect về (URL có ?code=...)
async function handleCallback() {
  // Lấy code từ URL
  const params = new URLSearchParams(window.location.search);
  const code = params.get('code'); // ⚠️ ĐỪNG encode lại code!
  
  // ❌ ĐỪNG LÀM: encodeURIComponent(code)
  // ✅ ĐÚNG: Dùng code trực tiếp
  
  // Gửi code lên backend
  const response = await fetch('http://localhost:8080/api/Authentication/exchange-google', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      code: code, // Code trực tiếp, không encode
      redirectUri: window.location.origin + '/auth/google/callback'
    })
  });
  
  const data = await response.json();
  
  if (data.isSuccess) {
    // Lưu token
    localStorage.setItem('access_token', data.data.token);
    localStorage.setItem('refresh_token', data.data.refreshToken);
    localStorage.setItem('user', JSON.stringify(data.data.user));
    
    // Redirect đến dashboard
    window.location.href = '/dashboard';
  }
}

// Auto chạy khi load trang callback
handleCallback();
```

---

## ⚠️ LỖI THƯỜNG GẶP

### 1. Code bị double-encode (%252F thay vì %2F)

**Nguyên nhân:** Encode code nhiều lần

**Fix:**
```javascript
// ❌ SAI
const code = encodeURIComponent(params.get('code'));

// ✅ ĐÚNG
const code = params.get('code'); // Browser đã tự decode
```

### 2. "redirect_uri_mismatch"

**Nguyên nhân:** redirectUri không khớp

**Fix:**
```javascript
// Phải khớp chính xác với Google Console
redirectUri: 'http://localhost:3000/auth/google/callback'
```

### 3. "invalid_grant"

**Nguyên nhân:** Code đã dùng hoặc hết hạn

**Fix:** Lấy code mới (login lại)

---

## 🔧 Test nhanh

### Cách 1: Test với curl (đúng cách)

```bash
# Bước 1: Lấy code từ browser
# Mở: http://localhost:8080/api/Authentication/google-login-url
# Copy URL → mở trong browser → login Google
# Lấy code từ URL callback (đã decode)

# Bước 2: Test exchange
curl -X POST http://localhost:8080/api/Authentication/exchange-google \
  -H "Content-Type: application/json" \
  -d '{
    "code": "4/0Ab32j93YhSpENUGDpRk0zfEpgIXeIEJX7jjfmumBkdwuzx3cYnyu",
    "redirectUri": "http://localhost:3000/auth/google/callback"
  }'
```

### Cách 2: Test với browser DevTools

1. Mở trang có button Login
2. F12 → Console
3. Chạy:

```javascript
// Test lấy URL
fetch('http://localhost:8080/api/Authentication/google-login-url')
  .then(r => r.json())
  .then(data => console.log(data.data.url));

// Test exchange (thay CODE_HERE)
fetch('http://localhost:8080/api/Authentication/exchange-google', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    code: 'CODE_HERE',
    redirectUri: 'http://localhost:3000/auth/google/callback'
  })
})
.then(r => r.json())
.then(data => console.log(data));
```

---

## 📋 Checklist trước khi deploy

- [ ] Google Console có đăng ký đúng redirect URI: `http://localhost:3000/auth/google/callback`
- [ ] Backend `appsettings.json` có ClientId và ClientSecret đúng
- [ ] Frontend không encode code thêm lần nữa
- [ ] redirectUri trong code khớp với Google Console
- [ ] CORS đã enable cho frontend origin
- [ ] Test cả flow từ đầu đến cuối

---

## 🎯 HTTP vs HTTPS

**Trả lời:** API hoạt động với **CẢ HTTP và HTTPS**

- Development: `http://localhost:8080` - OK ✅
- Production: `https://yourdomain.com` - REQUIRED ✅

**Lưu ý:**
- Google yêu cầu HTTPS cho production redirect URIs
- Localhost cho phép HTTP (chỉ development)
- Code không phụ thuộc HTTP/HTTPS, phụ thuộc vào encoding đúng

---

## 💡 TL;DR (Tóm tắt siêu ngắn)

```javascript
// 1. Login button
fetch('/api/Authentication/google-login-url')
  .then(r => r.json())
  .then(data => window.location.href = data.data.url);

// 2. Callback page
const code = new URLSearchParams(location.search).get('code');
fetch('/api/Authentication/exchange-google', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    code: code, // ⚠️ Không encode!
    redirectUri: location.origin + '/auth/google/callback'
  })
})
.then(r => r.json())
.then(data => {
  localStorage.setItem('access_token', data.data.token);
  location.href = '/dashboard';
});
```

**Xong! Chỉ có thế thôi.** 🚀

---

**Created:** 2025-11-23

