using System;

namespace StroiSnabApp.Models
{
    public class Order
    {
        public int      OrderID         { get; set; }
        public string   OrderNumber     { get; set; }
        public int      ClientID        { get; set; }
        public string   ClientName      { get; set; }   // JOIN
        public string   ClientPhone     { get; set; }   // JOIN
        public int      StatusID        { get; set; }
        public string   StatusName      { get; set; }   // JOIN
        public string   StatusColor     { get; set; }   // JOIN
        public string   DeliveryAddress { get; set; }
        public DateTime CreatedAt       { get; set; }
        public DateTime? DeliveryDate   { get; set; }
        public decimal  TotalAmount     { get; set; }
        public string   Notes           { get; set; }
    }
}
