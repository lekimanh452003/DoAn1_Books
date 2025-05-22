using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace web.Utility
{
	public static class SD
	{// gom vào 1 class dễ tổ chức hơn, nếu muốn admin-< administrator thì có thể đổi ở đây 
		public const string Role_Customer= "Customer"; // const là giá trị cố định tại thời điểm biên dịch-> nhanh và hiệu quả hơn biến thông thường
        public const string Role_Admin = "Admin";
        public const string Role_Employee = "Employee";
        // Trạng thái đơn hàng
        public const string StatusPending = "Chờ xác nhận";
        public const string StatusApproved = "Đã xác nhận";
        public const string StatusInProcess = "Đang xử lý";
        public const string StatusShipped = "Đã giao hàng";
        public const string StatusCancelled = "Đã hủy";
        public const string StatusRefunded = "Đã hoàn tiền";

        // Trạng thái thanh toán
        public const string PaymentStatusPending = "Chờ thanh toán";
        public const string PaymentStatusApproved = "Đã thanh toán";
        public const string PaymentStatusRejected = "Từ chối thanh toán";
        public const string PaymentStatusDelayedPayment = "Thanh toán khi nhận hàng";
        public const string PaymentStatusCancelled = "Đã hủy thanh toán";
        public const string PaymentStatusRefunded = "Đã hoàn tiền";

        public const string SessionCart = "SessionShoppingCart"; // tên session
    }
}
