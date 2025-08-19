using System.ComponentModel.DataAnnotations;
namespace DoAn_LTW_Nhom15_22DTHG3.Models
{
    public class Voucher
    {
        public int Id { get; set; }

        [Required]
        public string Code { get; set; }

        public string Description { get; set; }

        [Range(0, 1, ErrorMessage = "DiscountPercent phải nằm trong khoảng 0 đến 1. Ví dụ: 0.1 là 10%.")]
        public decimal DiscountPercent { get; set; }

        public decimal? MaxDiscountAmount { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
