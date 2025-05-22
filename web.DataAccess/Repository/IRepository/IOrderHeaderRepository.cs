using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using web.Models;

namespace web.DataAccess.Repository.IRepository
{
	public interface IOrderHeaderRepository: IRepository<OrderHeader>
	{
		void Update(OrderHeader obj);
		void UpdateStatus(int id, string orderStatus,string? paymentStatus=null);

		public void UpdatePaypalPaymentId(int id, string paypalOrderId, string transactionId);
    }
}