using System.ComponentModel.DataAnnotations;

namespace DoAn_LTW_Nhom15_22DTHG3.Models
{
    public class PaymentModel
    {
        [Required]
        public string OrderId { get; set; }
        [Required]
        public string Amount { get; set; }
        [Required]
        public string OrderInfo { get; set; }
    }
} 