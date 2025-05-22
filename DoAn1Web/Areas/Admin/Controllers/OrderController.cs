using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using Service;
using System.Security.Claims;
using web.DataAccess.Repository.IRepository;
using web.Models;
using web.Models.ViewModels;
using web.Utility;

namespace DoAn1Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaypalService _paypalService;

        public OrderController(IUnitOfWork unitOfWork, IPaypalService paypalService)
        {
            _unitOfWork = unitOfWork;
            _paypalService = paypalService;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Detail(int orderId)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser");

            if (orderHeader == null)
            {
                return NotFound();
            }
            OrderVM orderVM= new()
            {
                OrderHeader=_unitOfWork.OrderHeader.Get(u=>u.Id==orderId, includeProperties:"ApplicationUser"),
                OrderDetail=_unitOfWork.OrderDetail.GetAll(u=>u.OrderHeaderId==orderId, includeProperties:"Product")
            };
           
            orderVM.CalculateTotals();
            return View(orderVM);
        }
        [HttpPost]
        [Authorize(Roles =SD.Role_Admin+","+ SD.Role_Employee)]
        public IActionResult UpdateOrderDetail(OrderVM orderVM)
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id);
;           orderHeaderFromDb.FullName = orderVM.OrderHeader.FullName;
            orderHeaderFromDb.PhoneNumber = orderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.Address = orderVM.OrderHeader.Address;
            if (!string.IsNullOrEmpty(orderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = orderVM.OrderHeader.Carrier;

            }
            if (!string.IsNullOrEmpty(orderVM.OrderHeader.CheckingNumber))
            {
                orderHeaderFromDb.CheckingNumber = orderVM.OrderHeader.CheckingNumber;

            }
            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();
            TempData["success"] = "Cập nhật chi tiết đơn hàng thành công";

            return RedirectToAction(nameof(Detail),new {orderId=orderHeaderFromDb.Id});
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing(OrderVM orderVM)
        {
            _unitOfWork.OrderHeader.UpdateStatus(orderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["success"] = "Đơn hàng đã được xử lý";
            return RedirectToAction(nameof(Detail), new { orderId = orderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder(OrderVM orderVM)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id);

            // Cập nhật thông tin vận chuyển
            orderHeader.CheckingNumber = orderVM.OrderHeader.CheckingNumber;
            orderHeader.Carrier = orderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;

            // Nếu là thanh toán COD, cập nhật trạng thái thanh toán khi giao hàng
            if (orderHeader.PaymentStatus == SD.PaymentStatusPending)
            {
                orderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
            }
            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();
            TempData["success"] = "Đơn hàng đã được gửi thành công";
            return RedirectToAction(nameof(Detail), new { orderId = orderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public async Task<IActionResult> CancelOrder(OrderVM model)
        {
            var orderId = model.OrderHeader.Id;

            // 1. Lấy OrderHeader từ DB
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId);
            if (orderHeader == null)
            {
                TempData["Error"] = "Đơn hàng không tồn tại.";
                return RedirectToAction(nameof(Detail), new { orderId });
            }
            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved && !string.IsNullOrEmpty(orderHeader.TransactionId))
            {
                try
                {
                    var refundResponse = await _paypalService.RefundCaptureAsync(orderHeader.TransactionId);

                    if (refundResponse != null && refundResponse.Status.ToLower() == "completed")
                    {
                        _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.PaymentStatusRefunded);
                        TempData["Success"] = "Đơn hàng đã được hủy và hoàn tiền thành công.";
                    }
                    else
                    {
                        TempData["Error"] = "Không thể hoàn tiền. Vui lòng kiểm tra lại.";
                        return RedirectToAction(nameof(Detail), new { orderId });
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Lỗi hoàn tiền: {ex.Message}";
                    return RedirectToAction(nameof(Detail), new { orderId });
                }
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.PaymentStatusCancelled);
                TempData["Success"] = "Đơn hàng đã được hủy (chưa thanh toán).";
            }

            _unitOfWork.Save();

            return RedirectToAction(nameof(Detail), new { orderId });
        }
       
        #region API CALLS
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOderHeader;
            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                objOderHeader = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                objOderHeader = _unitOfWork.OrderHeader.
                    GetAll(u=>u.ApplicationUserId==userId,includeProperties:"ApplicationUser");
            }
            switch (status)
            {
                case "inprocess":
                    objOderHeader = objOderHeader.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "paymentpending":
                    objOderHeader = objOderHeader.Where(u => u.PaymentStatus == SD.PaymentStatusPending);
                    break;
                case "completed":
                    objOderHeader = objOderHeader.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOderHeader = objOderHeader.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    // Trả về tất cả đơn hàng
                    break;
            }



            return Json(new { data = objOderHeader });
        }
       
        #endregion
    }
}
