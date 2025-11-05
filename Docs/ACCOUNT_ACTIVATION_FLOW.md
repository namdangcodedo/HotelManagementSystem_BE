

# ACCOUNT ACTIVATION FLOW - FRONTEND INTEGRATION GUIDE

## 📋 Tổng quan

Hệ thống kích hoạt tài khoản với các tính năng:
- ✅ Đăng ký → Gửi email kích hoạt tự động
- ✅ Link kích hoạt có hiệu lực **5 phút**
- ✅ **Auto-login** sau khi kích hoạt (trả về token)
- ✅ Gửi lại email nếu hết hạn hoặc không nhận được
- ✅ Token mã hóa 2 chiều bảo mật

---

## 🔄 Luồng hoạt động đầy đủ

```
┌─────────────────────────────────────────────────────────────────┐
│                    ACCOUNT ACTIVATION FLOW                      │
└─────────────────────────────────────────────────────────────────┘

1. USER ĐĂNG KÝ
   │
   ├─► Frontend: POST /api/Authentication/register
   │   {
   │     "username": "user123",
   │     "email": "user@example.com",
   │     "password": "Pass@123",
   │     "fullName": "Nguyen Van A",
   │     "phoneNumber": "0987654321"
   │   }
   │
   ├─► Backend: 
   │   • Tạo account với IsLocked = true
   │   • Tạo customer record
   │   • Lưu token vào cache (5 phút)
   │   • Gửi email kích hoạt
   │
   └─► Response:
       {
         "isSuccess": true,
         "message": "Đăng ký thành công! Vui lòng kiểm tra email...",
         "data": {
           "accountId": 123,
           "email": "user@example.com"
         }
       }

2. FRONTEND XỬ LÝ SAU ĐĂNG KÝ
   │
   ├─► Hiển thị thông báo:
   │   "✅ Đăng ký thành công!"
   │   "📧 Vui lòng kiểm tra email để kích hoạt tài khoản"
   │   "⏰ Link có hiệu lực trong 5 phút"
   │
   ├─► Hiển thị nút: "Chưa nhận được email? Gửi lại"
   │
   └─► Redirect về trang: /check-email hoặc /activation-pending

3. USER MỞ EMAIL
   │
   ├─► Email chứa link:
   │   http://localhost:3000/activate-account/{TOKEN}
   │   
   │   Token ví dụ: 
   │   "abc123def456ghi789..."
   │
   └─► User click link

4. FRONTEND XỬ LÝ ACTIVATION
   │
   ├─► Route: /activate-account/:token
   │
   ├─► Parse token từ URL params
   │
   ├─► Gọi API:
   │   GET /api/Authentication/activate-account/{token}
   │
   ├─► Response Success:
   │   {
   │     "isSuccess": true,
   │     "message": "Kích hoạt thành công! Đang tự động đăng nhập...",
   │     "data": {
   │       "email": "user@example.com",
   │       "username": "user123",
   │       "token": "eyJhbGciOiJIUzI1NiIs...",      ← Access Token
   │       "refreshToken": "xyz789abc123...",        ← Refresh Token
   │       "roles": ["User"]
   │     }
   │   }
   │
   ├─► Frontend lưu token:
   │   localStorage.setItem('accessToken', data.token)
   │   localStorage.setItem('refreshToken', data.refreshToken)
   │   localStorage.setItem('userRoles', JSON.stringify(data.roles))
   │
   ├─► Hiển thị: "✅ Kích hoạt thành công! Đang chuyển hướng..."
   │
   └─► Redirect về: /dashboard hoặc /home (đã đăng nhập)

5. XỬ LÝ LỖI - LINK HẾT HẠN
   │
   ├─► Response Error:
   │   {
   │     "isSuccess": false,
   │     "message": "Link kích hoạt đã hết hạn (quá 5 phút)...",
   │     "statusCode": 400
   │   }
   │
   ├─► Frontend hiển thị:
   │   "⚠️ Link kích hoạt đã hết hạn"
   │   "Nhập email để nhận link mới"
   │   
   │   [Input: Email]
   │   [Button: Gửi lại email kích hoạt]
   │
   └─► User nhập email → Chuyển đến BƯỚC 6

6. GỬI LẠI EMAIL KÍCH HOẠT
   │
   ├─► Frontend: POST /api/Authentication/resend-activation-email
   │   {
   │     "email": "user@example.com"
   │   }
   │
   ├─► Response Success:
   │   {
   │     "isSuccess": true,
   │     "message": "Email kích hoạt đã được gửi lại!...",
   │     "data": {
   │       "email": "user@example.com",
   │       "message": "Link kích hoạt mới có hiệu lực trong 5 phút"
   │     }
   │   }
   │
   ├─► Frontend hiển thị:
   │   "✅ Email đã được gửi lại!"
   │   "📧 Vui lòng check email và kích hoạt trong 5 phút"
   │
   └─► Quay lại BƯỚC 3 (User mở email mới)
```

