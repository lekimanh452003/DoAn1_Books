using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;
using System.Security.Claims;
using web.DataAccess.Repository.IRepository;
using web.Models;
using web.Models.ViewModels;
using web.Utility;
using Newtonsoft.Json;
using System.Text.Json;
using PayPalCheckoutSdk.Orders;

namespace DoAn1Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaypalService _paypalService;
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IUnitOfWork unitOfWork, IPaypalService paypalService)
        {
            _unitOfWork = unitOfWork;
            _paypalService = paypalService;
        }

        public IActionResult Index()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            var cartItems = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product");
            var vm = new ShoppingCartVM
            {
                ShoppingCartList = cartItems,
            };
            vm.CalculateTotals();
            return View(vm);
        }
        [HttpPost]
        public IActionResult Index(ShoppingCartVM cartVm)
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            cartVm.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
               includeProperties: "Product");
            if (!ModelState.IsValid)
            {
                return View(cartVm);
            }
            cartVm.CalculateTotals();
            return View(cartVm);
        }
        // Xử lý hành động cộng số lượng sản phẩm trong giỏ
        public IActionResult Plus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            cartFromDb.Count += 1;
            _unitOfWork.ShoppingCart.Update(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        // Xử lý hành động trừ số lượng sản phẩm trong giỏ
        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId, tracked: true);

            if (cartFromDb.Count <= 1)
            {// xóa khỏi giỏ hàng
                HttpContext.Session.SetInt32(SD.SessionCart,
                   _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
            }
            else
            {
                cartFromDb.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        // Xử lý hành động xóa sản phẩm khỏi giỏ
        public IActionResult Remove(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId, tracked: true);
            _unitOfWork.ShoppingCart.Remove(cartFromDb);
            HttpContext.Session.SetInt32(SD.SessionCart,
                    _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult UpdateRentalDays(int rentalDays)
        {
            // Redirect lại Index với số ngày thuê được chọn
            return RedirectToAction(nameof(Index), new { rentalDays = rentalDays });
        }

        public IActionResult Sumary(int rentalDays = 1)
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            var cartItems = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product");
            var vm = new ShoppingCartVM
            {
                ShoppingCartList = cartItems,
                OrderHeader = new()
                {
                    ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId)
                }
            };
            vm.CalculateTotals();

            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Sumary")]
        public async Task<IActionResult> Sumary(ShoppingCartVM shoppingCartVM)
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            shoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");

            shoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;

            shoppingCartVM.OrderHeader.ApplicationUserId = userId;

            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
            shoppingCartVM.CalculateTotals();
           

            shoppingCartVM.OrderHeader = new OrderHeader
            {
                ApplicationUserId = userId,
                OrderDate = DateTime.Now,
                ShippingDate = DateTime.Now.AddDays(1),
                OrderTotal = (double)shoppingCartVM.TotalAmount,
                OrderStatus = SD.StatusPending,
                PaymentStatus = SD.PaymentStatusPending,
                FullName = shoppingCartVM.OrderHeader.ApplicationUser.FullName,
                PhoneNumber = shoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber,
                Address = shoppingCartVM.OrderHeader.ApplicationUser.Address
            };
            shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            _unitOfWork.OrderHeader.Add(shoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            // Tạo các bản ghi OrderDetail
            foreach (var cartItem in shoppingCartVM.ShoppingCartList)
            {
                var product = _unitOfWork.Product.Get(p => p.Id == cartItem.ProductId);
                OrderDetail detail = new OrderDetail
                {
                    ProductId = cartItem.ProductId,
                    OrderHeaderId = shoppingCartVM.OrderHeader.Id,
                    Price = (double)cartItem.Product.Price,
                    Count = cartItem.Count
                };

                _unitOfWork.OrderDetail.Add(detail);

            }

            _unitOfWork.Save();
            // Tạo PayPal Order
            var returnUrl = Url.Action("PaymentSuccess", "Cart", new { orderId = shoppingCartVM.OrderHeader.Id }, Request.Scheme);
            var cancelUrl = Url.Action("PaymentCancelled", "Cart", new { orderId = shoppingCartVM.OrderHeader.Id }, Request.Scheme);

            try
            {
                var paypalOrderResponse = await _paypalService.CreateOrderAsync(shoppingCartVM.OrderHeader, returnUrl, cancelUrl);

                // Lưu PayPal OrderId
                _unitOfWork.OrderHeader.UpdatePaypalPaymentId(shoppingCartVM.OrderHeader.Id, paypalOrderResponse.Id, null);
                _unitOfWork.Save();

                // Chuyển hướng đến trang thanh toán PayPal
                var approvalLink = paypalOrderResponse.Links.FirstOrDefault(x => x.Rel == "approve");
                if (approvalLink != null)
                {
                    return Redirect(approvalLink.Href);
                }
                else
                {
                    TempData["error"] = "Không tìm thấy đường dẫn thanh toán PayPal.";
                    return RedirectToAction("Index", "Home");
                }

            }
            catch (Exception ex)
            {
                // Xử lý lỗi
                TempData["error"] = "Có lỗi xảy ra khi tạo đơn hàng PayPal: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }
        public IActionResult PaymentSuccess(string token, string PayerID)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.PaypalOrderId == token);

            if (orderHeader == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Home");
            }


            var result = _paypalService.CaptureOrderAsync(token).GetAwaiter().GetResult();

            if (result.Status == "COMPLETED")
            {
                var captureId = result.PurchaseUnits?
                    .FirstOrDefault()?
                    .Payments?.Captures?
                    .FirstOrDefault()?.Id;

                if (string.IsNullOrEmpty(captureId))
                {
                    TempData["Error"] = "⚠️ Capture ID không tồn tại trong phản hồi PayPal";
                    Console.WriteLine("⚠️ JSON phản hồi:\n" + System.Text.Json.JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                    return RedirectToAction("Index", "Home");
                }

                _unitOfWork.OrderHeader.UpdatePaypalPaymentId(orderHeader.Id, token, captureId);
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusApproved, SD.PaymentStatusApproved);
                _unitOfWork.Save();

                // Xóa giỏ hàng
                var cart = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
                _unitOfWork.ShoppingCart.RemoveRange(cart);
                _unitOfWork.Save();

                return View(orderHeader.Id); // => hoặc truyền viewModel nếu cần
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.PaymentStatusRejected);
                _unitOfWork.Save();
                TempData["Error"] = "Thanh toán thất bại.";
                return RedirectToAction("Index", "Home");
            }
        }



        public IActionResult PaymentCancelled(int orderId)
        {
            // Hủy đơn hàng
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId);
            if (orderHeader != null)
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.PaymentStatusRejected);
                _unitOfWork.Save();
            }

            TempData["error"] = "Thanh toán đã bị hủy";
            return RedirectToAction(nameof(Index));
        }
       
    }
}
