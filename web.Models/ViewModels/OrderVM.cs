using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace web.Models.ViewModels
{
    public class OrderVM
    {
      
            public OrderHeader OrderHeader { get; set; }
            public IEnumerable<OrderDetail> OrderDetail { get; set; }
            public decimal SubTotal { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal ServiceFee { get; set; }
            public decimal TotalAmount => SubTotal + ServiceFee - DiscountAmount;

        public void CalculateTotals()
        {
            if (OrderDetail == null) return;

            SubTotal = OrderDetail
                .Where(item => item.Product != null)
                .Sum(item => Convert.ToDecimal(item.Product.Price) * item.Count);

            int totalQuantity = OrderDetail.Sum(item => item.Count);

            DiscountAmount = SubTotal * GetDiscountRateByQuantity(totalQuantity);
            ServiceFee = GetServiceFee(SubTotal);
        }

        //  giảm giá theo số lượng sản phẩm
        private decimal GetDiscountRateByQuantity(int quantity)
        {
            if (quantity >= 10)
                return 0.15m; // 15% nếu mua từ 10 sản phẩm trở lên
            else if (quantity >= 7)
                return 0.10m; // 10%
            else if (quantity >= 5)
                return 0.05m; // 5%
            else
                return 0m;
        }

        private decimal GetServiceFee(decimal subtotal)
        {
            if (subtotal >= 500_000)
                return 20_000m;
            else if (subtotal >= 200_000)
                return 10_000m;
            return 5_000m;
        }

    }
    
}
