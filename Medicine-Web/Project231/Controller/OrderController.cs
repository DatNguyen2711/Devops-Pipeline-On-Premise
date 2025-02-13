using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project231.DTO;
using Project231.Models;
using Project231.Services;

namespace Project231.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {

        private readonly ProjectPrn231Context _context;
        private readonly IConfiguration _configuration;
        private readonly TokenService _tokenService;

        private readonly Counter<int> _orderCounter;
        private readonly Counter<int> _orderCanceledCounter;

        public OrderController(ProjectPrn231Context context, IConfiguration configuration, Meter meter)
        {
            _context = context;
            _configuration = configuration;
            _tokenService = new TokenService(_configuration["JWT:Secret"]);

            // Sử dụng Meter từ DI
            _orderCounter = meter.CreateCounter<int>("successful_orders_total", "orders", "Total successful orders");
            _orderCanceledCounter = meter.CreateCounter<int>("cancled_orders_total", "orders", "Total successful orders");

        }


        [HttpPost("PlaceOrder/{userId}")]
        public IActionResult PlaceOrder(int userId, [FromHeader] string Authorization)
        {
            try
            {
                if (string.IsNullOrEmpty(Authorization))
                {
                    return BadRequest(new { message = "Authorization header is missing." });
                }

                var user = _context.Users.FirstOrDefault(u => u.Id == userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                var cartItems = _context.Carts.Where(c => c.UserId == userId).ToList();
                if (cartItems.Count == 0)
                {
                    return BadRequest(new { message = "Cart is empty!" });
                }

                decimal orderTotal = cartItems.Sum(c => c.TotalPrice ?? 0);

                if (user.Fund < orderTotal)
                {
                    return BadRequest(new { message = "Insufficient funds!" });
                }
                user.Fund -= orderTotal;

                var order = new Order
                {
                    UserId = userId,
                    OrderNo = Guid.NewGuid().ToString(),
                    OrderTotal = orderTotal,
                    OrderDate = DateTime.Now,
                    OrderStatus = "Pending"
                };
                _context.Orders.Add(order);
                _context.SaveChanges();
                foreach (var item in cartItems)
                {
                    OrderItem orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        MedicineId = item.MedicineId,
                        UnitPrice = item.UnitPrice,
                        Discount = item.Discount,
                        Quantity = item.Quantity,
                        TotalPrice = item.UnitPrice * item.Quantity,
                    };
                    _context.OrderItems.Add(orderItem);
                }
                _context.Carts.RemoveRange(cartItems);
                _context.SaveChanges();
                _orderCounter.Add(1);
                return Ok(new { message = "Order successfully placed." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred: " + ex.Message });
            }
        }

        [HttpPut("ConfirmOrder/{orderId}")]
        public IActionResult ConfirmOrder(int orderId, [FromHeader] string Authorization)
        {
            try
            {
                if (string.IsNullOrEmpty(Authorization))
                {
                    return BadRequest(new { message = "Authorization header is missing." });
                }

                var token = Authorization.Split(' ')[1];
                var claimsPrincipal = _tokenService.DecodeToken(token);

                var userRole = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
                if (userRole != "Admin")
                {
                    return Unauthorized(new { message = "You do not have permission to perform this action." });
                }

                var order = _context.Orders.Include(o => o.User).FirstOrDefault(o => o.Id == orderId);

                if (order == null)
                {
                    return NotFound(new { message = "Order not found." });
                }

                if (order.OrderStatus == "Confirmed")
                {
                    return BadRequest(new { message = "Order has already been confirmed." });
                }

                var admin = _context.Users.FirstOrDefault(u => u.Type == "Admin");
                if (admin != null)
                {
                    admin.Fund += order.OrderTotal;
                }

                order.OrderStatus = "Confirmed";
                _context.SaveChanges();
                _orderCounter.Add(1);

                return Ok(new { message = "Order confirmed successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while confirming the order: " + ex.Message });
            }
        }

        [HttpGet("GetTopProducts")]
        public IActionResult GetTopProducts()
        {
            var orders = _context.Orders
                .Include(x => x.OrderItems).ThenInclude(x => x.Medicine)
                .Where(x => x.OrderStatus.Equals("Confirmed"))
                .ToList();
            var products = new List<MedicineDTO>();
            Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();

            foreach (var item in orders)
            {
                foreach (var odi in item.OrderItems)
                {
                    if (keyValuePairs.ContainsKey((int)odi.MedicineId))
                    {
                        keyValuePairs[(int)odi.MedicineId]++;
                    }
                    else
                    {
                        keyValuePairs[(int)odi.MedicineId] = 1;
                    }
                }
            }
            keyValuePairs = keyValuePairs.OrderByDescending(x => x.Value)
                .Take(5)
                .ToDictionary();
            foreach (var item in keyValuePairs)
            {
                var p = _context.Medicines
                    .SingleOrDefault(x => x.Id == item.Key);
                if (p != null)
                {
                    products.Add(new MedicineDTO
                    {
                        Manufacturer = p.Manufacturer,
                        Name = p.Name,
                        Quantity = item.Value,
                        Discount = item.Value,
                        UnitPrice = p.UnitPrice,
                        Id = p.Id
                    });
                }
            }
            return Ok(products);
        }

        [HttpPut("DeleteOrder/{orderId}")]
        public IActionResult DeleteOrder(int orderId)
        {
            try
            {
                var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);

                if (order == null)
                {
                    return NotFound(new { message = "Order not found." });
                }
                order.OrderStatus = "Deleted";
                _context.SaveChanges();
                _orderCanceledCounter.Add(1);
                return Ok(new { message = "Order delete successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the order: " + ex.Message });
            }
        }
        [HttpPut("CancelOrder/{orderId}")]
        public IActionResult CancelOrder(int orderId, [FromHeader] string Authorization)
        {
            try
            {
                if (string.IsNullOrEmpty(Authorization))
                {
                    return BadRequest(new { message = "Authorization header is missing." });
                }
                var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);

                if (order == null)
                {
                    return NotFound(new { message = "Order not found." });
                }
                order.OrderStatus = "Canceled";
                _context.SaveChanges();
                return Ok(new { message = "Order cancel successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while Canceling the order: " + ex.Message });
            }
        }
        [HttpGet("ViewOrder/{userId}")]
        public IActionResult ViewOrder(int userId)
        {
            try
            {
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                var orders = _context.Orders.Where(o => o.UserId == userId)
                                            .OrderByDescending(o => o.Id)
                                            .ToList();

                if (orders.Count == 0)
                {
                    return NotFound(new { message = "No orders found for the user." });
                }

                var orderDTOs = orders.Select(order => new OrderDTO
                {
                    Id = order.Id,
                    CustomerName = order.User.FirstName + order.User.LastName,
                    OrderNo = order.OrderNo,
                    OrderDate = order.OrderDate,
                    OrderTotal = order.OrderTotal,
                    OrderStatus = order.OrderStatus
                }).ToList();

                return Ok(orderDTOs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching the orders: " + ex.Message });
            }
        }
        [HttpGet("GetAllOrders")]
        public IActionResult GetAllOrders([FromHeader] string Authorization)
        {
            try
            {
                if (string.IsNullOrEmpty(Authorization))
                {
                    return BadRequest(new { message = "Authorization header is missing." });
                }

                var token = Authorization.Split(' ')[1];
                var claimsPrincipal = _tokenService.DecodeToken(token);

                var userRole = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
                if (userRole != "Admin")
                {
                    return Unauthorized(new { message = "You do not have permission to perform this action." });
                }
                var orders = _context.Orders.Include(o => o.User).ToList();

                var res = orders.Select(x => new OrderDTO()
                {
                    CustomerName = x.User.FirstName + x.User.LastName,
                    Id = x.Id,
                    OrderNo = x.OrderNo,
                    OrderStatus = x.OrderStatus,
                    OrderDate = x.OrderDate,
                    OrderTotal = x.OrderTotal,
                });

                return Ok(res);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching orders: " + ex.Message });
            }
        }
        [HttpGet("ViewOrderDetail/{id}")]
        public IActionResult ViewOrderDetail(int id, [FromHeader] string Authorization)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest(new { message = "Authorization header is missing." });
            }


            var orderItems = _context.OrderItems
                .Include(x => x.Order)
                .Include(x => x.Medicine)
                .Where(x => x.OrderId == id)
                .ToList();
            var res = orderItems.Select(x => new OrderItemDTO()
            {
                Id = x.Id,
                Discount = x.Discount,
                Medicine = new MedicineDTO
                {
                    Name = x.Medicine.Name
                },
                OrderId = x.OrderId,
                TotalPrice = x.TotalPrice,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
            });
            return Ok(res);
        }
    }
}
