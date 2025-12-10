# Frontend Integration Guide - Room Type API & Media CRUD

## 🚀 Quick Start

### 1. Fetch All Room Types (No Pagination)

```javascript
// Fetch all room types - simple and clean
const response = await fetch('http://your-api/api/rooms/types');
const data = await response.json();

if (data.isSuccess) {
  const roomTypes = data.data.items; // All room types here
  roomTypes.forEach(room => {
    console.log(`${room.typeName}: ${room.images.length} images`);
  });
}
```

### 2. Filter by Active Status

```javascript
// Get only active room types
const response = await fetch('http://your-api/api/rooms/types?isActive=true');
const data = await response.json();
const activeRooms = data.data.items;

// Get inactive room types
const response2 = await fetch('http://your-api/api/rooms/types?isActive=false');
const data2 = await response2.json();
const inactiveRooms = data2.data.items;
```

### 3. Search Room Types

The backend supports searching by TypeName, TypeCode, and Description (if you add `Search` parameter support):

```javascript
// Example structure (if implemented in GetRoomTypeListRequest)
const response = await fetch('http://your-api/api/rooms/types?search=deluxe');
```

---

## 📸 Working with Images

### Display Images in Order

```javascript
function displayRoomImages(roomType) {
  // Images are automatically ordered by displayOrder
  const sortedImages = roomType.images
    .sort((a, b) => a.displayOrder - b.displayOrder);

  return sortedImages.map(img => ({
    src: img.filePath,
    alt: img.description || 'Room image',
    order: img.displayOrder
  }));
}
```

### Image Object Structure

```javascript
{
  "mediaId": 1,                    // Use this for keep/remove
  "filePath": "https://...",       // Cloudinary URL
  "description": "Main view",      // Alt text
  "displayOrder": 0,               // Order (0 = first)
  "publishId": "cloudinary_id",    // Optional: Cloudinary public ID
  "createdAt": "2024-12-10T...",
  "createdBy": 1,
  "updatedAt": "2024-12-10T...",
  "updatedBy": 1,
  "isActive": true
}
```

---

## ✏️ Update Room Type with Images

### Complete Update Example

```javascript
async function updateRoomType(roomTypeId, formData) {
  const payload = {
    roomTypeId: roomTypeId,
    typeName: formData.typeName,
    typeCode: formData.typeCode,
    description: formData.description,
    basePriceNight: formData.price,
    maxOccupancy: formData.maxGuests,
    roomSize: formData.size,
    numberOfBeds: formData.beds,
    bedType: formData.bedType,
    isActive: formData.isActive,
    imageMedia: buildImageMediaArray(formData.images) // See below
  };

  const response = await fetch(`/api/rooms/types/${roomTypeId}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(payload)
  });

  return await response.json();
}
```

### Build Image Media Array

```javascript
function buildImageMediaArray(currentImages, changes) {
  const imageMedia = [];

  // Keep existing images (except removed ones)
  currentImages.forEach((img, index) => {
    if (!changes.removedIds?.includes(img.mediaId)) {
      imageMedia.push({
        id: img.mediaId,
        crudKey: 'keep',
        url: null,           // Don't change URL
        altText: img.description,
        providerId: null
      });
    }
  });

  // Add new images from Cloudinary
  changes.newImages?.forEach(newImg => {
    imageMedia.push({
      id: null,
      crudKey: 'add',
      url: newImg.cloudinaryUrl,
      altText: newImg.description,
      providerId: newImg.cloudinaryPublicId // Optional
    });
  });

  return imageMedia;
}

// Usage
const changes = {
  removedIds: [2, 3],  // Remove images with these IDs
  newImages: [
    {
      cloudinaryUrl: 'https://res.cloudinary.com/.../image.jpg',
      cloudinaryPublicId: 'hotel/room/image1',
      description: 'New bathroom photo'
    }
  ]
};

