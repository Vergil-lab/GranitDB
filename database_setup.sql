-- ============================================================
-- ООО «ТрансГранит» — Скрипт создания базы данных
-- Система учёта заявок на поставку строительных материалов
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'GranitDB')
BEGIN
    CREATE DATABASE GranitDB;
END
GO

USE GranitDB;
GO

-- ─── Таблица: Клиенты ────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Clients' AND xtype='U')
CREATE TABLE Clients (
    ClientID    INT IDENTITY(1,1) PRIMARY KEY,
    CompanyName NVARCHAR(200) NOT NULL,
    ContactName NVARCHAR(100) NOT NULL,
    Phone       NVARCHAR(20),
    Email       NVARCHAR(100),
    Address     NVARCHAR(300),
    CreatedAt   DATETIME DEFAULT GETDATE()
);
GO

-- ─── Таблица: Категории материалов ──────────────────────────
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Categories' AND xtype='U')
CREATE TABLE Categories (
    CategoryID   INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL
);
GO

-- ─── Таблица: Материалы (справочник) ────────────────────────
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Materials' AND xtype='U')
CREATE TABLE Materials (
    MaterialID   INT IDENTITY(1,1) PRIMARY KEY,
    MaterialName NVARCHAR(200) NOT NULL,
    CategoryID   INT FOREIGN KEY REFERENCES Categories(CategoryID),
    Unit         NVARCHAR(20) NOT NULL,   -- шт, м², м³, т, кг, л
    PricePerUnit DECIMAL(12,2) NOT NULL,
    Stock        INT DEFAULT 0            -- остаток на складе
);
GO

-- ─── Таблица: Статусы заявок ─────────────────────────────────
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OrderStatuses' AND xtype='U')
CREATE TABLE OrderStatuses (
    StatusID   INT IDENTITY(1,1) PRIMARY KEY,
    StatusName NVARCHAR(50) NOT NULL,
    ColorHex   NVARCHAR(10) DEFAULT '#AAAAAA'
);
GO

-- ─── Таблица: Заявки на поставку ─────────────────────────────
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Orders' AND xtype='U')
CREATE TABLE Orders (
    OrderID        INT IDENTITY(1,1) PRIMARY KEY,
    OrderNumber    NVARCHAR(20) NOT NULL UNIQUE,
    ClientID       INT NOT NULL FOREIGN KEY REFERENCES Clients(ClientID),
    StatusID       INT NOT NULL FOREIGN KEY REFERENCES OrderStatuses(StatusID),
    DeliveryAddress NVARCHAR(300),
    CreatedAt      DATETIME DEFAULT GETDATE(),
    DeliveryDate   DATE,
    TotalAmount    DECIMAL(14,2) DEFAULT 0,
    Notes          NVARCHAR(500)
);
GO

-- ─── Таблица: Позиции заявки (состав) ────────────────────────
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OrderItems' AND xtype='U')
CREATE TABLE OrderItems (
    ItemID     INT IDENTITY(1,1) PRIMARY KEY,
    OrderID    INT NOT NULL FOREIGN KEY REFERENCES Orders(OrderID) ON DELETE CASCADE,
    MaterialID INT NOT NULL FOREIGN KEY REFERENCES Materials(MaterialID),
    Quantity   DECIMAL(10,2) NOT NULL,
    UnitPrice  DECIMAL(12,2) NOT NULL,
    LineTotal  AS (Quantity * UnitPrice)  -- вычисляемый столбец
);
GO

-- ─── Индексы ─────────────────────────────────────────────────
CREATE INDEX IX_Orders_Client  ON Orders(ClientID);
CREATE INDEX IX_Orders_Status  ON Orders(StatusID);
CREATE INDEX IX_Orders_Created ON Orders(CreatedAt);
CREATE INDEX IX_Items_Order    ON OrderItems(OrderID);
GO

-- ─── Начальные данные ────────────────────────────────────────

INSERT INTO OrderStatuses (StatusName, ColorHex) VALUES
    (N'Новая',           '#2196F3'),
    (N'Подтверждена',    '#FF9800'),
    (N'На складе',       '#9C27B0'),
    (N'Отгружена',       '#03A9F4'),
    (N'Доставлена',      '#4CAF50'),
    (N'Отменена',        '#F44336');

