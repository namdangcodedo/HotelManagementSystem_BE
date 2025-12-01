# Frontend Google Login - Code Example Đầy Đủ

## 📋 Tổng quan

File này chứa code mẫu **HOÀN CHỈNH** để implement Google Login ở Frontend (React/Vue/Vanilla JS).

---

## 🎯 Luồng hoạt động

```
1. User click button "Login with Google"
   ↓
2. Frontend gọi API GET /google-login-url
   ↓
3. Redirect user đến Google OAuth URL
   ↓
4. User login Google → Google redirect về: 
   http://localhost:3000/auth/google/callback?code=4/0Ab32j93YhSpE...
   ↓
5. Frontend parse code từ URL
   ↓
6. Frontend POST code lên /exchange-google
   ↓
7. Backend trả về token + user info
   ↓
8. Frontend lưu token và redirect đến dashboard
```

---

## 💻 React Implementation (Khuyến nghị)

### File 1: `components/GoogleLoginButton.tsx`

```typescript
import React, { useState } from 'react';
import './GoogleLoginButton.css';

const GoogleLoginButton: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleGoogleLogin = async () => {
    try {
      setLoading(true);
      setError(null);

      // Gọi API để lấy Google OAuth URL
      const response = await fetch('http://localhost:8080/api/Authentication/google-login-url');
      const data = await response.json();

      if (data.isSuccess) {
        // Redirect user đến Google
        console.log('Redirecting to Google:', data.data.url);
        window.location.href = data.data.url;
      } else {
        setError(data.message || 'Không thể lấy Google login URL');
        setLoading(false);
      }
    } catch (err) {
      console.error('Error getting Google login URL:', err);
      setError('Lỗi kết nối. Vui lòng thử lại.');
      setLoading(false);
    }
  };

  return (
    <div className="google-login-container">
      <button 
        onClick={handleGoogleLogin} 
        className="google-login-btn"
        disabled={loading}
      >
        {loading ? (
          <span>Đang chuyển hướng...</span>
        ) : (
          <>
            <img 
              src="https://www.google.com/favicon.ico" 
              alt="Google" 
              width="20" 
              height="20"
            />
            <span>Đăng nhập với Google</span>
          </>
        )}
      </button>
      
      {error && (
        <div className="error-message">{error}</div>
      )}
    </div>
  );
};

export default GoogleLoginButton;
```

### File 2: `pages/auth/GoogleCallback.tsx`

```typescript
import React, { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';

const GoogleCallback: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
  const [message, setMessage] = useState('Đang xử lý đăng nhập...');

  useEffect(() => {
    const handleGoogleCallback = async () => {
      try {
        // Lấy code từ URL
        const code = searchParams.get('code');
        const error = searchParams.get('error');

        // Kiểm tra nếu có error từ Google
        if (error) {
          setStatus('error');
          setMessage(`Google login failed: ${error}`);
          console.error('Google OAuth error:', error);
          
          // Redirect về login page sau 3 giây
          setTimeout(() => navigate('/login'), 3000);
          return;
        }

        // Kiểm tra nếu không có code
        if (!code) {
          setStatus('error');
          setMessage('Không nhận được authorization code từ Google');
          console.error('No code in URL');
          
          setTimeout(() => navigate('/login'), 3000);
          return;
        }

        console.log('📩 Received code from Google:', code.substring(0, 20) + '...');

        // Gửi code lên Backend để exchange
        setMessage('Đang xác thực với server...');
        
        const response = await fetch('http://localhost:8080/api/Authentication/exchange-google', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            code: code,
            // ⚠️ QUAN TRỌNG: redirectUri phải KHỚP với URI đã dùng lúc lấy code
            redirectUri: window.location.origin + '/auth/google/callback'
          })
        });

        // Parse response
        const data = await response.json();

        if (!response.ok) {
          // Xử lý lỗi từ backend
          setStatus('error');
          setMessage(data.message || 'Xác thực thất bại');
          console.error('Exchange failed:', data);
          
          setTimeout(() => navigate('/login'), 3000);
          return;
        }

        if (data.isSuccess) {
          // ✅ Đăng nhập thành công
          setStatus('success');
          setMessage('Đăng nhập thành công! Đang chuyển hướng...');
          
          console.log('✅ Login successful!');
          console.log('User:', data.data.user);
          console.log('Token:', data.data.token.substring(0, 20) + '...');

          // Lưu token vào localStorage
          localStorage.setItem('access_token', data.data.token);
          localStorage.setItem('refresh_token', data.data.refreshToken);
          
          // Lưu user info (optional)
          localStorage.setItem('user', JSON.stringify(data.data.user));

          // Redirect đến dashboard sau 1 giây
          setTimeout(() => {
            navigate('/dashboard');
          }, 1000);
        } else {
          setStatus('error');
          setMessage(data.message || 'Đăng nhập thất bại');
          setTimeout(() => navigate('/login'), 3000);
        }
      } catch (error) {
        console.error('Network error:', error);
        setStatus('error');
        setMessage('Lỗi kết nối. Vui lòng thử lại.');
        setTimeout(() => navigate('/login'), 3000);
      }
    };

    handleGoogleCallback();
  }, [searchParams, navigate]);

  return (
    <div className="google-callback-container">
      <div className="callback-card">
        {status === 'loading' && (
          <>
            <div className="spinner"></div>
            <h2>{message}</h2>
          </>
        )}
        
        {status === 'success' && (
          <>
            <div className="success-icon">✓</div>
            <h2>{message}</h2>
          </>
        )}
        
        {status === 'error' && (
          <>
            <div className="error-icon">✗</div>
            <h2>Đăng nhập thất bại</h2>
            <p>{message}</p>
            <button onClick={() => navigate('/login')}>Thử lại</button>
          </>
        )}
      </div>
    </div>
  );
};

export default GoogleCallback;
```

