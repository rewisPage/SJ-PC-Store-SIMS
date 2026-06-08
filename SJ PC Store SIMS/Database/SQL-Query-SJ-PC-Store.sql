-- =====================================================
-- FINAL DATABASE SCRIPT FOR SJ PC STORE
-- Integrated Sales and Inventory Management System
-- No ALTER statements – everything in CREATE TABLE
-- =====================================================

-- Drop database if exists (optional – comment out if not needed)
-- USE master;
-- IF EXISTS (SELECT * FROM sys.databases WHERE name = 'SJ_PC_STORE')
--     DROP DATABASE SJ_PC_STORE;
-- GO

-- Create database if not exists
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SJ_PC_STORE')
BEGIN
    CREATE DATABASE SJ_PC_STORE;
END
GO

USE SJ_PC_STORE;
GO

-- =====================================================
-- 1. SAFE DROP TABLES (Reverse dependency order)
-- =====================================================
IF OBJECT_ID('ATTACHMENTS', 'U') IS NOT NULL DROP TABLE ATTACHMENTS;
IF OBJECT_ID('TRANSACTION_ITEM', 'U') IS NOT NULL DROP TABLE TRANSACTION_ITEM;
IF OBJECT_ID('[TRANSACTION]', 'U') IS NOT NULL DROP TABLE [TRANSACTION];
IF OBJECT_ID('PROCUREMENT_INVOICE', 'U') IS NOT NULL DROP TABLE PROCUREMENT_INVOICE;
IF OBJECT_ID('PROCUREMENT_ITEM', 'U') IS NOT NULL DROP TABLE PROCUREMENT_ITEM;
IF OBJECT_ID('PROCUREMENT', 'U') IS NOT NULL DROP TABLE PROCUREMENT;
IF OBJECT_ID('STOCK_INSTANCE', 'U') IS NOT NULL DROP TABLE STOCK_INSTANCE;
IF OBJECT_ID('ITEM_MASTER', 'U') IS NOT NULL DROP TABLE ITEM_MASTER;
IF OBJECT_ID('CATEGORY_LIST', 'U') IS NOT NULL DROP TABLE CATEGORY_LIST;
IF OBJECT_ID('SUPPLIER', 'U') IS NOT NULL DROP TABLE SUPPLIER;
IF OBJECT_ID('ACTIVITY_LOG', 'U') IS NOT NULL DROP TABLE ACTIVITY_LOG;
IF OBJECT_ID('STORE_SETTINGS', 'U') IS NOT NULL DROP TABLE STORE_SETTINGS;
IF OBJECT_ID('[USER]', 'U') IS NOT NULL DROP TABLE [USER];
GO

-- =====================================================
-- 2. CREATE TABLES (with all columns from original + ALTER merges)
-- =====================================================

-- Lookup table for valid categories
CREATE TABLE CATEGORY_LIST (
    CategoryName VARCHAR(50) PRIMARY KEY
);

-- Store global settings (single row)
CREATE TABLE STORE_SETTINGS (
    StoreID INT PRIMARY KEY,
    StoreName VARCHAR(100),
    Address VARCHAR(255),
    ContactNumber VARCHAR(20),
    TIN VARCHAR(20),
    LogoPath VARCHAR(MAX)
);

-- User table (employees)
CREATE TABLE [USER] (
    UserID VARCHAR(20) PRIMARY KEY,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    ContactNumber VARCHAR(15),
    Username VARCHAR(50) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(20) NOT NULL,                  -- 'Admin' or 'Cashier'
    Passkey VARCHAR(6) NOT NULL,
    Status VARCHAR(20) DEFAULT 'Active'
);

-- Supplier table (with extended fields)
CREATE TABLE SUPPLIER (
    SupplierID VARCHAR(20) PRIMARY KEY,
    CompanyName VARCHAR(100) NOT NULL,
    ContactNumber VARCHAR(15),
    Address VARCHAR(255),
    ContactPerson VARCHAR(100) NULL,
    EmailAddress VARCHAR(100) NULL,
    Remarks VARCHAR(255) NULL,
    DateRegistered DATETIME DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1
);

-- Item Master (hardware blueprints)
CREATE TABLE ITEM_MASTER (
    ItemCode VARCHAR(50) PRIMARY KEY,
    Category VARCHAR(50) NOT NULL FOREIGN KEY REFERENCES CATEGORY_LIST(CategoryName),
    Specs VARCHAR(255) NOT NULL,
    BaselineCost DECIMAL(10,2) NOT NULL,
    CurrentValue DECIMAL(10,2) NOT NULL,
    ItemCondition VARCHAR(20) NOT NULL,        -- 'Brand New', 'Used', 'Refurbished'
    IsActive BIT DEFAULT 1,                    -- Soft delete flag
    CreatedTime DATETIME DEFAULT GETDATE(),
    CreatedBy VARCHAR(20) NULL FOREIGN KEY REFERENCES [USER](UserID),
    LastModifiedTime DATETIME NULL,
    ModifiedBy VARCHAR(20) NULL FOREIGN KEY REFERENCES [USER](UserID)
);

