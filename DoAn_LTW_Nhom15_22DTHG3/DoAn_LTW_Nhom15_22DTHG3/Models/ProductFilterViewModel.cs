namespace DoAn_LTW_Nhom15_22DTHG3.Models
{
    public class ProductFilterViewModel
    {
        public string? Region { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinArea { get; set; }
        public int? MaxArea { get; set; }

        public List<Product> Products { get; set; } = new List<Product>();
    }
}
