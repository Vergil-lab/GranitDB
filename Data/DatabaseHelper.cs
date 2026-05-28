using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StroiSnabApp.Models;

namespace StroiSnabApp.Data
{
    public class DatabaseHelper
    {
        private readonly string _connStr =
            @"Server=localhost;Database=GranitDB;Integrated Security=True;";

        private SqlConnection GetConnection() =>
            new SqlConnection(_connStr);

        public List<Order> GetOrders(int? statusId = null, string search = null)
        {
            var list = new List<Order>();

            string sql = @"
                SELECT
                    o.OrderID,    o.OrderNumber,
                    o.ClientID,   c.CompanyName  AS ClientName,
                                  c.Phone        AS ClientPhone,
                    o.StatusID,   s.StatusName,  s.ColorHex AS StatusColor,
                    o.DeliveryAddress,
                    o.CreatedAt,  o.DeliveryDate,
                    o.TotalAmount, o.Notes
                FROM  Orders o
                JOIN  Clients      c ON c.ClientID = o.ClientID
                JOIN  OrderStatuses s ON s.StatusID = o.StatusID
                WHERE 1=1";

            if (statusId.HasValue)
                sql += " AND o.StatusID = @StatusID";
            if (!string.IsNullOrWhiteSpace(search))
                sql += @" AND (o.OrderNumber     LIKE @Search
                           OR  c.CompanyName     LIKE @Search
                           OR  o.DeliveryAddress LIKE @Search)";

