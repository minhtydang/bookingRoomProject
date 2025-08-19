namespace DoAn_LTW_Nhom15_22DTHG3.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public Order Order { get; set; }
        public Product Product { get; set; }
    }
}