---

## 💻 Code mẫu cho Frontend

### 1. Component Đăng ký (Register.tsx/jsx)

```typescript
const Register = () => {
  const [formData, setFormData] = useState({
    username: '',
    email: '',
    password: '',
    fullName: '',
    phoneNumber: '',
    identityCard: '',
    address: ''
  });

  const handleRegister = async (e) => {
    e.preventDefault();
    
    try {
      const response = await fetch('http://localhost:8080/api/Authentication/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(formData)
      });
      
      const result = await response.json();
      
      if (result.isSuccess) {
        // Hiển thị thông báo thành công
        toast.success('Đăng ký thành công! Vui lòng check email để kích hoạt tài khoản.');
        
        // Redirect về trang check email
        navigate('/check-email', { 
          state: { email: formData.email } 
        });
      } else {
        toast.error(result.message);
      }
    } catch (error) {
      toast.error('Lỗi đăng ký. Vui lòng thử lại!');
    }
  };

  return (
    <form onSubmit={handleRegister}>
      {/* Form fields... */}
      <button type="submit">Đăng ký</button>
    </form>
  );
};
```

---

### 2. Component Check Email (CheckEmail.tsx/jsx)

```typescript
const CheckEmail = () => {
  const location = useLocation();
  const email = location.state?.email;

  const handleResendEmail = async () => {
    try {
      const response = await fetch('http://localhost:8080/api/Authentication/resend-activation-email', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email })
      });
      
      const result = await response.json();
      
      if (result.isSuccess) {
        toast.success('Email đã được gửi lại! Vui lòng check hộp thư.');
      } else {
        toast.error(result.message);
      }
    } catch (error) {
      toast.error('Không thể gửi email. Vui lòng thử lại!');
    }
  };

  return (
    <div className="check-email-container">
      <h2>📧 Kiểm tra Email của bạn</h2>
      <p>Chúng tôi đã gửi link kích hoạt đến:</p>
      <strong>{email}</strong>
      
      <div className="info-box">
        <p>⏰ Link có hiệu lực trong <strong>5 phút</strong></p>
        <p>📨 Vui lòng check cả mục Spam/Junk</p>
      </div>

      <button onClick={handleResendEmail} className="btn-secondary">
        Chưa nhận được email? Gửi lại
      </button>
    </div>
  );
};
```

---

### 3. Component Kích hoạt tài khoản (ActivateAccount.tsx/jsx)

