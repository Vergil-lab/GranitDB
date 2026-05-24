# 🏗️ StroiSnabApp — Учёт заявок на поставку строительных материалов

> Проект производственной практики · ООО «СтройСнаб» · 20 апреля — 30 мая 2025  
> Специальность: **Информационные системы и программирование (ИСИП)**

---

## 📌 О проекте

**StroiSnabApp** — десктопное WinForms-приложение для автоматизации учёта заявок на поставку строительных материалов. Менеджер создаёт заявки, добавляет в них позиции материалов, отслеживает статусы поставок и выгружает отчёты в JSON.

### Функциональность

| Возможность | Описание |
|---|---|
| ✅ CRUD заявок | Создание, просмотр, редактирование, удаление |
| 📋 Состав заявки | Добавление/удаление позиций материалов с пересчётом суммы |
| 🔍 Поиск и фильтрация | По номеру, клиенту, адресу доставки, статусу |
| 🎨 Цветовая индикация | Статус цветом; красный фон — просроченная доставка |
| 📊 Сводная статистика | Всего / Новые / В работе / Доставлено / Сумма |
| 📤 Экспорт в JSON | Выгрузка текущей выборки с итоговой суммой |
| 🔢 Автономер | Формат SS-{год}-{NNNN} генерируется в SQL |

---

## 🛠️ Стек технологий

```
C# (.NET Framework 4.8)    — язык разработки
WinForms                    — графический интерфейс
ADO.NET (SqlConnection)     — работа с базой данных
SQL Server 2019/2022/Express — СУБД
T-SQL                       — запросы, вычисляемые столбцы, каскадное удаление
JSON (ручная сериализация)  — экспорт отчётов
Git / GitHub                — контроль версий
```

---

## 📁 Структура проекта

```
StroiSnabApp/
│
├── Program.cs
│
├── Models/
│   ├── Order.cs         ← Заявка на поставку
│   └── Other.cs         ← Client, Material, Category, OrderStatus, OrderItem
│
├── Data/
│   └── DatabaseHelper.cs ← Все SQL-запросы (CRUD + позиции + статистика)
│
├── Services/
│   └── JsonExportService.cs ← Экспорт в JSON
│
├── Forms/
│   ├── MainForm.cs      ← Главное окно (список заявок)
│   ├── OrderForm.cs     ← Создание / редактирование заявки
│   └── ItemsForm.cs     ← Состав заявки (позиции материалов)
│
├── database_setup.sql   ← Скрипт создания БД + тестовые данные
├── .gitignore
└── StroiSnabApp.csproj
```

---

## 🗃️ Схема базы данных

```
Categories          Materials
──────────          ─────────────────────────
CategoryID  PK      MaterialID   PK
CategoryName        MaterialName
                    CategoryID   FK → Categories
                    Unit         (шт, м², м³, т, кг)
                    PricePerUnit
                    Stock

Clients             OrderStatuses
───────────         ─────────────
ClientID    PK      StatusID  PK
CompanyName         StatusName
ContactName         ColorHex
Phone
Email
Address

Orders                          OrderItems
──────────────────────────────  ──────────────────────────
OrderID        PK               ItemID      PK
OrderNumber    UNIQUE           OrderID     FK → Orders (CASCADE)
ClientID       FK → Clients     MaterialID  FK → Materials
StatusID       FK → Statuses    Quantity
DeliveryAddress                 UnitPrice
CreatedAt                       LineTotal   COMPUTED (Qty * Price)
DeliveryDate
TotalAmount
Notes
```

---

## 🚀 Быстрый старт

### 1. Клонировать репозиторий
```bash
git clone https://github.com/<логин>/StroiSnabApp.git
```

### 2. Создать базу данных
Открой `database_setup.sql` в SSMS и выполни `F5`.  
База `StroiSnabDB` создастся с таблицами и тестовыми данными (10 материалов, 5 клиентов, 5 заявок).

### 3. Проверить строку подключения
`Data/DatabaseHelper.cs`:
```csharp
@"Server=.;Database=StroiSnabDB;Integrated Security=True;"
// Для SQL Server Express:
@"Server=.\SQLEXPRESS;Database=StroiSnabDB;Integrated Security=True;"
```

### 4. Запустить
```
Открой StroiSnabApp.csproj в Visual Studio → F5
```

---

## 💡 Ключевые SQL-запросы

### Заявки с JOIN
```sql
SELECT o.OrderID, o.OrderNumber,
       c.CompanyName AS ClientName, c.Phone,
       s.StatusName,  s.ColorHex,
       o.DeliveryAddress, o.TotalAmount, o.DeliveryDate
FROM   Orders o
JOIN   Clients       c ON c.ClientID = o.ClientID
JOIN   OrderStatuses s ON s.StatusID = o.StatusID
WHERE  o.StatusID = @StatusID
  AND  o.OrderNumber LIKE @Search
ORDER BY o.CreatedAt DESC
```

### Пересчёт суммы заявки после изменения позиций
```sql
UPDATE Orders
SET TotalAmount = (
    SELECT ISNULL(SUM(Quantity * UnitPrice), 0)
    FROM   OrderItems WHERE OrderID = @OrderID
)
WHERE OrderID = @OrderID
```

### Сводная статистика
```sql
SELECT
    COUNT(*)                                              AS TotalOrders,
    SUM(CASE WHEN StatusID = 1 THEN 1 ELSE 0 END)        AS NewOrders,
    SUM(CASE WHEN StatusID IN (2,3,4) THEN 1 ELSE 0 END) AS InProgress,
    SUM(CASE WHEN StatusID = 5 THEN 1 ELSE 0 END)        AS Delivered,
    ISNULL(SUM(CASE WHEN StatusID != 6 THEN TotalAmount ELSE 0 END), 0) AS TotalRevenue
FROM Orders
```

---

## 📋 История изменений

| Версия | Дата | Что изменено |
|---|---|---|
| v1.0 | 25.05.2025 | Первый релиз: CRUD заявок, состав, экспорт JSON |

---

*Учебный проект. Производственная практика — специальность «Информационные системы и программирование».*
