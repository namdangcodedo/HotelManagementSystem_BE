# Room Type API Documentation - Media CRUD System

## 📋 Overview

This document describes the updated Room Type API endpoints with the new Media CRUD (Create, Read, Update, Delete) system. The media system supports smart image management without deleting and re-uploading everything.

---

## 🔄 API Endpoints

### 1. Get All Room Types (Search without pagination)

**Endpoint:**
```
GET /api/rooms/types
```

**Description:**
- Retrieves all room types (no pagination - returns all records)
- Supports optional filtering by `IsActive` status
- Returns all room types with their media

**Request Parameters (Query):**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `isActive` | boolean | No | Filter by active status (true/false). Omit to get all |
| `pageIndex` | integer | No | Ignored (kept for backward compatibility) |
| `pageSize` | integer | No | Ignored (kept for backward compatibility) |

**Request Example:**
```bash
# Get all active room types
GET /api/rooms/types?isActive=true

# Get all room types (active and inactive)
GET /api/rooms/types

# Get inactive room types only
GET /api/rooms/types?isActive=false
```

**Response Format:**
```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "message": "Lấy danh sách loại phòng thành công",
  "data": {
    "items": [
      {
        "roomTypeId": 1,
        "typeName": "Deluxe Room",
        "typeCode": "DR001",
        "description": "Phòng cao cấp với view đẹp",
        "basePriceNight": 500000,
        "maxOccupancy": 4,
        "roomSize": 45.5,
        "numberOfBeds": 2,
        "bedType": "King",
        "isActive": true,
        "images": [
          {
            "mediaId": 1,
            "filePath": "https://res.cloudinary.com/...",
            "description": "Deluxe Room main view",
            "displayOrder": 0,
            "publishId": "cloudinary_public_id_1"
          },
          {
            "mediaId": 2,
            "filePath": "https://res.cloudinary.com/...",
            "description": "Deluxe Room bathroom",
            "displayOrder": 1,
            "publishId": "cloudinary_public_id_2"
          }
        ],
        "amenities": [...],
        "comments": [...],
        "totalRooms": 5,
        "createdAt": "2024-12-10T10:00:00Z",
        "updatedAt": "2024-12-10T15:30:00Z"
      },
      {
        "roomTypeId": 2,
        "typeName": "Standard Room",
        "typeCode": "SR001",
        ...
      }
    ],
    "totalCount": 2,
    "pageIndex": 1,
    "pageSize": 100
  },
  "statusCode": 200
}
```

**Response Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `roomTypeId` | integer | Unique ID of the room type |
| `typeName` | string | Display name (e.g., "Deluxe Room") |
| `typeCode` | string | Code identifier (e.g., "DR001") |
| `description` | string | Detailed description |
| `basePriceNight` | decimal | Price per night in your currency |
| `maxOccupancy` | integer | Maximum guests allowed |
| `roomSize` | decimal | Room area in m² |
| `numberOfBeds` | integer | Number of beds |
| `bedType` | string | Type of beds (King, Queen, Twin, etc.) |
| `isActive` | boolean | Whether room type is available for booking |
| `images[]` | array | Array of media items (see below) |
| `amenities[]` | array | Array of amenities |
| `comments[]` | array | Array of guest comments/reviews |
| `totalRooms` | integer | How many physical rooms of this type exist |

**Image/Media Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `mediaId` | integer | Database ID (use when updating/removing) |
| `filePath` | string | Full URL to the image from Cloudinary |
| `description` | string | Alt text or description |
| `displayOrder` | integer | Display order (0 = first, 1 = second, etc.) |
| `publishId` | string | Cloudinary public ID (for direct uploads) |

---

### 2. Get All Room Types (List View - for Admin)

**Endpoint:**
```
GET /api/rooms/admin/types
```

**Description:**
- Retrieves all room types with basic info (no pagination)
- Returns type names, codes, prices, and image count
- Used for room type management in admin panel

