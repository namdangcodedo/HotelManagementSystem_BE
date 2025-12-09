# Comment API Documentation

## Tổng quan
API quản lý bình luận cho hệ thống khách sạn, bao gồm các chức năng:
- Lấy danh sách bình luận theo loại phòng
- Thêm bình luận mới (bao gồm cả reply)
- Cập nhật bình luận
- Ẩn bình luận (hiển thị nút action khi đang loin role Receptionist, Manager, Admin)

**Base URL**: `/api/Comment`

---

## 1. Lấy danh sách bình luận

### Endpoint
```
GET /api/Comment
```

### Mô tả
Lấy danh sách bình luận theo RoomTypeId hoặc theo ParentCommentId (để lấy các reply).

### Authentication
Không yêu cầu đăng nhập (Public)

### Query Parameters

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---------|------|----------|-------|
| `RoomTypeId` | `int` | Có* | ID của loại phòng cần lấy bình luận |
| `ParentCommentId` | `int` | Có* | ID của comment cha (để lấy các reply) |
| `IncludeReplies` | `boolean` | Không | Có bao gồm các reply hay không (default: `true`) |
| `MaxReplyDepth` | `int` | Không | Độ sâu tối đa của reply tree (default: `3`) |
| `PageIndex` | `int` | Không | Số trang (default: `1`) |
| `PageSize` | `int` | Không | Số lượng item mỗi trang (default: `10`) |
| `IsNewest` | `boolean` | Không | Sắp xếp mới nhất trước (default: `true`) |

*Lưu ý: Phải có ít nhất một trong hai: `RoomTypeId` hoặc `ParentCommentId`

### Request Example

**Lấy comment của một loại phòng:**
```http
GET /api/Comment?RoomTypeId=1&PageIndex=1&PageSize=10&IncludeReplies=true&IsNewest=true
```

**Lấy các reply của một comment:**
```http
GET /api/Comment?ParentCommentId=5&PageIndex=1&PageSize=5
```

### Response Success (200 OK)

```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "message": "Lấy danh sách bình luận thành công",
  "data": {
    "comments": [
      {
        "commentId": 1,
        "roomTypeId": 1,
        "replyId": null,
        "accountId": 10,
        "content": "Phòng rất đẹp và sạch sẽ!",
        "rating": 5,
        "createdDate": "2024-12-01T00:00:00Z",
        "createdTime": "2024-12-01T10:30:00Z",
        "updatedAt": "2024-12-01T10:30:00Z",
        "status": "Approved",
        "userFullName": "Nguyễn Văn A",
        "userEmail": "nguyenvana@example.com",
        "userType": "Customer",
        "replies": [
          {
            "commentId": 2,
            "roomTypeId": 1,
            "replyId": 1,
            "accountId": 15,
            "content": "Cảm ơn bạn đã đánh giá!",
            "rating": null,
            "createdDate": "2024-12-01T00:00:00Z",
            "createdTime": "2024-12-01T11:00:00Z",
            "updatedAt": "2024-12-01T11:00:00Z",
            "status": "Approved",
            "userFullName": "Trần Thị B",
            "userEmail": "manager@hotel.com",
            "userType": "Employee",
            "replies": []
          }
        ]
      }
    ],
    "totalCount": 25,
    "pageIndex": 1,
    "pageSize": 10,
    "totalPages": 3
  },
  "statusCode": 200
}
```

### Response Error (400 Bad Request)

```json
{
  "isSuccess": false,
  "responseCode": "BAD_REQUEST",
  "message": "RoomTypeId hoặc ParentCommentId là bắt buộc",
  "data": null,
  "statusCode": 400
}
```

---

## 2. Thêm bình luận mới

### Endpoint
```
POST /api/Comment
```

### Mô tả
Thêm bình luận mới hoặc reply cho một bình luận đã có. User phải đăng nhập.

**🤖 AI Moderation**: Bình luận sẽ được tự động phân tích bằng Gemini AI để kiểm duyệt nội dung:
- ✅ **Approved**: Bình luận được chấp nhận và hiển thị công khai
- ❌ **Rejected**: Bình luận bị từ chối (toxic, spam, phá hoại)
- ⏳ **Pending**: Bình luận cần kiểm duyệt thủ công

