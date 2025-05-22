
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using web.Models;

namespace web.DataAccess.Data
{
	public class ApplicationDbContext : IdentityDbContext<IdentityUser>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
		{
		}
		public DbSet<Category> Categories { get; set; }
		public DbSet<Product> Products { get; set; }
		public DbSet<ApplicationUser> applicationUsers { get; set; }
		public DbSet<ShoppingCart> ShoppingCarts { get; set; }
		public DbSet<OrderHeader> OrderHeader { get; set; }
		public DbSet<OrderDetail> OrderDetail { get; set; }
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, CategoryName = "Fiction", DisplayOrder = 1 },
                new Category { Id = 2, CategoryName = "Non-fiction", DisplayOrder = 2 },
                new Category { Id = 3, CategoryName = "Science", DisplayOrder = 3 },
                new Category { Id = 4, CategoryName = "History", DisplayOrder = 4 },
                new Category { Id = 5, CategoryName = "Children", DisplayOrder = 5 }
            );

            modelBuilder.Entity<Product>().HasData(
              new Product { Id = 1, Title = "The Midnight Library", Author = "Matt Haig", Price = 200000, ReleaseDate = new DateTime(2020, 8, 13), CategoryId = 1, ImageUrl = "", Description = "A novel about the choices that go into a life well lived." },
              new Product { Id = 2, Title = "Educated", Author = "Tara Westover", Price = 250000, ReleaseDate = new DateTime(2018, 2, 20), CategoryId = 2, ImageUrl = "", Description = "A memoir about a woman who grew up in a strict family and sought education." },
              new Product { Id = 3, Title = "A Brief History of Time", Author = "Stephen Hawking", Price = 300000, ReleaseDate = new DateTime(1988, 4, 1), CategoryId = 3, ImageUrl = "", Description = "Explores fundamental questions about the universe." },
              new Product { Id = 4, Title = "Sapiens", Author = "Yuval Noah Harari", Price = 280000, ReleaseDate = new DateTime(2011, 1, 1), CategoryId = 4, ImageUrl = "", Description = "A brief history of humankind." },
              new Product { Id = 5, Title = "Harry Potter and the Philosopher's Stone", Author = "J.K. Rowling", Price = 220000, ReleaseDate = new DateTime(1997, 6, 26), CategoryId = 5, ImageUrl = "", Description = "The first book in the Harry Potter series." },
              new Product { Id = 6, Title = "The Power of Habit", Author = "Charles Duhigg", Price = 180000, ReleaseDate = new DateTime(2012, 2, 28), CategoryId = 2, ImageUrl = "", Description = "How habits shape our lives and how to change them." },
              new Product { Id = 7, Title = "Thinking, Fast and Slow", Author = "Daniel Kahneman", Price = 350000, ReleaseDate = new DateTime(2011, 10, 25), CategoryId = 3, ImageUrl = "", Description = "Insights into human thinking and decision-making." },
              new Product { Id = 8, Title = "The Cat in the Hat", Author = "Dr. Seuss", Price = 150000, ReleaseDate = new DateTime(1957, 3, 12), CategoryId = 5, ImageUrl = "", Description = "A classic children's book." },
              new Product { Id = 9, Title = "1984", Author = "George Orwell", Price = 210000, ReleaseDate = new DateTime(1949, 6, 8), CategoryId = 1, ImageUrl = "", Description = "A dystopian novel about totalitarianism." }
          );

        }
    }
}
