/* 
=============================================================================
KỊCH BẢN TỐI ƯU HÓA DATABASE - ERP PRODUCTION SYSTEM
Mục tiêu: Tăng tốc độ Tìm kiếm, Lọc dữ liệu và Autocomplete
=============================================================================
*/

-- 1. TRANG QUẢN LÝ SẢN PHẨM (Products Page)
-- Giúp tìm kiếm theo mã, tên, nhóm và lọc theo loại/trạng thái cực nhanh
PRINT 'Optimizing ProductMasters...';
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductMasters_Search_Filters')
BEGIN
    CREATE INDEX IX_ProductMasters_Search_Filters 
    ON ProductMasters (ItemCode, Item_Status, Item_Type)
    INCLUDE (ItemName, [Group], Category, Brand);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductMasters_Timestamp')
BEGIN
    CREATE INDEX IX_ProductMasters_Timestamp 
    ON ProductMasters ([timestamp] DESC);
END
GO

-- 2. TRANG NHẬT KÝ TIÊU THỤ (Consumption Log)
-- Bảng này thường rất lớn, cần Index để Join và Lọc theo thời gian
PRINT 'Optimizing MESMaterialConsumption...';
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MESMaterialConsumption_PO_Search')
BEGIN
    CREATE INDEX IX_MESMaterialConsumption_PO_Search 
    ON MESMaterialConsumption (ProductionOrderNumber, BatchCode, IngredientCode)
    INCLUDE (Respone, Quantity, UnitOfMeasurement);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MESMaterialConsumption_Timestamp')
BEGIN
    CREATE INDEX IX_MESMaterialConsumption_Timestamp 
    ON MESMaterialConsumption ([timestamp] DESC);
END
GO

-- 3. TRANG LỆNH SẢN XUẤT (Production Orders)
-- Tối ưu hóa các phép JOIN phức tạp giữa PO, Sản phẩm và Công thức
PRINT 'Optimizing ProductionOrders...';
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_Main_Search')
BEGIN
    CREATE INDEX IX_ProductionOrders_Main_Search 
    ON ProductionOrders (ProductionOrderNumber, ProductCode, PlannedStart DESC)
    INCLUDE (ProcessArea, Shift, RecipeCode, RecipeVersion, Status);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Batches_PO_Link')
BEGIN
    CREATE INDEX IX_Batches_PO_Link 
    ON Batches (ProductionOrderId, BatchNumber)
    INCLUDE (Quantity, Status);
END
GO

-- 4. TRANG CÔNG THỨC SẢN XUẤT (Recipes Page)
-- Tối ưu tìm kiếm theo mã công thức và sản phẩm
PRINT 'Optimizing RecipeDetails...';
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RecipeDetails_Code_Status')
BEGIN
    CREATE INDEX IX_RecipeDetails_Code_Status 
    ON RecipeDetails (RecipeCode, ProductCode, RecipeStatus)
    INCLUDE (RecipeName, Version);
END
GO

-- 5. TRANG TRẠNG THÁI SẢN XUẤT (Production Status)
-- Tập trung vào việc đếm và thống kê theo thời gian thực
PRINT 'Optimizing ProductionStatus_Optimization...';
-- (Sử dụng chung các Index của ProductionOrders và MESMaterialConsumption ở trên)
GO

/* 
GHI CHÚ KIỂM TRA:
Sau khi chạy các lệnh trên, bạn có thể kiểm tra hiệu năng bằng cách:
1. Mở trang quản lý trên trình duyệt.
2. Thử lọc dữ liệu hoặc tìm kiếm.
3. Nếu vẫn chậm, hãy dùng lệnh sau để xem SQL Server gợi ý thêm gì:
   SELECT * FROM sys.dm_db_missing_index_details
*/