await updateRoomType(1, {
  typeName: 'Updated Name',
  // ... other fields
  images: currentRoomImages, // The current images array
}, changes);
```

---

## 🎯 Common Patterns

### Pattern 1: Add Single Image

```javascript
const imageMedia = [
  ...currentImages.map(img => ({
    id: img.mediaId,
    crudKey: 'keep'
  })),
  {
    id: null,
    crudKey: 'add',
    url: 'https://res.cloudinary.com/.../new-image.jpg',
    altText: 'New room photo'
  }
];
```

### Pattern 2: Remove Single Image

```javascript
const imageMedia = currentImages
  .filter(img => img.mediaId !== idToRemove)
  .map(img => ({
    id: img.mediaId,
    crudKey: 'keep'
  }));
```

### Pattern 3: Reorder Images

```javascript
// Reorder based on user's drag-and-drop
const newOrder = [3, 1, 2]; // Array of mediaIds in new order

const imageMedia = newOrder.map(mediaId => ({
  id: mediaId,
  crudKey: 'keep',
  url: null,
  altText: null
}));
// DisplayOrder will be auto-assigned based on array position
```

### Pattern 4: Replace All Images

```javascript
const imageMedia = [
  // Remove all old images
  ...currentImages.map(img => ({
    id: img.mediaId,
    crudKey: 'remove'
  })),
  // Add all new images
  ...newImages.map(img => ({
    id: null,
    crudKey: 'add',
    url: img.url,
    altText: img.description
  }))
];
```

---

## 🔌 API Response Structure

### Success Response

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
        "description": "...",
        "basePriceNight": 500000,
        "maxOccupancy": 4,
        "roomSize": 45.5,
        "numberOfBeds": 2,
        "bedType": "King",
        "isActive": true,
        "images": [ /* sorted by displayOrder */ ],
        "amenities": [ /* room amenities */ ],
        "totalRooms": 5,
        "createdAt": "2024-12-10T10:00:00Z",
        "updatedAt": "2024-12-10T15:30:00Z"
      }
    ],
    "totalCount": 2,
    "pageIndex": 1,
    "pageSize": 2,
    "totalPages": 1
  },
  "statusCode": 200
}
```

### Error Response

```json
{
  "isSuccess": false,
  "responseCode": "ERROR",
  "message": "Error message describing what went wrong",
  "data": null,
  "statusCode": 400
}
```

---

## 🛠️ React Component Example

```jsx
import React, { useState, useEffect } from 'react';

function RoomTypeManager() {
  const [roomTypes, setRoomTypes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Fetch room types on mount
  useEffect(() => {
    fetchRoomTypes();
  }, []);

  const fetchRoomTypes = async () => {
    try {
      setLoading(true);
      const response = await fetch('/api/rooms/types?isActive=true');
      const data = await response.json();

      if (data.isSuccess) {
        setRoomTypes(data.data.items);
      } else {
        setError(data.message);
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateRoom = async (roomTypeId, imageMedia) => {
    try {
      const response = await fetch(`/api/rooms/types/${roomTypeId}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({
          roomTypeId: roomTypeId,
          imageMedia: imageMedia
          // ... other fields
        })
      });

      const data = await response.json();
      if (data.isSuccess) {
        // Refresh the list
        fetchRoomTypes();
      } else {
        alert('Update failed: ' + data.message);
      }
    } catch (error) {
      alert('Error: ' + error.message);
    }
  };

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
      <h1>Room Types</h1>
      {roomTypes.map(room => (
        <RoomTypeCard
          key={room.roomTypeId}
          room={room}
          onUpdate={handleUpdateRoom}
        />
      ))}
    </div>
  );
}

function RoomTypeCard({ room, onUpdate }) {
  return (
    <div className="room-card">
      <h2>{room.typeName}</h2>
      <p>{room.description}</p>
      <p>Price: ${room.basePriceNight}</p>

      <div className="images">
        {room.images
          .sort((a, b) => a.displayOrder - b.displayOrder)
          .map(img => (
            <img
              key={img.mediaId}
              src={img.filePath}
              alt={img.description}
              style={{ width: '200px', height: '150px', objectFit: 'cover' }}
            />
          ))}
      </div>
    </div>
  );
}