-- Procurement header (purchase orders) – final expanded version
CREATE TABLE PROCUREMENT (
    PO_Number VARCHAR(50) PRIMARY KEY,
    SupplierID VARCHAR(20) NOT NULL FOREIGN KEY REFERENCES SUPPLIER(SupplierID),
    OrderDate DATETIME DEFAULT GETDATE(),
    ExpectedDate DATETIME NULL,
    Status VARCHAR(20) DEFAULT 'Draft',        -- Draft, Pending Approval, Ordered, Received, Completed, Cancelled
    CreatedBy VARCHAR(20) NOT NULL FOREIGN KEY REFERENCES [USER](UserID),
    CreatedOn DATETIME DEFAULT GETDATE(),
    ModifiedBy VARCHAR(20) NULL FOREIGN KEY REFERENCES [USER](UserID),
    ModifiedOn DATETIME NULL,
    Remarks VARCHAR(MAX) NULL,
    SubTotal DECIMAL(12,2) NULL,
    Discount DECIMAL(12,2) NULL,
    Tax DECIMAL(12,2) NULL,
    GrandTotal DECIMAL(12,2) NULL
);

-- Procurement line items
CREATE TABLE PROCUREMENT_ITEM (
    ItemID INT IDENTITY(1,1) PRIMARY KEY,
    PO_Number VARCHAR(50) NOT NULL FOREIGN KEY REFERENCES PROCUREMENT(PO_Number),
    ItemCode VARCHAR(50) NOT NULL FOREIGN KEY REFERENCES ITEM_MASTER(ItemCode),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(12,2) NULL
);

-- Procurement invoice (for 3-way matching)
CREATE TABLE PROCUREMENT_INVOICE (
    InvoiceNumber VARCHAR(50) PRIMARY KEY,
    PO_Number VARCHAR(50) NOT NULL FOREIGN KEY REFERENCES PROCUREMENT(PO_Number),
    InvoiceDate DATETIME NULL,
    InvoiceAmount DECIMAL(12,2) NULL,
    InvoiceFilePath VARCHAR(MAX) NULL
);

-- Stock instance (individual physical items)
CREATE TABLE STOCK_INSTANCE (
    SerialNumber VARCHAR(100) PRIMARY KEY,
    Status VARCHAR(20) DEFAULT 'Available',    -- Available, Sold, Defective, RMA, Written-Off
    DefectReason VARCHAR(100) NULL,
    ItemCode VARCHAR(50) NOT NULL FOREIGN KEY REFERENCES ITEM_MASTER(ItemCode),
    SupplierID VARCHAR(20) NOT NULL FOREIGN KEY REFERENCES SUPPLIER(SupplierID),
    PO_Number VARCHAR(50) NOT NULL FOREIGN KEY REFERENCES PROCUREMENT(PO_Number)
);

-- Transaction header (sales receipts) – with all extended fields
CREATE TABLE [TRANSACTION] (
    ReceiptID VARCHAR(50) PRIMARY KEY,
    SaleDate DATETIME DEFAULT GETDATE(),
    GrandTotal DECIMAL(12,2) NOT NULL,
    CashierID VARCHAR(20) NOT NULL FOREIGN KEY REFERENCES [USER](UserID),
    CustomerName VARCHAR(100) NULL,
    PaymentMethod VARCHAR(20) DEFAULT 'Cash',
    TransactionNumber VARCHAR(100) NULL,
    Discount DECIMAL(12,2) DEFAULT 0,
    Tax DECIMAL(12,2) DEFAULT 0,
    SubTotal DECIMAL(12,2) DEFAULT 0,
    Status VARCHAR(20) DEFAULT 'Quotation',    -- Quotation, Completed, Voided
    WarrantyDays INT DEFAULT 7,
    CreatedBy VARCHAR(20) NOT NULL FOREIGN KEY REFERENCES [USER](UserID),
    CreatedOn DATETIME DEFAULT GETDATE(),
    ModifiedBy VARCHAR(20) NULL FOREIGN KEY REFERENCES [USER](UserID),
    ModifiedOn DATETIME NULL,
    Remarks VARCHAR(MAX) NULL
);

-- Transaction items (bridge between receipt and serial numbers)
CREATE TABLE TRANSACTION_ITEM (
    ReceiptID VARCHAR(50) NOT NULL FOREIGN KEY REFERENCES [TRANSACTION](ReceiptID),
    SerialNumber VARCHAR(100) NOT NULL FOREIGN KEY REFERENCES STOCK_INSTANCE(SerialNumber),
    SoldPrice DECIMAL(10,2) NOT NULL,
    PRIMARY KEY (ReceiptID, SerialNumber)
);

-- Attachments table (supports both PO and Transaction)
CREATE TABLE ATTACHMENTS (
    AttachmentID INT IDENTITY(1,1) PRIMARY KEY,
    PO_Number VARCHAR(50) NULL FOREIGN KEY REFERENCES PROCUREMENT(PO_Number),
    TransactionID VARCHAR(50) NULL FOREIGN KEY REFERENCES [TRANSACTION](ReceiptID),
    FileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    UploadedBy NVARCHAR(100) NOT NULL,
    UploadedDate DATETIME DEFAULT GETDATE()
);

-- Activity log (audit trail)
CREATE TABLE ACTIVITY_LOG (
    LogID INT IDENTITY(1,1) PRIMARY KEY,
    UserID VARCHAR(20) NOT NULL FOREIGN KEY REFERENCES [USER](UserID),
    ActionDescription VARCHAR(255) NOT NULL,
    LogDate DATETIME DEFAULT GETDATE()
);

-- =====================================================
-- 3. INSERT DEFAULT CATEGORIES
-- =====================================================
INSERT INTO CATEGORY_LIST (CategoryName) VALUES 
    ('Motherboard'),
    ('Processor'),
    ('RAM'),
    ('Graphics Card'),
    ('Storage');
GO

-- =====================================================
-- End of Script
-- =====================================================