**Lưu ý**: Phản hồi tiêu cực mang tính **XÂY DỰNG** vẫn được approve. Chỉ những bình luận thực sự toxic, spam mới bị reject.

### Authentication
**Yêu cầu đăng nhập** - Gửi JWT token trong header

```
Authorization: Bearer {access_token}
```

### Request Headers

| Header | Giá trị |
|--------|---------|
| `Content-Type` | `application/json` |
| `Authorization` | `Bearer {access_token}` |

### Request Body

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---------|------|----------|-------|
| `roomTypeId` | `int` | Có | ID của loại phòng |
| `replyId` | `int` | Không | ID của comment cha (nếu là reply) |
| `content` | `string` | Có | Nội dung bình luận |
| `rating` | `int` | Không | Đánh giá từ 1-5 sao (chỉ cho comment gốc, không dùng cho reply) |

### Request Example

**Thêm comment mới cho phòng:**
```json
{
  "roomTypeId": 1,
  "content": "Phòng rất tuyệt vời, dịch vụ tốt!",
  "rating": 5
}
```

**Thêm reply cho một comment:**
```json
{
  "roomTypeId": 1,
  "replyId": 10,
  "content": "Cảm ơn bạn đã phản hồi!"
}
```

### Response Success (201 Created) - Approved

**Comment được chấp nhận:**
```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "message": "Thêm bình luận thành công",
  "data": {
    "commentId": 25,
    "status": "Approved",
    "reason": "Bình luận phù hợp",
    "toxicityScore": 0.1
  },
  "statusCode": 201
}
```

### Response Success (201 Created) - Pending

**Comment cần kiểm duyệt:**
```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "message": "Bình luận đang chờ kiểm duyệt",
  "data": {
    "commentId": 26,
    "status": "Pending",
    "reason": "Cần xem xét thêm",
    "toxicityScore": 0.5
  },
  "statusCode": 201
}
```

### Response Error (403 Forbidden) - Rejected

**Comment bị từ chối do nội dung không phù hợp:**
```json
{
  "isSuccess": false,
  "responseCode": "REJECTED",
  "message": "Bình luận bị từ chối: Nội dung chứa ngôn từ không phù hợp",
  "data": {
    "commentId": 27,
    "status": "Rejected",
    "reason": "Nội dung chứa ngôn từ không phù hợp",
    "toxicityScore": 0.9
  },
  "statusCode": 403
}
```

*Lưu ý: Comment bị rejected vẫn được lưu vào DB nhưng có status="Rejected" và không hiển thị công khai*

---

## 3. Cập nhật bình luận

### Endpoint
```
PUT /api/Comment
```

### Mô tả
Cập nhật nội dung và rating của bình luận. Chỉ chủ sở hữu bình luận mới có quyền cập nhật.

### Authentication
**Yêu cầu đăng nhập** - Gửi JWT token trong header

```
Authorization: Bearer {access_token}
```

### Request Headers

| Header | Giá trị |
|--------|---------|
| `Content-Type` | `application/json` |
| `Authorization` | `Bearer {access_token}` |

### Request Body

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---------|------|----------|-------|
| `commentId` | `int` | Có | ID của bình luận cần cập nhật |
| `content` | `string` | Có | Nội dung bình luận mới |
| `rating` | `int` | Không | Đánh giá mới từ 1-5 sao |

### Request Example

```json
{
  "commentId": 25,
  "content": "Phòng rất tuyệt vời, dịch vụ xuất sắc! (đã chỉnh sửa)",
  "rating": 5
}
```

### Response Success (200 OK)

```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "message": "Cập nhật bình luận thành công",
  "data": null,
  "statusCode": 200
}
```

### Response Error (401 Unauthorized)

```json
{
  "message": "Không thể xác thực người dùng"
}
```

### Response Error (403 Forbidden)

```json
{
  "isSuccess": false,
  "responseCode": "UNAUTHORIZED",
  "message": "Bạn không có quyền chỉnh sửa bình luận này",
  "data": null,
  "statusCode": 403
}
```

