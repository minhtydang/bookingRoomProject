using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAn_LTW_Nhom15_22DTHG3.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Range(0.01, 1000000000)]
        public decimal Price { get; set; }

        public string Description { get; set; }

        public string? ImageUrl { get; set; } // Ảnh chính
        public string? ExtraImageUrls { get; set; } // Các ảnh phụ, ngăn cách bằng dấu phẩy
        public string? VideoUrls { get; set; } // Các video từ file, ngăn cách bằng dấu phẩy
        public string? ExternalVideoUrls { get; set; } // Các URL video nhập tay, ngăn cách bằng dấu phẩy (mới thêm)

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; } // Ảnh chính upload

        [NotMapped]
        public List<IFormFile>? ExtraImageFiles { get; set; } // Ảnh phụ upload

        [NotMapped]
        public List<IFormFile>? VideoFiles { get; set; } // Video từ file upload

        [NotMapped]
        public List<string>? ExternalVideoUrlList { get; set; } // Danh sách URL video nhập tay (mới thêm)

        [NotMapped]
        public List<string>? ImagesToDelete { get; set; } // Ảnh phụ muốn xóa

        // Thêm thông tin vị trí và diện tích 
        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? Region { get; set; } // Khu vực 

        public double? Area { get; set; } // Diện tích (m²)

        public double? Latitude { get; set; } // Vĩ độ
        public double? Longitude { get; set; } // Kinh độ
    }
}
