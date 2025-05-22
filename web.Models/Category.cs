using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace web.Models
{
	public class Category
	{
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [DisplayName("Category Name")]
        public string? CategoryName { get; set; }
        [Required]
		[Range(1, 1000, ErrorMessage = "Số hiển thị phải trong khoảng 1-1000")]
		public int DisplayOrder { get; set; }
	}
}