INSERT INTO Categories (CategoryName) VALUES
    (N'Цемент и смеси'),
    (N'Кирпич и блоки'),
    (N'Металлопрокат'),
    (N'Пиломатериалы'),
    (N'Кровельные материалы'),
    (N'Утеплители'),
    (N'Отделочные материалы');

INSERT INTO Materials (MaterialName, CategoryID, Unit, PricePerUnit, Stock) VALUES
    (N'Цемент М400 (50 кг)',         1, N'мешок',  320.00,  500),
    (N'Пескобетон М300 (40 кг)',      1, N'мешок',  290.00,  300),
    (N'Кирпич рядовой М150',         2, N'шт',      12.50, 15000),
    (N'Блок газобетонный 600×300×200',2, N'шт',     180.00, 3000),
    (N'Арматура А500С ø12мм',        3, N'т',     48000.00,   20),
    (N'Профнастил С-8 (1.2м)',        5, N'м²',     380.00,  800),
    (N'Доска обрезная 50×150×6000',  4, N'м³',    7500.00,   40),
    (N'Минвата ROCKWOOL 100мм',       6, N'м²',     320.00,  600),
    (N'Гипсокартон 12.5мм',          7, N'лист',   480.00, 1200),
    (N'Плитка керамическая 30×30',   7, N'м²',     650.00,  400);

INSERT INTO Clients (CompanyName, ContactName, Phone, Email, Address) VALUES
    (N'ООО «СтройМастер»',    N'Иванов А.С.',   N'+7 495 123-45-67', N'ivanov@stroymaster.ru',   N'Москва, ул. Строителей, 15'),
    (N'ИП Петров К.В.',       N'Петров К.В.',   N'+7 910 987-65-43', N'petrov@mail.ru',           N'Москва, Новосибирская, 3'),
    (N'ЗАО «РемСервис»',      N'Сидорова Е.Н.', N'+7 495 555-00-11', N'info@remservice.ru',       N'Москва, пр-т Мира, 88'),
    (N'ООО «ГорСтрой»',       N'Козлов Д.В.',   N'+7 916 000-22-33', N'kozlov@gorstroy.ru',       N'Москва, ул. Садовая, 42'),
    (N'ИП Макаров Р.И.',      N'Макаров Р.И.',  N'+7 926 111-33-55', N'makarov@yandex.ru',        N'Москва, Лесная, 7');

-- Заявки
INSERT INTO Orders (OrderNumber, ClientID, StatusID, DeliveryAddress, DeliveryDate, TotalAmount, Notes) VALUES
    (N'SS-2025-0001', 1, 5, N'Москва, ул. Строителей, 15',  '2025-04-03', 128000.00, N'Срочная доставка'),
    (N'SS-2025-0002', 2, 4, N'Москва, Новосибирская, 3',    '2025-04-14', 57600.00,  NULL),
    (N'SS-2025-0003', 3, 2, N'Москва, пр-т Мира, 88',       '2025-04-22', 96000.00,  N'Доставка до 10:00'),
    (N'SS-2025-0004', 1, 1, N'Москва, ул. Строителей, 15',  '2025-04-25', 24000.00,  NULL),
    (N'SS-2025-0005', 4, 6, N'Москва, ул. Садовая, 42',     '2025-04-08', 15000.00,  N'Клиент отказался');

-- Позиции заявок
INSERT INTO OrderItems (OrderID, MaterialID, Quantity, UnitPrice) VALUES
    (1, 1,  200, 320.00),   -- Цемент 200 мешков
    (1, 3, 3000,  12.50),   -- Кирпич 3000 шт
    (2, 6,   80, 380.00),   -- Профнастил 80 м²
    (2, 8,   60, 320.00),   -- Минвата 60 м²
    (3, 4,  200, 180.00),   -- Газоблок 200 шт
    (3, 5,    1, 48000.00), -- Арматура 1 т
    (4, 9,   50, 480.00),   -- Гипсокартон 50 листов
    (5, 2,   50, 290.00),   -- Пескобетон 50 мешков
    (5, 10,   4, 650.00);   -- Плитка 4 м²
GO

PRINT 'База данных GranitDB успешно создана и заполнена тестовыми данными.';
GO