### File 3: `App.tsx` - Setup Routes

```typescript
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import GoogleLoginButton from './components/GoogleLoginButton';
import GoogleCallback from './pages/auth/GoogleCallback';
import Dashboard from './pages/Dashboard';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<GoogleLoginButton />} />
        <Route path="/auth/google/callback" element={<GoogleCallback />} />
        <Route path="/dashboard" element={<Dashboard />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
```

### File 4: `GoogleLoginButton.css`

```css
.google-login-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
}

.google-login-btn {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 24px;
  background: white;
  border: 1px solid #dadce0;
  border-radius: 4px;
  font-size: 14px;
  font-weight: 500;
  color: #3c4043;
  cursor: pointer;
  transition: all 0.3s;
}

.google-login-btn:hover {
  box-shadow: 0 1px 3px rgba(0,0,0,0.12), 0 1px 2px rgba(0,0,0,0.24);
}

.google-login-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.error-message {
  color: #d93025;
  font-size: 14px;
  margin-top: 8px;
}

.google-callback-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background: #f5f5f5;
}

.callback-card {
  background: white;
  padding: 40px;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.1);
  text-align: center;
  max-width: 400px;
}

.spinner {
  width: 50px;
  height: 50px;
  border: 4px solid #f3f3f3;
  border-top: 4px solid #4285f4;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 20px;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.success-icon, .error-icon {
  font-size: 60px;
  margin-bottom: 20px;
}

.success-icon {
  color: #34a853;
}

.error-icon {
  color: #ea4335;
}
```

---

## 📱 Vue 3 Implementation

### File 1: `components/GoogleLoginButton.vue`

```vue
<template>
  <div class="google-login-container">
    <button 
      @click="handleGoogleLogin" 
      class="google-login-btn"
      :disabled="loading"
    >
      <span v-if="loading">Đang chuyển hướng...</span>
      <template v-else>
        <img 
          src="https://www.google.com/favicon.ico" 
          alt="Google" 
          width="20" 
          height="20"
        />
        <span>Đăng nhập với Google</span>
      </template>
    </button>
    
    <div v-if="error" class="error-message">{{ error }}</div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const loading = ref(false);
const error = ref<string | null>(null);

const handleGoogleLogin = async () => {
  try {
    loading.value = true;
    error.value = null;

    const response = await fetch('http://localhost:8080/api/Authentication/google-login-url');
    const data = await response.json();

    if (data.isSuccess) {
      console.log('Redirecting to Google:', data.data.url);
      window.location.href = data.data.url;
    } else {
      error.value = data.message || 'Không thể lấy Google login URL';
      loading.value = false;
    }
  } catch (err) {
    console.error('Error getting Google login URL:', err);
    error.value = 'Lỗi kết nối. Vui lòng thử lại.';
    loading.value = false;
  }
};
</script>
```

### File 2: `pages/GoogleCallback.vue`

