namespace DoAn_LTW_Nhom15_22DTHG3.Models
{
    public class ShoppingCart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public string VoucherCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalBeforeDiscount
        {
            get
            {
                return Items.Sum(i => (i.Price * 0.05m) * i.Quantity);
            }
        }
        public decimal TotalAfterDiscount
        {
            get
            {
                return TotalBeforeDiscount - DiscountAmount;
            }
        }
        public decimal DiscountPercent { get; set; }
        public void AddItem(CartItem item)
        {
            var existingItem = Items.FirstOrDefault(i => i.ProductId ==
            item.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                Items.Add(item);
            }
        }
        public void RemoveItem(int productId)
        {
            Items.RemoveAll(i => i.ProductId == productId);
        }
        // Các phương thức khác...
    }
}
