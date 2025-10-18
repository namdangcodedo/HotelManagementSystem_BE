

-- Seed Additional Amenities (Manager có thể add thêm vào phòng cụ thể)
INSERT INTO [dbo].[Amenity] ([AmenityName], [Description], [AmenityType], [IsActive], [CreatedAt], [CreatedBy])
VALUES 
    (N'Máy pha cà phê Nespresso', N'Máy pha cà phê cao cấp với capsule miễn phí', 'Additional', 1, GETDATE(), 1),
    (N'Minibar cao cấp', N'Minibar với đồ uống và snack đa dạng', 'Additional', 1, GETDATE(), 1),
    (N'Bồn tắm nằm', N'Bồn tắm nằm thư giãn lớn', 'Additional', 1, GETDATE(), 1),