### Response Error (404 Not Found)

```json
{
  "isSuccess": false,
  "responseCode": "NOT_FOUND",
  "message": "Không tìm thấy bình luận",
  "data": null,
  "statusCode": 404
}
```

---

## 4. Ẩn bình luận

### Endpoint
```
PATCH /api/Comment/{commentId}/hide
```

### Mô tả
Ẩn một bình luận. Chỉ dành cho staff (Receptionist, Manager, Admin).

### Authentication
**Yêu cầu role:** `Receptionist`, `Manager`, hoặc `Admin`

```
Authorization: Bearer {access_token}
```

### Request Headers

| Header | Giá trị |
|--------|---------|
| `Authorization` | `Bearer {access_token}` |

### Path Parameters

| Tham số | Kiểu | Mô tả |
|---------|------|-------|
| `commentId` | `int` | ID của bình luận cần ẩn |

### Request Example

```http
PATCH /api/Comment/15/hide
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Response Success (200 OK)

```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "message": "Ẩn bình luận thành công",
  "data": null,
  "statusCode": 200
}
```

### Response Error (401 Unauthorized)

```json
{
  "message": "Unauthorized"
}
```

### Response Error (403 Forbidden)

```json
{
  "message": "Forbidden - You don't have permission to access this resource"
}
```

### Response Error (404 Not Found)

```json
{
  "isSuccess": false,
  "responseCode": "NOT_FOUND",
  "message": "Không tìm thấy bình luận",
  "data": null,
  "statusCode": 404
}
```

---

## Status Values

Các giá trị có thể có của trường `status`:

| Status | Mô tả |
|--------|-------|
| `Approved` | Đã được duyệt và hiển thị (default) |
| `Pending` | Đang chờ duyệt |
| `Rejected` | Bị từ chối |
| `Hidden` | Đã bị ẩn bởi staff |

---

## Data Models

### CommentDTO

```typescript
interface CommentDTO {
  commentId: number;
  roomTypeId: number | null;
  replyId: number | null;
  accountId: number | null;
  content: string | null;
  rating: number | null;       // 1-5 sao
  createdDate: string;         // ISO 8601 date
  createdTime: string;         // ISO 8601 datetime
  updatedAt: string;           // ISO 8601 datetime
  status: string;              // "Approved", "Pending", "Rejected", "Hidden"
  
  // Thông tin người dùng
  userFullName: string | null; // Tên đầy đủ từ Customer hoặc Employee
  userEmail: string | null;    // Email từ Account
  userType: string | null;     // "Customer" hoặc "Employee"
  
