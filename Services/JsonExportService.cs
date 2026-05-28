using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using StroiSnabApp.Models;

namespace StroiSnabApp.Services
{
    public class JsonExportService
    {
        public void ExportOrders(List<Order> orders, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"exportDate\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",");
            sb.AppendLine($"  \"company\":    \"ООО ЦентрТрансГранит — Поставки строительных материалов\",");
            sb.AppendLine($"  \"totalCount\": {orders.Count},");
            sb.AppendLine($"  \"totalAmount\": {SumAmount(orders)},");
            sb.AppendLine("  \"orders\": [");

            for (int i = 0; i < orders.Count; i++)
            {
                var o = orders[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"orderID\":         {o.OrderID},");
                sb.AppendLine($"      \"orderNumber\":     \"{Esc(o.OrderNumber)}\",");
                sb.AppendLine($"      \"client\":          \"{Esc(o.ClientName)}\",");
                sb.AppendLine($"      \"clientPhone\":     \"{Esc(o.ClientPhone)}\",");
                sb.AppendLine($"      \"status\":          \"{Esc(o.StatusName)}\",");
                sb.AppendLine($"      \"deliveryAddress\": \"{Esc(o.DeliveryAddress)}\",");
                sb.AppendLine($"      \"totalAmount\":     {o.TotalAmount},");
                sb.AppendLine($"      \"createdAt\":       \"{o.CreatedAt:yyyy-MM-dd}\",");
                sb.AppendLine($"      \"deliveryDate\":    \"{(o.DeliveryDate.HasValue ? o.DeliveryDate.Value.ToString("yyyy-MM-dd") : "")}\",");
                sb.AppendLine($"      \"notes\":           \"{Esc(o.Notes)}\"");
                sb.Append("    }");
                sb.AppendLine(i < orders.Count - 1 ? "," : "");
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private decimal SumAmount(List<Order> orders)
        {
            decimal sum = 0;
            foreach (var o in orders) sum += o.TotalAmount;
            return sum;
        }

        private string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