```vue
<template>
  <div class="google-callback-container">
    <div class="callback-card">
      <div v-if="status === 'loading'">
        <div class="spinner"></div>
        <h2>{{ message }}</h2>
      </div>
      
      <div v-if="status === 'success'">
        <div class="success-icon">✓</div>
        <h2>{{ message }}</h2>
      </div>
      
      <div v-if="status === 'error'">
        <div class="error-icon">✗</div>
        <h2>Đăng nhập thất bại</h2>
        <p>{{ message }}</p>
        <button @click="$router.push('/login')">Thử lại</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';

const router = useRouter();
const route = useRoute();

const status = ref<'loading' | 'success' | 'error'>('loading');
const message = ref('Đang xử lý đăng nhập...');

onMounted(async () => {
  try {
    const code = route.query.code as string;
    const error = route.query.error as string;

    if (error) {
      status.value = 'error';
      message.value = `Google login failed: ${error}`;
      setTimeout(() => router.push('/login'), 3000);
      return;
    }

    if (!code) {
      status.value = 'error';
      message.value = 'Không nhận được authorization code từ Google';
      setTimeout(() => router.push('/login'), 3000);
      return;
    }

    console.log('📩 Received code from Google');

    message.value = 'Đang xác thực với server...';
    
    const response = await fetch('http://localhost:8080/api/Authentication/exchange-google', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        code: code,
        redirectUri: window.location.origin + '/auth/google/callback'
      })
    });

    const data = await response.json();

    if (!response.ok || !data.isSuccess) {
      status.value = 'error';
      message.value = data.message || 'Xác thực thất bại';
      setTimeout(() => router.push('/login'), 3000);
      return;
    }

    status.value = 'success';
    message.value = 'Đăng nhập thành công! Đang chuyển hướng...';
    
    localStorage.setItem('access_token', data.data.token);
    localStorage.setItem('refresh_token', data.data.refreshToken);
    localStorage.setItem('user', JSON.stringify(data.data.user));

    setTimeout(() => router.push('/dashboard'), 1000);
  } catch (error) {
    console.error('Network error:', error);
    status.value = 'error';
    message.value = 'Lỗi kết nối. Vui lòng thử lại.';
    setTimeout(() => router.push('/login'), 3000);
  }
});
</script>
```

---

## 🌐 Vanilla JavaScript (Không dùng framework)

### File: `login.html`

```html
<!DOCTYPE html>
<html lang="vi">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Login with Google</title>
  <style>
    .google-login-btn {
      display: inline-flex;
      align-items: center;
      gap: 12px;
      padding: 12px 24px;
      background: white;
      border: 1px solid #dadce0;
      border-radius: 4px;
      font-size: 14px;
      cursor: pointer;
    }
  </style>
</head>
<body>
  <h1>Đăng nhập</h1>
  
  <button id="googleLoginBtn" class="google-login-btn">
    <img src="https://www.google.com/favicon.ico" width="20" height="20">
    <span>Đăng nhập với Google</span>
  </button>
  
  <div id="error" style="color: red; margin-top: 10px;"></div>

  <script>
    document.getElementById('googleLoginBtn').addEventListener('click', async () => {
      try {
        // Gọi API để lấy Google OAuth URL
        const response = await fetch('http://localhost:8080/api/Authentication/google-login-url');
        const data = await response.json();
        
        if (data.isSuccess) {
          // Redirect đến Google
          window.location.href = data.data.url;
        } else {
          document.getElementById('error').textContent = data.message || 'Lỗi';
        }
      } catch (error) {
        console.error('Error:', error);
        document.getElementById('error').textContent = 'Lỗi kết nối';
      }
    });
  </script>
</body>
</html>
```

### File: `callback.html`