  replies: CommentDTO[];       // Mảng các reply con
}
```

### AccountDTO (Simplified)

```typescript
// Deprecated - Thông tin user giờ được trả về trực tiếp trong CommentDTO
// qua các trường: userFullName, userEmail, userType
```

### AddCommentRequest

```typescript
interface AddCommentRequest {
  roomTypeId: number;
  replyId?: number | null;
  content: string;
  rating?: number | null;  // 1-5
}
```

### UpdateCommentRequest

```typescript
interface UpdateCommentRequest {
  commentId: number;
  content: string;
  rating?: number | null;  // 1-5
}
```

### GetCommentRequest

```typescript
interface GetCommentRequest {
  roomTypeId?: number | null;
  parentCommentId?: number | null;
  includeReplies?: boolean;     // default: true
  maxReplyDepth?: number;       // default: 3
  pageIndex?: number;           // default: 1
  pageSize?: number;            // default: 10
  isNewest?: boolean;           // default: true
}
```

---

## Code Examples

### JavaScript/TypeScript với Axios

#### 1. Lấy danh sách comment

```javascript
async function getComments(roomTypeId, pageIndex = 1, pageSize = 10) {
  try {
    const response = await axios.get('/api/Comment', {
      params: {
        RoomTypeId: roomTypeId,
        PageIndex: pageIndex,
        PageSize: pageSize,
        IncludeReplies: true,
        IsNewest: true
      }
    });
    
    if (response.data.isSuccess) {
      return response.data.data;
    }
  } catch (error) {
    console.error('Error fetching comments:', error);
    throw error;
  }
}
```

#### 2. Thêm comment mới

```javascript
async function addComment(roomTypeId, content, rating = null) {
  try {
    const token = localStorage.getItem('access_token');
    const response = await axios.post('/api/Comment', {
      roomTypeId,
      content,
      rating
    }, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });
    
    if (response.data.isSuccess) {
      return response.data.data; // commentId
    }
  } catch (error) {
    if (error.response?.status === 401) {
      console.error('Bạn cần đăng nhập để bình luận');
    }
    throw error;
  }
}
```

#### 3. Thêm reply

```javascript
async function addReply(roomTypeId, parentCommentId, content) {
  try {
    const token = localStorage.getItem('access_token');
    const response = await axios.post('/api/Comment', {
      roomTypeId,
      replyId: parentCommentId,
      content
    }, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });
    
    if (response.data.isSuccess) {
      return response.data.data; // commentId
    }
  } catch (error) {
    console.error('Error adding reply:', error);
    throw error;
  }
}
```

#### 4. Cập nhật comment

```javascript
async function updateComment(commentId, content, rating = null) {
  try {
    const token = localStorage.getItem('access_token');
    const response = await axios.put('/api/Comment', {
      commentId,
      content,
      rating
    }, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });
    
    return response.data;
  } catch (error) {
    if (error.response?.status === 403) {
      console.error('Bạn không có quyền chỉnh sửa bình luận này');
    }
    throw error;
  }
}
```

#### 5. Ẩn comment (Staff only)

```javascript
async function hideComment(commentId) {
  try {
    const token = localStorage.getItem('access_token');
    const response = await axios.patch(`/api/Comment/${commentId}/hide`, null, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });
    
    return response.data;
  } catch (error) {
    if (error.response?.status === 403) {
      console.error('Bạn không có quyền ẩn bình luận');
    }
    throw error;
  }
}
```

### React Hook Example

```typescript
import { useState, useEffect } from 'react';
import axios from 'axios';