**Request Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `isActive` | boolean | No | Filter by active status |

**Response Example:**
```json
{
  "isSuccess": true,
  "data": [
    {
      "roomTypeId": 1,
      "typeName": "Deluxe Room",
      "typeCode": "DR001",
      "basePriceNight": 500000,
      "isActive": true,
      "imageCount": 3,
      "amenityCount": 5,
      "totalRooms": 5
    },
    {
      "roomTypeId": 2,
      "typeName": "Standard Room",
      "typeCode": "SR001",
      "basePriceNight": 300000,
      "isActive": true,
      "imageCount": 2,
      "amenityCount": 3,
      "totalRooms": 8
    }
  ],
  "statusCode": 200
}
```

---

### 3. Update Room Type (with Media CRUD)

**Endpoint:**
```
PUT /api/rooms/types/{id}
```

**Description:**
- Updates room type details and manages images using the new Media CRUD system
- Uses `ImageMedia` array with CRUD actions (add/keep/remove)
- Each image action includes DisplayOrder for custom ordering

**Request Body:**
```json
{
  "roomTypeId": 1,
  "typeName": "Deluxe Room - Updated",
  "typeCode": "DR001",
  "description": "Updated description",
  "basePriceNight": 550000,
  "maxOccupancy": 5,
  "roomSize": 50,
  "numberOfBeds": 2,
  "bedType": "King",
  "isActive": true,
  "imageMedia": [
    {
      "id": 1,
      "crudKey": "keep",
      "url": null,
      "altText": "Main view",
      "providerId": null
    },
    {
      "id": 2,
      "crudKey": "remove",
      "url": null,
      "altText": null,
      "providerId": null
    },
    {
      "id": null,
      "crudKey": "add",
      "url": "https://res.cloudinary.com/new-image.jpg",
      "altText": "Bathroom view",
      "providerId": "cloudinary_public_id_3"
    }
  ]
}
```

**ImageMedia Fields (Media CRUD DTO):**

| Field | Type | CrudKey | Description |
|-------|------|---------|-------------|
| `id` | integer\|null | keep, remove | Database media ID. Required for keep/remove. Null for add |
| `crudKey` | string | Required | Action: `"add"` \| `"keep"` \| `"remove"` |
| `url` | string\|null | add, keep | Cloudinary URL or file path. Use for add/keep operations |
| `altText` | string\|null | add, keep | Image description/alt text for accessibility |
| `providerId` | string\|null | add | Cloudinary public ID (alternative to url) |

**CRUD Actions Explained:**

1. **"add"** - Add new image
   - Required fields: `crudKey`, `url` (or `providerId`)
   - Optional: `altText`
   - `id` must be null

2. **"keep"** - Keep existing image (may update alt text or url)
   - Required fields: `crudKey`, `id`
   - Optional: `url` (to replace), `altText` (to update)
   - Image position in array determines `displayOrder` (0-indexed)

3. **"remove"** - Delete image
   - Required fields: `crudKey`, `id`
   - Other fields ignored

**Display Order:**
- Images are ordered by their position in the `imageMedia` array
- First image = displayOrder 0, second = 1, etc.
- If you reorder the array, displayOrder will be updated automatically

---

## 📝 Frontend Integration Examples

### Example 1: Fetch All Room Types (Basic)

```javascript
// Fetch all active room types
async function fetchRoomTypes() {
  try {
    const response = await fetch('/api/rooms/types?isActive=true');
    const data = await response.json();

    if (data.isSuccess) {
      console.log('Room Types:', data.data.items);
      // Use data.data.items for display
      data.data.items.forEach(roomType => {
        console.log(`${roomType.typeName} - ${roomType.images.length} images`);
      });
    } else {
      console.error('Error:', data.message);
    }
  } catch (error) {
    console.error('Fetch error:', error);
  }
}
```

### Example 2: Display Room Images

