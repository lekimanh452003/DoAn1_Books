using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using web.DataAccess.Data;
using web.DataAccess.Repository.IRepository;
using web.Models;
using web.Models.ViewModels;
using web.Utility;


namespace DoAn1Web.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles=SD.Role_Admin)]
	public class ProductController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IWebHostEnvironment _webHostEnvironment;
		public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
		{
			_unitOfWork = unitOfWork;
			_webHostEnvironment = webHostEnvironment;
		}
		public IActionResult Index()
		{
			List<Product> objProducList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();

			return View(objProducList);
		}
        public IActionResult Detail(int id)
        {
            var objProducList = _unitOfWork.Product.Get(u=>u.Id== id,includeProperties: "Category");
			if(objProducList != null)
			{
				return NotFound();
			}
            return View(objProducList);
        }
        public IActionResult Upsert(int? id)
		{
			ProductVM productVM = new()
			{
				CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
				{
					Text = u.CategoryName,
					Value = u.Id.ToString(),
				}),
				Product = new Product()
			};
			if (id == null || id == 0)
			{
				//create
				return View(productVM);
			}
			else
			{
				//update
				productVM.Product = _unitOfWork.Product.Get(u => u.Id == id);
				return View(productVM);
			}

		}
		[HttpPost]
		public IActionResult Upsert(ProductVM producVM, IFormFile? file)
		{
			if (ModelState.IsValid)
			{
				string wwwRootPath = _webHostEnvironment.WebRootPath;
				if (file != null)
				{
					string filename = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
					string productPath = Path.Combine(wwwRootPath, @"images\product");

					if (!string.IsNullOrEmpty(producVM.Product.ImageUrl))
					{
						var oldImagePath =
							Path.Combine(wwwRootPath, producVM.Product.ImageUrl.TrimStart('\\'));

						if (System.IO.File.Exists(oldImagePath))
						{
							System.IO.File.Delete(oldImagePath);
						}
					}
					using (var fileStream = new FileStream(Path.Combine(productPath, filename), FileMode.Create))
					{
						file.CopyTo(fileStream);
					}
					producVM.Product.ImageUrl = @"\images\product\" + filename;
				}
				if (producVM.Product.Id == 0)
				{
					_unitOfWork.Product.Add(producVM.Product);
				}
				else
				{
					_unitOfWork.Product.Update(producVM.Product);
				}
				_unitOfWork.Save();
                TempData["success"] = "Thêm thành công!";
                return RedirectToAction("Index");
			}
			else
			{
				producVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
				{
					Text = u.CategoryName,
					Value = u.Id.ToString(),
				});
				return View(producVM);
			}

		}

		

		
		#region API CALLS
		[HttpGet]
		public IActionResult GetAll()
		{
            List<Product> objProducList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
			return Json(new {data= objProducList});
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
			var productTobeDelete=_unitOfWork.Product.Get(u=>u.Id == id);
			if (productTobeDelete == null) 
			{ 
				return Json(new {success=false, message="Lỗi khi xóa"});
			}
            var oldImagePath =
                            Path.Combine(_webHostEnvironment.WebRootPath,
							productTobeDelete.ImageUrl.TrimStart('\\'));

            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }
			_unitOfWork.Product.Remove(productTobeDelete);
			_unitOfWork.Save();
            return Json(new { success = false, message = "Xóa thành công!" });

        }

        #endregion
    }
}