function useComments(roomTypeId: number) {
  const [comments, setComments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [pagination, setPagination] = useState({
    pageIndex: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0
  });

  const fetchComments = async (pageIndex = 1) => {
    try {
      setLoading(true);
      const response = await axios.get('/api/Comment', {
        params: {
          RoomTypeId: roomTypeId,
          PageIndex: pageIndex,
          PageSize: pagination.pageSize,
          IncludeReplies: true,
          IsNewest: true
        }
      });

      if (response.data.isSuccess) {
        setComments(response.data.data.comments);
        setPagination({
          pageIndex: response.data.data.pageIndex,
          pageSize: response.data.data.pageSize,
          totalCount: response.data.data.totalCount,
          totalPages: response.data.data.totalPages
        });
      }
    } catch (err) {
      setError(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchComments();
  }, [roomTypeId]);

  const addComment = async (content: string, rating?: number) => {
    const token = localStorage.getItem('access_token');
    const response = await axios.post('/api/Comment', {
      roomTypeId,
      content,
      rating
    }, {
      headers: { Authorization: `Bearer ${token}` }
    });

    if (response.data.isSuccess) {
      fetchComments(); // Refresh comments
    }
    return response.data;
  };

  const hideComment = async (commentId: number) => {
    const token = localStorage.getItem('access_token');
    const response = await axios.patch(`/api/Comment/${commentId}/hide`, null, {
      headers: { Authorization: `Bearer ${token}` }
    });

    if (response.data.isSuccess) {
      fetchComments(); // Refresh comments
    }
    return response.data;
  };

  return {
    comments,
    loading,
    error,
    pagination,
    fetchComments,
    addComment,
    hideComment
  };
}

export default useComments;
```

---

## Error Handling Best Practices

```javascript
async function handleApiCall(apiFunction) {
  try {
    const response = await apiFunction();
    
    if (!response.isSuccess) {
      // Handle business logic errors
      switch (response.responseCode) {
        case 'NOT_FOUND':
          alert('Không tìm thấy dữ liệu');
          break;
        case 'UNAUTHORIZED':
          alert('Bạn không có quyền thực hiện hành động này');
          break;
        case 'BAD_REQUEST':
          alert(response.message);
          break;
        default:
          alert('Có lỗi xảy ra');
      }
      return null;
    }
    
    return response.data;
  } catch (error) {
    // Handle HTTP errors
    if (error.response) {
      switch (error.response.status) {
        case 401:
          // Redirect to login
          window.location.href = '/login';
          break;
        case 403:
          alert('Bạn không có quyền truy cập');
          break;
        case 404:
          alert('Không tìm thấy');
          break;
        case 500:
          alert('Lỗi server');
          break;
        default:
          alert('Có lỗi xảy ra');
      }
    } else {
      alert('Không thể kết nối đến server');
    }
    return null;
  }
}
```

---

## Notes

1. **Authentication**: Token phải được gửi trong header `Authorization: Bearer {token}` cho các API cần đăng nhập
2. **Date Format**: Tất cả datetime đều sử dụng format ISO 8601 (UTC)
3. **Rating**: Chỉ comment gốc mới có rating, reply không có rating
4. **Status**: Comment mặc định được phân tích bởi AI, có thể nhận status: "Approved", "Rejected", hoặc "Pending"
5. **Pagination**: Default là page 1, size 10. Tối đa 100 items/page
6. **Reply Depth**: Default là 3 levels, có thể điều chỉnh qua `MaxReplyDepth`
7. **Hidden Comments**: Comment bị ẩn vẫn tồn tại trong DB nhưng không hiển thị cho user thông thường
8. **🤖 AI Moderation**: 
   - Sử dụng Google Gemini AI để phân tích nội dung bình luận
   - Tự động phát hiện: toxic language, spam, nội dung phá hoại
   - Phản hồi tiêu cực **mang tính xây dựng** vẫn được approve
   - Rating thấp (1-2 sao) kèm góp ý hợp lý → APPROVED
   - Chỉ reject những bình luận thực sự có vấn đề
   - Nếu AI không chắc chắn → set status "Pending" để admin review

---

## AI Moderation Details

### Tiêu chí đánh giá của AI:

| Loại | Hành động | Ví dụ |
|------|-----------|-------|
| 🟢 **Constructive Feedback** | ✅ APPROVED | "Phòng đẹp nhưng cách âm chưa tốt lắm, hy vọng khách sạn cải thiện" |
| 🟢 **Positive Review** | ✅ APPROVED | "Dịch vụ tuyệt vời, nhân viên thân thiện!" |
| 🟡 **Low Rating + Valid Reason** | ✅ APPROVED | "2 sao vì phòng có mùi hôi và wifi yếu" |
| 🔴 **Toxic Language** | ❌ REJECTED | Ngôn từ thô tục, xúc phạm, đe dọa |
| 🔴 **Spam** | ❌ REJECTED | Quảng cáo, nội dung không liên quan |
| 🔴 **Destructive** | ❌ REJECTED | Phá hoại danh tiếng không có căn cứ |
| 🟡 **Unclear Intent** | ⏳ PENDING | AI không chắc chắn → cần admin review |

### Toxicity Score:
- **0.0 - 0.3**: Nội dung an toàn → Approved
- **0.4 - 0.6**: Cần xem xét → Pending
- **0.7 - 1.0**: Nội dung không phù hợp → Rejected

### Fallback Mechanism:
Nếu Gemini AI không khả dụng:
- Comment tự động được set status "Pending"
- Admin cần kiểm duyệt thủ công
- Hệ thống vẫn hoạt động bình thường

---

## Changelog

- **v1.1.0** (2024-12-10): AI Moderation Update
  - 🤖 Thêm tự động kiểm duyệt bình luận bằng Gemini AI
  - Phát hiện toxic content, spam, nội dung phá hoại
  - Support cho constructive criticism
  - Trả về toxicity score và lý do reject
  
- **v1.0.0** (2024-12-09): Initial release
  - GET comments endpoint
  - POST add comment endpoint  
  - PUT update comment endpoint
  - PATCH hide comment endpoint