            sql += " ORDER BY o.CreatedAt DESC";

            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                if (statusId.HasValue)
                    cmd.Parameters.AddWithValue("@StatusID", statusId.Value);
                if (!string.IsNullOrWhiteSpace(search))
                    cmd.Parameters.AddWithValue("@Search", "%" + search + "%");

                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read()) list.Add(MapOrder(rdr));
            }
            return list;
        }

        public Order GetOrderById(int orderId)
        {
            string sql = @"
                SELECT o.OrderID, o.OrderNumber,
                       o.ClientID, c.CompanyName AS ClientName, c.Phone AS ClientPhone,
                       o.StatusID, s.StatusName, s.ColorHex AS StatusColor,
                       o.DeliveryAddress, o.CreatedAt, o.DeliveryDate,
                       o.TotalAmount, o.Notes
                FROM   Orders o
                JOIN   Clients       c ON c.ClientID = o.ClientID
                JOIN   OrderStatuses s ON s.StatusID = o.StatusID
                WHERE  o.OrderID = @ID";

            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ID", orderId);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    return rdr.Read() ? MapOrder(rdr) : null;
            }
        }

        public int AddOrder(Order o)
        {
            string sql = @"
                DECLARE @num INT =
                    (SELECT ISNULL(MAX(OrderID), 0) + 1 FROM Orders);
                DECLARE @orderNum NVARCHAR(20) =
                    N'SS-' + CAST(YEAR(GETDATE()) AS NVARCHAR) + '-'
                    + RIGHT('0000' + CAST(@num AS NVARCHAR), 4);

                INSERT INTO Orders
                    (OrderNumber, ClientID, StatusID,
                     DeliveryAddress, DeliveryDate, TotalAmount, Notes)
                VALUES
                    (@orderNum, @ClientID, @StatusID,
                     @DeliveryAddress, @DeliveryDate, 0, @Notes);

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                AddOrderParams(cmd, o);
                conn.Open();
                return (int)cmd.ExecuteScalar();
            }
        }

        public void UpdateOrder(Order o)
        {
            string sql = @"
                UPDATE Orders SET
                    ClientID        = @ClientID,
                    StatusID        = @StatusID,
                    DeliveryAddress = @DeliveryAddress,
                    DeliveryDate    = @DeliveryDate,
                    Notes           = @Notes
                WHERE OrderID = @OrderID";

            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                AddOrderParams(cmd, o);
                cmd.Parameters.AddWithValue("@OrderID", o.OrderID);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteOrder(int orderId)
        {
            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(
                "DELETE FROM Orders WHERE OrderID = @ID", conn))
            {
                cmd.Parameters.AddWithValue("@ID", orderId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public List<OrderItem> GetOrderItems(int orderId)
        {
            var list = new List<OrderItem>();
            string sql = @"
                SELECT oi.ItemID, oi.OrderID,
                       oi.MaterialID, m.MaterialName, m.Unit,
                       oi.Quantity, oi.UnitPrice
                FROM   OrderItems oi
                JOIN   Materials  m ON m.MaterialID = oi.MaterialID
                WHERE  oi.OrderID = @OrderID
                ORDER BY oi.ItemID";

            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@OrderID", orderId);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(new OrderItem
                        {
                            ItemID       = rdr.GetInt32(0),
                            OrderID      = rdr.GetInt32(1),
                            MaterialID   = rdr.GetInt32(2),
                            MaterialName = rdr.GetString(3),
                            Unit         = rdr.GetString(4),
                            Quantity     = rdr.GetDecimal(5),
                            UnitPrice    = rdr.GetDecimal(6)
                        });
            }
            return list;
        }

        public void AddOrderItem(OrderItem item)
        {
            string sql = @"
                INSERT INTO OrderItems (OrderID, MaterialID, Quantity, UnitPrice)
                VALUES (@OrderID, @MaterialID, @Qty, @Price);

                UPDATE Orders
                SET TotalAmount = (
                    SELECT ISNULL(SUM(Quantity * UnitPrice), 0)
                    FROM   OrderItems WHERE OrderID = @OrderID
                )
                WHERE OrderID = @OrderID;";

            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@OrderID",    item.OrderID);
                cmd.Parameters.AddWithValue("@MaterialID", item.MaterialID);
                cmd.Parameters.AddWithValue("@Qty",        item.Quantity);
                cmd.Parameters.AddWithValue("@Price",      item.UnitPrice);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteOrderItem(int itemId, int orderId)
        {
            string sql = @"
                DELETE FROM OrderItems WHERE ItemID = @ItemID;

                UPDATE Orders
                SET TotalAmount = (
                    SELECT ISNULL(SUM(Quantity * UnitPrice), 0)
                    FROM   OrderItems WHERE OrderID = @OrderID
                )
                WHERE OrderID = @OrderID;";

            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ItemID",  itemId);
                cmd.Parameters.AddWithValue("@OrderID", orderId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }


        public List<Client> GetClients()
        {
            var list = new List<Client>();
            string sql = @"SELECT ClientID, CompanyName, ContactName,
                                  Phone, Email, ISNULL(Address,'')
                           FROM Clients ORDER BY CompanyName";
            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(new Client
                        {
                            ClientID    = rdr.GetInt32(0),
                            CompanyName = rdr.GetString(1),
                            ContactName = rdr.GetString(2),
                            Phone       = rdr.GetString(3),
                            Email       = rdr.IsDBNull(4) ? "" : rdr.GetString(4),
                            Address     = rdr.GetString(5)
                        });
            }
            return list;
        }

        public List<Material> GetMaterials()
        {
            var list = new List<Material>();
            string sql = @"
                SELECT m.MaterialID, m.MaterialName,
                       m.CategoryID, c.CategoryName,
                       m.Unit, m.PricePerUnit, m.Stock
                FROM   Materials m
                JOIN   Categories c ON c.CategoryID = m.CategoryID
                ORDER BY c.CategoryName, m.MaterialName";
            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(new Material
                        {
                            MaterialID   = rdr.GetInt32(0),
                            MaterialName = rdr.GetString(1),
                            CategoryID   = rdr.GetInt32(2),
                            CategoryName = rdr.GetString(3),
                            Unit         = rdr.GetString(4),
                            PricePerUnit = rdr.GetDecimal(5),
                            Stock        = rdr.GetInt32(6)
                        });
            }
            return list;
        }

        public List<OrderStatus> GetStatuses()
        {
            var list = new List<OrderStatus>();
            string sql = "SELECT StatusID, StatusName, ColorHex FROM OrderStatuses ORDER BY StatusID";
            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(new OrderStatus
                        {
                            StatusID   = rdr.GetInt32(0),
                            StatusName = rdr.GetString(1),
                            ColorHex   = rdr.IsDBNull(2) ? "#AAAAAA" : rdr.GetString(2)
                        });
            }
            return list;
        }


        public DataTable GetSummary()
        {
            string sql = @"
                SELECT
                    COUNT(*)                                            AS TotalOrders,
                    SUM(CASE WHEN StatusID = 1 THEN 1 ELSE 0 END)      AS NewOrders,
                    SUM(CASE WHEN StatusID IN (2,3,4) THEN 1 ELSE 0 END) AS InProgress,
                    SUM(CASE WHEN StatusID = 5 THEN 1 ELSE 0 END)      AS Delivered,
                    ISNULL(SUM(CASE WHEN StatusID != 6
                                    THEN TotalAmount ELSE 0 END), 0)   AS TotalRevenue
                FROM Orders";

            var dt = new DataTable();
            using (var conn = GetConnection())
            using (var da   = new SqlDataAdapter(sql, conn))
                da.Fill(dt);
            return dt;
        }


        private Order MapOrder(SqlDataReader r) => new Order
        {
            OrderID         = r.GetInt32(r.GetOrdinal("OrderID")),
            OrderNumber     = r.GetString(r.GetOrdinal("OrderNumber")),
            ClientID        = r.GetInt32(r.GetOrdinal("ClientID")),
            ClientName      = r.GetString(r.GetOrdinal("ClientName")),
            ClientPhone     = r.IsDBNull(r.GetOrdinal("ClientPhone"))
                                ? "" : r.GetString(r.GetOrdinal("ClientPhone")),
            StatusID        = r.GetInt32(r.GetOrdinal("StatusID")),
            StatusName      = r.GetString(r.GetOrdinal("StatusName")),
            StatusColor     = r.IsDBNull(r.GetOrdinal("StatusColor"))
                                ? "#AAAAAA" : r.GetString(r.GetOrdinal("StatusColor")),
            DeliveryAddress = r.IsDBNull(r.GetOrdinal("DeliveryAddress"))
                                ? "" : r.GetString(r.GetOrdinal("DeliveryAddress")),
            CreatedAt       = r.GetDateTime(r.GetOrdinal("CreatedAt")),
            DeliveryDate    = r.IsDBNull(r.GetOrdinal("DeliveryDate"))
                                ? (DateTime?)null
                                : r.GetDateTime(r.GetOrdinal("DeliveryDate")),
            TotalAmount     = r.GetDecimal(r.GetOrdinal("TotalAmount")),
            Notes           = r.IsDBNull(r.GetOrdinal("Notes"))
                                ? "" : r.GetString(r.GetOrdinal("Notes"))
        };

        private void AddOrderParams(SqlCommand cmd, Order o)
        {
            cmd.Parameters.AddWithValue("@ClientID",        o.ClientID);
            cmd.Parameters.AddWithValue("@StatusID",        o.StatusID);
            cmd.Parameters.AddWithValue("@DeliveryAddress", (object)o.DeliveryAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DeliveryDate",
                o.DeliveryDate.HasValue ? (object)o.DeliveryDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes",           (object)o.Notes ?? DBNull.Value);
        }
    }
}