```typescript
const ActivateAccount = () => {
  const { token } = useParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState('loading'); // loading | success | error
  const [message, setMessage] = useState('');
  const [email, setEmail] = useState('');

  useEffect(() => {
    activateAccount();
  }, [token]);

  const activateAccount = async () => {
    try {
      const response = await fetch(
        `http://localhost:8080/api/Authentication/activate-account/${token}`
      );
      
      const result = await response.json();
      
      if (result.isSuccess) {
        // ✅ LƯU TOKEN VÀO LOCALSTORAGE
        localStorage.setItem('accessToken', result.data.token);
        localStorage.setItem('refreshToken', result.data.refreshToken);
        localStorage.setItem('userEmail', result.data.email);
        localStorage.setItem('userRoles', JSON.stringify(result.data.roles));
        
        setStatus('success');
        setMessage(result.message);
        
        // ✅ AUTO REDIRECT SAU 2 GIÂY
        setTimeout(() => {
          navigate('/dashboard'); // hoặc '/home'
        }, 2000);
        
      } else {
        setStatus('error');
        setMessage(result.message);
      }
    } catch (error) {
      setStatus('error');
      setMessage('Có lỗi xảy ra. Vui lòng thử lại!');
    }
  };

  const handleResendEmail = async () => {
    if (!email) {
      toast.error('Vui lòng nhập email');
      return;
    }

    try {
      const response = await fetch(
        'http://localhost:8080/api/Authentication/resend-activation-email',
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ email })
        }
      );
      
      const result = await response.json();
      
      if (result.isSuccess) {
        toast.success('Email mới đã được gửi! Vui lòng check hộp thư.');
      } else {
        toast.error(result.message);
      }
    } catch (error) {
      toast.error('Không thể gửi email!');
    }
  };

  return (
    <div className="activate-container">
      {status === 'loading' && (
        <div className="loading">
          <Spinner />
          <p>Đang kích hoạt tài khoản...</p>
        </div>
      )}

      {status === 'success' && (
        <div className="success">
          <CheckCircleIcon className="icon-success" />
          <h2>✅ Kích hoạt thành công!</h2>
          <p>{message}</p>
          <p>Đang tự động đăng nhập và chuyển hướng...</p>
        </div>
      )}

      {status === 'error' && (
        <div className="error">
          <ErrorIcon className="icon-error" />
          <h2>⚠️ Kích hoạt thất bại</h2>
          <p>{message}</p>
          
          {message.includes('hết hạn') && (
            <div className="resend-form">
              <p>Nhập email để nhận link kích hoạt mới:</p>
              <input
                type="email"
                placeholder="Email của bạn"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
              <button onClick={handleResendEmail}>
                Gửi lại email
              </button>
            </div>
          )}
          
          <button onClick={() => navigate('/login')}>
            Về trang đăng nhập
          </button>
        </div>
      )}
    </div>
  );
};
```

---

### 4. Routing Setup (App.tsx/jsx hoặc routes.js)

```typescript
import { BrowserRouter, Routes, Route } from 'react-router-dom';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* ... other routes */}
        
        <Route path="/register" element={<Register />} />
        <Route path="/check-email" element={<CheckEmail />} />
        <Route path="/activate-account/:token" element={<ActivateAccount />} />
        
        {/* Protected routes */}
        <Route path="/dashboard" element={
          <ProtectedRoute>
            <Dashboard />
          </ProtectedRoute>
        } />
      </Routes>
    </BrowserRouter>
  );
}
```

---

## 🎯 Các trường hợp cần xử lý

### ✅ Case 1: Kích hoạt thành công
```json
Response: 200 OK
{
  "isSuccess": true,
  "message": "Kích hoạt tài khoản thành công! Đang tự động đăng nhập...",
  "data": {
    "email": "user@example.com",
    "username": "user123",
    "token": "eyJhbGci...",
    "refreshToken": "xyz789...",
    "roles": ["User"]
  }
}
```
**Action**: Lưu token + redirect về dashboard

---

### ❌ Case 2: Link hết hạn (> 5 phút)
```json
Response: 400 Bad Request
{
  "isSuccess": false,
  "message": "Link kích hoạt đã hết hạn (quá 5 phút). Vui lòng gửi lại email kích hoạt.",
  "statusCode": 400
}
```
**Action**: Hiển thị form nhập email → Gọi API resend

---

### ❌ Case 3: Token không hợp lệ
```json
Response: 400 Bad Request
{
  "isSuccess": false,
  "message": "Token không hợp lệ: ...",
  "statusCode": 400
}
```
**Action**: Hiển thị lỗi + nút về trang đăng ký

---

### ❌ Case 4: Tài khoản đã kích hoạt
```json
Response: 400 Bad Request
{
  "isSuccess": false,
  "message": "Tài khoản đã được kích hoạt trước đó. Bạn có thể đăng nhập ngay.",
  "statusCode": 400
}
```
**Action**: Redirect về trang login

---

## 🔒 Security Notes

1. **Token Security**:
   - Token được mã hóa AES 256-bit
   - URL-safe encoding
   - Chỉ có hiệu lực 5 phút

2. **Auto-login Security**:
   - Access token có expiry time
   - Refresh token được lưu trong cache server
   - Validate roles trước khi truy cập protected routes

3. **Best Practices**:
   - Lưu token trong localStorage (hoặc httpOnly cookie nếu có)
   - Clear token khi logout
   - Refresh token khi hết hạn
   - Validate token trước mỗi API call

---

## 📝 Checklist cho Frontend Developer

- [ ] Trang đăng ký có form đầy đủ fields
- [ ] Hiển thị thông báo sau đăng ký thành công
- [ ] Trang "Check Email" với nút gửi lại
- [ ] Route `/activate-account/:token` hoạt động
- [ ] Parse token từ URL params
- [ ] Gọi API activate và lưu token
- [ ] Auto-redirect sau kích hoạt thành công
- [ ] Xử lý trường hợp link hết hạn
- [ ] Form gửi lại email kích hoạt
- [ ] Loading state khi đang kích hoạt
- [ ] Error handling đầy đủ
- [ ] UI/UX thân thiện với người dùng

---

## 🎨 UI/UX Recommendations

### Màn hình "Check Email"
```
┌────────────────────────────────────┐
│   📧 Kiểm tra Email của bạn        │
│                                    │
│   Chúng tôi đã gửi link kích hoạt  │
│   đến: user@example.com            │
│                                    │
│   ┌──────────────────────────┐    │
│   │ ⏰ Link có hiệu lực 5 phút│    │
│   │ 📨 Check cả Spam/Junk    │    │
│   └──────────────────────────┘    │
│                                    │
│   [Chưa nhận được? Gửi lại]       │
└────────────────────────────────────┘
```

### Màn hình "Activating"
```
┌────────────────────────────────────┐
│         ⌛ Loading...               │
│                                    │
│   Đang kích hoạt tài khoản...      │
│                                    │
│   [Spinner Animation]              │
└────────────────────────────────────┘
```

### Màn hình "Success"
```
┌────────────────────────────────────┐
│          ✅ Thành công!            │
│                                    │
│   Kích hoạt tài khoản thành công!  │
│   Đang tự động đăng nhập...        │
│                                    │
│   Chuyển hướng trong 2 giây...     │
└────────────────────────────────────┘
```

### Màn hình "Link Expired"
```
┌────────────────────────────────────┐
│       ⚠️ Link đã hết hạn           │
│                                    │
│   Link kích hoạt chỉ có hiệu lực   │
│   trong 5 phút.                    │
│                                    │
│   Nhập email để nhận link mới:     │
│   [___________________]            │
│                                    │
│   [Gửi lại email kích hoạt]       │
└────────────────────────────────────┘
```

---

## 🚀 Testing Checklist

- [ ] Đăng ký tài khoản mới → Nhận email
- [ ] Click link trong email → Kích hoạt thành công
- [ ] Token được lưu vào localStorage
- [ ] Auto-redirect về dashboard
- [ ] Đăng nhập bình thường sau khi kích hoạt
- [ ] Test link hết hạn (đợi 5 phút)
- [ ] Gửi lại email kích hoạt → Nhận email mới
- [ ] Click link mới → Kích hoạt thành công
- [ ] Test với token không hợp lệ
- [ ] Test với email không tồn tại

---

## 📞 API Endpoints Summary

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/Authentication/register` | ❌ | Đăng ký tài khoản |
| GET | `/api/Authentication/activate-account/{token}` | ❌ | Kích hoạt tài khoản |
| POST | `/api/Authentication/resend-activation-email` | ❌ | Gửi lại email |
| POST | `/api/Authentication/login` | ❌ | Đăng nhập |

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-15  
**Author**: Backend Team