export default RoomTypeManager;
```

---

## Vue.js Example

```vue
<template>
  <div class="room-types">
    <h1>Room Types</h1>
    <div v-if="loading">Loading...</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else>
      <div v-for="room in roomTypes" :key="room.roomTypeId" class="room-card">
        <h2>{{ room.typeName }}</h2>
        <p>{{ room.description }}</p>
        <p>Price: ${{ room.basePriceNight }}</p>

        <div class="images">
          <img
            v-for="img in sortedImages(room.images)"
            :key="img.mediaId"
            :src="img.filePath"
            :alt="img.description"
            style="width: 200px; height: 150px; object-fit: cover;"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  data() {
    return {
      roomTypes: [],
      loading: true,
      error: null
    };
  },

  mounted() {
    this.fetchRoomTypes();
  },

  methods: {
    async fetchRoomTypes() {
      try {
        const response = await fetch('/api/rooms/types?isActive=true');
        const data = await response.json();

        if (data.isSuccess) {
          this.roomTypes = data.data.items;
        } else {
          this.error = data.message;
        }
      } catch (err) {
        this.error = err.message;
      } finally {
        this.loading = false;
      }
    },

    sortedImages(images) {
      return images.sort((a, b) => a.displayOrder - b.displayOrder);
    }
  }
};
</script>
```

---

## ⚙️ Configuration

### API Base URL

```javascript
// Set this to your backend URL
const API_BASE = 'https://api.yourdomain.com';
const API_ENDPOINTS = {
  GET_ROOM_TYPES: `${API_BASE}/api/rooms/types`,
  UPDATE_ROOM_TYPE: (id) => `${API_BASE}/api/rooms/types/${id}`
};
```

### Authorization

```javascript
// Add token to requests
const headers = {
  'Content-Type': 'application/json',
  'Authorization': `Bearer ${localStorage.getItem('authToken')}`
};
```

---

## 🐛 Error Handling

```javascript
async function safeApiCall(url, options = {}) {
  try {
    const response = await fetch(url, options);

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    const data = await response.json();

    if (!data.isSuccess) {
      throw new Error(data.message || 'API returned error');
    }

    return data.data;
  } catch (error) {
    console.error('API Error:', error);
    throw error;
  }
}

// Usage
try {
  const roomTypes = await safeApiCall('/api/rooms/types?isActive=true');
  console.log(roomTypes);
} catch (error) {
  console.error('Failed to fetch:', error.message);
}
```

---

## 📋 Checklist for Integration

- [ ] Fetch room types on page load
- [ ] Display images in correct order (by displayOrder)
- [ ] Handle loading and error states
- [ ] Implement image add functionality
- [ ] Implement image remove functionality
- [ ] Implement image reorder functionality
- [ ] Update room details with image changes
- [ ] Add authorization token to requests
- [ ] Handle API errors gracefully
- [ ] Test with various room type scenarios
- [ ] Test with multiple images
- [ ] Verify display order preservation

---

## 📞 Troubleshooting

### Images Not Displaying
- Check that `filePath` is a valid Cloudinary URL
- Verify CORS is enabled on your backend
- Check browser console for 403/404 errors

### Update Fails
- Verify `roomTypeId` matches the room you're updating
- Check that all required fields are provided
- Ensure `crudKey` is one of: "add", "keep", "remove"
- For "add", verify `url` or `providerId` is provided
- For "keep"/"remove", verify `id` is provided

### Images in Wrong Order
- Ensure images are sorted by `displayOrder` in frontend
- Verify the order of items in `imageMedia` array matches desired order

---

**Last Updated:** 2024-12-10
**Version:** 1.0
