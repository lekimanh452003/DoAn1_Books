using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using web.DataAccess.Data;
using web.DataAccess.Repository.IRepository;
using web.Models;

namespace web.DataAccess.Repository
{
	public class ProductRepository : Repository<Product>, IProductRepository
	{
		private ApplicationDbContext _db;

		public ProductRepository(ApplicationDbContext db) : base(db)
		{
			_db = db;
		}

        public void Update(Product obj)
        {
            var objFromDb = _db.Products.FirstOrDefault(u => u.Id == obj.Id);
            if (objFromDb != null)
            {
                objFromDb.Title = obj.Title;
                objFromDb.Author = obj.Author;
                objFromDb.ReleaseDate = obj.ReleaseDate;
                objFromDb.Price = obj.Price;
                objFromDb.CategoryId = obj.CategoryId;
                objFromDb.Description = obj.Description;
                if (!string.IsNullOrEmpty(obj.ImageUrl))
                {
                    objFromDb.ImageUrl = obj.ImageUrl;
                }
            }
        }

    }
}