```html
<!DOCTYPE html>
<html lang="vi">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Google Login Callback</title>
  <style>
    body {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      font-family: Arial, sans-serif;
    }
    .container {
      text-align: center;
      padding: 40px;
      background: white;
      border-radius: 8px;
      box-shadow: 0 2px 10px rgba(0,0,0,0.1);
    }
    .spinner {
      width: 50px;
      height: 50px;
      border: 4px solid #f3f3f3;
      border-top: 4px solid #4285f4;
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin: 0 auto 20px;
    }
    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }
  </style>
</head>
<body>
  <div class="container">
    <div class="spinner"></div>
    <h2 id="message">Đang xử lý đăng nhập...</h2>
  </div>

  <script>
    (async () => {
      try {
        // Lấy code từ URL
        const urlParams = new URLSearchParams(window.location.search);
        const code = urlParams.get('code');
        const error = urlParams.get('error');

        if (error) {
          document.getElementById('message').textContent = 'Đăng nhập thất bại: ' + error;
          setTimeout(() => window.location.href = '/login.html', 3000);
          return;
        }

        if (!code) {
          document.getElementById('message').textContent = 'Không nhận được code từ Google';
          setTimeout(() => window.location.href = '/login.html', 3000);
          return;
        }

        console.log('📩 Received code:', code.substring(0, 20) + '...');

        // Gửi code lên backend
        document.getElementById('message').textContent = 'Đang xác thực...';
        
        const response = await fetch('http://localhost:8080/api/Authentication/exchange-google', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            code: code,
            redirectUri: window.location.origin + '/callback.html'
          })
        });

        const data = await response.json();

        if (!response.ok || !data.isSuccess) {
          document.getElementById('message').textContent = 'Xác thực thất bại: ' + (data.message || 'Unknown error');
          setTimeout(() => window.location.href = '/login.html', 3000);
          return;
        }

        // Lưu token
        localStorage.setItem('access_token', data.data.token);
        localStorage.setItem('refresh_token', data.data.refreshToken);
        localStorage.setItem('user', JSON.stringify(data.data.user));

        console.log('✅ Login successful!');
        console.log('User:', data.data.user);

        document.getElementById('message').textContent = 'Đăng nhập thành công!';
        
        // Redirect đến dashboard
        setTimeout(() => {
          window.location.href = '/dashboard.html';
        }, 1000);
      } catch (error) {
        console.error('Error:', error);
        document.getElementById('message').textContent = 'Lỗi: ' + error.message;
        setTimeout(() => window.location.href = '/login.html', 3000);
      }
    })();
  </script>
</body>
</html>
```

---

## 🔍 Debug Guide

### 1. Check console logs

Trong callback page, mở Console (F12) và kiểm tra:

```javascript
// Xem code nhận được từ Google
console.log('Code:', code);

// Xem request body gửi lên backend
console.log('Request body:', JSON.stringify({code, redirectUri}));

// Xem response từ backend
console.log('Response:', data);
```

### 2. Verify redirect URI

```javascript
// Phải khớp chính xác
const redirectUri = window.location.origin + '/auth/google/callback';
console.log('Redirect URI:', redirectUri);
// Output: http://localhost:3000/auth/google/callback
```

### 3. Check network tab

1. Mở DevTools → Network tab
2. Tìm request `exchange-google`
3. Xem Request Payload và Response

---

## ⚠️ Common Issues

### Issue 1: "redirect_uri_mismatch"

**Nguyên nhân:** URI trong request không khớp với Google Console

**Fix:**
```javascript
// ❌ SAI
redirectUri: 'http://localhost:3000/callback'

// ✅ ĐÚNG (phải khớp với Google Console)
redirectUri: 'http://localhost:3000/auth/google/callback'
```

### Issue 2: "invalid_grant"

**Nguyên nhân:** Code đã được sử dụng hoặc hết hạn

**Fix:** Lấy code mới (login lại từ đầu)

### Issue 3: CORS error

**Nguyên nhân:** Backend chưa enable CORS

**Fix:** Thêm vào `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

app.UseCors();
```

---

## 📚 API Reference

### API 1: Get Google Login URL

```http
GET http://localhost:8080/api/Authentication/google-login-url
```

Response:
```json
{
  "isSuccess": true,
  "data": {
    "url": "https://accounts.google.com/o/oauth2/v2/auth?client_id=...",
    "redirectUri": "http://localhost:3000/auth/google/callback"
  }
}
```

### API 2: Exchange Code

```http
POST http://localhost:8080/api/Authentication/exchange-google
Content-Type: application/json

{
  "code": "4/0Ab32j93YhSpE...",
  "redirectUri": "http://localhost:3000/auth/google/callback"
}
```

Response:
```json
{
  "isSuccess": true,
  "message": "Đăng nhập Google thành công",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "abc123...",
    "user": {
      "email": "user@gmail.com",
      "name": "John Doe",
      "picture": "https://...",
      "roles": ["Customer"]
    }
  }
}
```

---

**Created:** 2025-11-23