```javascript
// Display room images in a carousel
function displayRoomImages(roomType) {
  const container = document.getElementById('room-images');
  container.innerHTML = '';

  // Sort by displayOrder
  const sortedImages = roomType.images.sort((a, b) => a.displayOrder - b.displayOrder);

  sortedImages.forEach((image, index) => {
    const img = document.createElement('img');
    img.src = image.filePath;
    img.alt = image.description || 'Room image';
    img.dataset.order = index;
    container.appendChild(img);
  });
}
```

### Example 3: Update Room Type with Media CRUD

```javascript
// Update room type and manage images
async function updateRoomType(roomTypeId, updates) {
  const payload = {
    roomTypeId: roomTypeId,
    typeName: updates.typeName,
    typeCode: updates.typeCode,
    description: updates.description,
    basePriceNight: updates.basePriceNight,
    maxOccupancy: updates.maxOccupancy,
    roomSize: updates.roomSize,
    numberOfBeds: updates.numberOfBeds,
    bedType: updates.bedType,
    isActive: updates.isActive,
    imageMedia: updates.imageMedia // New media CRUD array
  };

  try {
    const response = await fetch(`/api/rooms/types/${roomTypeId}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(payload)
    });

    const data = await response.json();
    if (data.isSuccess) {
      console.log('Room type updated successfully');
      return data.data;
    } else {
      console.error('Update failed:', data.message);
    }
  } catch (error) {
    console.error('Error:', error);
  }
}
```

### Example 4: Manage Images (Keep, Add, Remove)

```javascript
// Example: User edits room images
function buildMediaCrudArray(currentImages, changes) {
  const imageMedia = [];

  // Keep existing images (that weren't removed)
  currentImages.forEach((img, index) => {
    if (!changes.removedIds.includes(img.mediaId)) {
      imageMedia.push({
        id: img.mediaId,
        crudKey: 'keep',
        url: null,          // Only set if you want to replace the URL
        altText: img.description,
        providerId: null
      });
    }
  });

  // Add new images
  changes.newImages.forEach(newImg => {
    imageMedia.push({
      id: null,
      crudKey: 'add',
      url: newImg.cloudinaryUrl,  // URL from Cloudinary upload
      altText: newImg.description,
      providerId: newImg.cloudinaryPublicId  // Optional
    });
  });

  return imageMedia;
}

// Usage
const currentImages = roomType.images; // From API response
const changes = {
  removedIds: [2, 3],  // Remove image IDs 2 and 3
  newImages: [
    {
      cloudinaryUrl: 'https://res.cloudinary.com/.../image1.jpg',
      cloudinaryPublicId: 'hotel/room1/image1',
      description: 'New main view'
    }
  ]
};

const imageMedia = buildMediaCrudArray(currentImages, changes);

await updateRoomType(1, {
  typeName: roomType.typeName,
  // ... other fields
  imageMedia: imageMedia
});
```

### Example 5: Reorder Images

```javascript
// Reorder images by changing their position in the array
function reorderImages(imageIds) {
  // imageIds: [2, 1, 3] means image with ID 2 becomes first, ID 1 second, etc.

  const imageMedia = imageIds.map((id, index) => ({
    id: id,
    crudKey: 'keep',
    url: null,         // Don't modify URL
    altText: null,     // Don't modify alt text
    providerId: null
  }));

  return imageMedia;
  // DisplayOrder will be auto-assigned based on array position
}

// Usage
const newOrder = [3, 1, 2];  // Reorder the images
const imageMedia = reorderImages(newOrder);

await updateRoomType(roomTypeId, {
  // ... other fields
  imageMedia: imageMedia
});
```

---

## 🎯 Common Scenarios

### Scenario 1: Add New Room Type with Images

```javascript
const newRoomRequest = {
  typeName: 'Presidential Suite',
  typeCode: 'PS001',
  description: 'Luxury suite',
  basePriceNight: 1000000,
  maxOccupancy: 2,
  roomSize: 80,
  numberOfBeds: 1,
  bedType: 'King',
  imageUrls: [  // For add endpoint, use imageUrls array of strings
    'https://res.cloudinary.com/.../image1.jpg',
    'https://res.cloudinary.com/.../image2.jpg'
  ]
};

