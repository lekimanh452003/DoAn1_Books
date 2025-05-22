using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace web.Models.ViewModels
{
    public class ShoppingCartVM
    {
        public IEnumerable<ShoppingCart> ShoppingCartList { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Notes { get; set; }

        // Giá trị được tính toán từ phương thức CalculateTotals
        public decimal SubTotal { get; private set; }
        public decimal ServiceFee { get; private set; }
        public decimal DiscountRate { get; private set; }
        public decimal DiscountAmount { get; private set; }

        // Tổng cộng là thuộc tính được tính toán từ các giá trị khác
        public decimal TotalAmount => SubTotal - DiscountAmount + ServiceFee;

        // Thông tin đơn hàng
        public OrderHeader OrderHeader { get; set; }

  
        public void CalculateTotals()
        {
            if (ShoppingCartList == null || !ShoppingCartList.Any())
            {
                ResetValues();
                return;
            }

            // Tính tổng tiền hàng
            SubTotal = ShoppingCartList
                .Where(item => item.Product != null)
                .Sum(item => Convert.ToDecimal(item.Product.Price) * item.Count);

            // Tính số lượng sản phẩm để xác định tỷ lệ giảm giá
            int totalQuantity = ShoppingCartList.Sum(item => item.Count);

            // Xác định tỷ lệ giảm giá
            DiscountRate = GetDiscountRateByQuantity(totalQuantity);

            // Tính số tiền được giảm
            DiscountAmount = SubTotal * DiscountRate;

            // Xác định phí dịch vụ
            ServiceFee = GetServiceFee(SubTotal);
        }

      
        private void ResetValues()
        {
            SubTotal = 0;
            DiscountRate = 0;
            DiscountAmount = 0;
            ServiceFee = 5_000m; // Mức phí dịch vụ tối thiểu
        }


        private decimal GetDiscountRateByQuantity(int quantity)
        {
            if (quantity >= 10)
                return 0.15m; // 15% nếu mua từ 10 sản phẩm trở lên
            else if (quantity >= 7)
                return 0.10m; // 10% nếu mua từ 7-9 sản phẩm
            else if (quantity >= 5)
                return 0.05m; // 5% nếu mua từ 5-6 sản phẩm
            else
                return 0m; // Không giảm giá cho dưới 5 sản phẩm
        }

        private decimal GetServiceFee(decimal subtotal)
        {
            if (subtotal >= 500_000m)
                return 20_000m;
            else if (subtotal >= 200_000m)
                return 10_000m;
            return 5_000m; // Mức phí dịch vụ tối thiểu
        }
    }
}