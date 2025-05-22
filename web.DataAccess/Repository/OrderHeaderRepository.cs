using Microsoft.EntityFrameworkCore;
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
	public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
	{
		private ApplicationDbContext _db;

		public OrderHeaderRepository(ApplicationDbContext db) : base(db)
		{
			_db = db;
		}
        public void Update(OrderHeader obj)
        {
            _db.OrderHeader.Update(obj);
        }
        public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
		{
			var objFromDb = _db.OrderHeader.FirstOrDefault(u => u.Id == id);
			if(objFromDb!= null)
			{
				objFromDb.OrderStatus = orderStatus;
                if (!string.IsNullOrEmpty(paymentStatus))
                {
                    objFromDb.PaymentStatus = paymentStatus;
                }
            }
		}
        public void UpdatePaypalPaymentId(int id, string paypalOrderId, string transactionId)
        {
            var orderFromDb = _db.OrderHeader.FirstOrDefault(u => u.Id == id);
            if (orderFromDb != null)
            {
                if (!string.IsNullOrEmpty(paypalOrderId))
                {
                    orderFromDb.PaypalOrderId = paypalOrderId;
                    if (!string.IsNullOrEmpty(transactionId))
                    {
                        orderFromDb.TransactionId = transactionId;
                        orderFromDb.PaymentDate = DateTime.Now;
                    }
                }
            }
        }
    }
}
