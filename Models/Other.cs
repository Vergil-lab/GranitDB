// Models/Client.cs
namespace StroiSnabApp.Models
{
    public class Client
    {
        public int    ClientID    { get; set; }
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        public string Phone       { get; set; }
        public string Email       { get; set; }
        public string Address     { get; set; }

        public override string ToString() => CompanyName;
    }
}

// Models/Category.cs
namespace StroiSnabApp.Models
{
    public class Category
    {
        public int    CategoryID   { get; set; }
        public string CategoryName { get; set; }

        public override string ToString() => CategoryName;
    }
}

// Models/Material.cs
namespace StroiSnabApp.Models
{
    public class Material
    {
        public int     MaterialID   { get; set; }
        public string  MaterialName { get; set; }
        public int     CategoryID   { get; set; }
        public string  CategoryName { get; set; }
        public string  Unit         { get; set; }
        public decimal PricePerUnit { get; set; }
        public int     Stock        { get; set; }

        public override string ToString() =>
            $"{MaterialName} ({Unit}) — {PricePerUnit:N2} ₽";
    }
}

// Models/OrderStatus.cs
namespace StroiSnabApp.Models
{
    public class OrderStatus
    {
        public int    StatusID   { get; set; }
        public string StatusName { get; set; }
        public string ColorHex   { get; set; }

        public override string ToString() => StatusName;
    }
}

// Models/OrderItem.cs
namespace StroiSnabApp.Models
{
    public class OrderItem
    {
        public int     ItemID       { get; set; }
        public int     OrderID      { get; set; }
        public int     MaterialID   { get; set; }
        public string  MaterialName { get; set; }
        public string  Unit         { get; set; }
        public decimal Quantity     { get; set; }
        public decimal UnitPrice    { get; set; }
        public decimal LineTotal    => Quantity * UnitPrice;
    }
}
