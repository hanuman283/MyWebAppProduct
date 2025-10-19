using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MyWebAppProduct.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {

        // Hardcoded in-memory data store
        private static List<Product> _products = new List<Product>
        {
            new Product { Id = 1, Name = "Laptop", Description = "Gaming Laptop", Price = 1500 },
            new Product { Id = 2, Name = "Mouse", Description = "Wireless Mouse", Price = 25 },
            new Product { Id = 3, Name = "Keyboard", Description = "Mechanical Keyboard", Price = 80 }
        };

        // GET: api/products
        [HttpGet]
        public ActionResult<IEnumerable<Product>> GetAll()
        {
            return Ok(_products);
        }

        // GET: api/products/2
        [HttpGet("{id}")]
        public ActionResult<Product> GetById(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            return Ok(product);
        }

        // POST: api/products
        [HttpPost]
        public ActionResult<Product> Create(Product newProduct)
        {
            if (newProduct == null || string.IsNullOrWhiteSpace(newProduct.Name))
                return BadRequest(new { message = "Invalid product data" });

            newProduct.Id = _products.Max(p => p.Id) + 1;
            _products.Add(newProduct);

            return CreatedAtAction(nameof(GetById), new { id = newProduct.Id }, newProduct);
        }

        // PUT: api/products/2
        [HttpPut("{id}")]
        public ActionResult Update(int id, Product updatedProduct)
        {
            var existingProduct = _products.FirstOrDefault(p => p.Id == id);
            if (existingProduct == null)
                return NotFound(new { message = "Product not found" });

            existingProduct.Name = updatedProduct.Name;
            existingProduct.Description = updatedProduct.Description;
            existingProduct.Price = updatedProduct.Price;

            return NoContent(); // Standard 204 response for successful update
        }

        // DELETE: api/products/2
        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            _products.Remove(product);
            return NoContent();
        }
    }
}