// POST /api/rooms/types
const response = await fetch('/api/rooms/types', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(newRoomRequest)
});
```

### Scenario 2: Update Only Description (Keep All Images)

```javascript
// No changes to images - just omit or pass null for imageMedia
const updateRequest = {
  roomTypeId: 1,
  description: 'Updated description',
  imageMedia: null  // null means don't touch images
};

await updateRoomType(1, updateRequest);
```

### Scenario 3: Remove All Old Images and Add New Ones

```javascript
function replaceAllImages(currentImages, newImageUrls) {
  const imageMedia = [];

  // Mark all current images for removal
  currentImages.forEach(img => {
    imageMedia.push({
      id: img.mediaId,
      crudKey: 'remove'
    });
  });

  // Add new images
  newImageUrls.forEach(url => {
    imageMedia.push({
      id: null,
      crudKey: 'add',
      url: url,
      altText: 'Room image'
    });
  });

  return imageMedia;
}
```

---

## ⚠️ Important Notes

### Backward Compatibility
- The old `imageUrls` array (simple string array) is still supported but **deprecated**
- **Recommended:** Use the new `imageMedia` array for better control
- If both `imageMedia` and `imageUrls` are provided, `imageMedia` takes priority

### Image Ordering
- Images are ordered by their **position in the array**
- First item in array = displayOrder 0
- No need to manually set displayOrder - it's auto-assigned

### Cloudinary Integration
- `url` field: Full Cloudinary URL (e.g., `https://res.cloudinary.com/xyz/image/upload/...`)
- `providerId`: Cloudinary public ID for deletion/management (optional)
- Either `url` or `providerId` is sufficient for add operations

### Error Handling
```javascript
// Handle API errors
async function updateWithErrorHandling(roomTypeId, updates) {
  try {
    const response = await fetch(`/api/rooms/types/${roomTypeId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(updates)
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }

    const data = await response.json();
    if (!data.isSuccess) {
      throw new Error(data.message || 'Update failed');
    }

    return data.data;
  } catch (error) {
    console.error('Update error:', error.message);
    // Show error to user
  }
}
```

---

## 📊 Database Impact

Each media CRUD operation updates the database as follows:

| Operation | Action | Fields Updated |
|-----------|--------|-----------------|
| **add** | Insert new Medium | FilePath, Description, DisplayOrder, CreatedAt, CreatedBy |
| **keep** | Update existing | DisplayOrder, Description, FilePath (if provided), UpdatedAt, UpdatedBy |
| **remove** | Delete | Deletes the record completely |

**Display Order Update:**
- After all CRUD operations, displayOrder values are recalculated based on array position
- This ensures consistent ordering regardless of add/remove operations

---

## 🧪 Testing Checklist

- [ ] Fetch all room types without filter
- [ ] Fetch with `isActive=true` filter
- [ ] Fetch with `isActive=false` filter
- [ ] Display images in correct order
- [ ] Add new image to existing room
- [ ] Remove image from existing room
- [ ] Keep image and update its alt text
- [ ] Reorder images
- [ ] Replace all images at once
- [ ] Verify displayOrder updates correctly
- [ ] Handle API errors gracefully

---

## 📞 Support

For issues or questions:
1. Check the response error message: `data.message`
2. Verify request body matches the schema
3. Ensure all required fields are provided
4. Check that `crudKey` is one of: `"add"`, `"keep"`, `"remove"`
5. For add operations, ensure `url` or `providerId` is provided

---

**Last Updated:** 2024-12-10
**API Version:** 1.0
**Media CRUD System:** Active
