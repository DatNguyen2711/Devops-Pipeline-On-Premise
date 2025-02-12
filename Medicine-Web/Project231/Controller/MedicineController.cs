using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Project231.DTO;
using Project231.Models;
using Project231.Services;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;

namespace Project231.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class MedicineController : ControllerBase
    {
        private readonly ProjectPrn231Context _context;
        private readonly TokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly Meter _meter;
        private readonly Counter<int> _medicineSalesCounter;

        public MedicineController(ProjectPrn231Context context)
        {
            _context = context;

            // Khởi tạo Meter và Counter từ OpenTelemetry
            var meterProvider = new Meter("CustomMetrics", "1.0.0");
            _meter = meterProvider;
            _medicineSalesCounter = _meter.CreateCounter<int>("medicine_sales_total", "count", "Total number of medicines sold");
        }


        [HttpGet("GetAllMedicines")]
        public IActionResult GetAllMedicines([FromHeader] string Authorization)
        {
            try
            {
                if (string.IsNullOrEmpty(Authorization))
                {
                    return BadRequest(new { message = "Authorization header is missing." });
                }
                var medicines = _context.Medicines.ToList();

                var medicineDTOs = medicines.Select(m => new MedicineDTO
                {
                    Id = m.Id,
                    Name = m.Name,
                    Manufacturer = m.Manufacturer,
                    UnitPrice = m.UnitPrice,
                    Discount = m.Discount,
                    Quantity = m.Quantity,
                    ExpDate = m.ExpDate,
                    ImageUrl = m.ImageUrl,
                    Status = m.Status
                }).ToList();

                return Ok(medicineDTOs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching medicines: " + ex.Message });
            }
        }


        [HttpPost("CreateNewMedicine")]
        public IActionResult CreateNewMedicine(MedicineDTO medicine, [FromHeader] string Authorization)
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
                Medicine medicine1 = new Medicine()
                {
                    Id = medicine.Id,
                    Name = medicine.Name,
                    Manufacturer = medicine.Manufacturer,
                    UnitPrice = medicine.UnitPrice,
                    Discount = medicine.Discount,
                    Quantity = medicine.Quantity,
                    ExpDate = medicine.ExpDate,
                    ImageUrl = medicine.ImageUrl,
                    Status = 1
                };
                _context.Medicines.Add(medicine1);
                _context.SaveChanges();
                return Ok(medicine1);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching medicines: " + ex.Message });
            }
        }
        [HttpPut("UpdateMedicine/{medicineId}")]
        public IActionResult UpdateMedicine(int medicineId, MedicineDTO updatedMedicine, [FromHeader] string Authorization)
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
                var medicine = _context.Medicines.FirstOrDefault(m => m.Id == medicineId);
                if (medicine == null)
                {
                    return NotFound(new { message = "Medicine not found." });
                }
                medicine.Name = updatedMedicine.Name;
                medicine.Manufacturer = updatedMedicine.Manufacturer;
                medicine.UnitPrice = updatedMedicine.UnitPrice;
                medicine.Discount = updatedMedicine.Discount;
                medicine.Quantity = updatedMedicine.Quantity;
                medicine.ExpDate = updatedMedicine.ExpDate;
                medicine.ImageUrl = updatedMedicine.ImageUrl;


                _context.Medicines.Update(medicine);
                _context.SaveChanges();

                return Ok(new { message = "Medicine updated successfully.", medicine });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating medicine: " + ex.Message });
            }
        }


        [HttpPut("DeleteMedicine/{id}")]
        public IActionResult DeleteMedicine(int id, [FromHeader] string Authorization)
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
                var medicine = _context.Medicines.FirstOrDefault(u => u.Id == id);

                if (medicine == null)
                {
                    return NotFound(new { message = "medicine not found." });
                }

                medicine.Status = 0;
                _context.SaveChanges();

                return Ok(new { message = "medicine deleted successfully.", medicine });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while blocking the user: " + ex.Message });
            }
        }

        [HttpPost("CompleteSale")]
        public IActionResult CompleteSale(Medicine model, [FromHeader] string Authorization)
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
                if (userRole != "Admin" && userRole != "Sales") // Kiểm tra quyền của người dùng
                {
                    return Unauthorized(new { message = "You do not have permission to perform this action." });
                }

                // Lấy thông tin thuốc từ model
                var medicine = _context.Medicines.FirstOrDefault(m => m.Name == model.Name);
                if (medicine == null)
                {
                    return NotFound(new { message = "Medicine not found." });
                }

                // Giảm số lượng thuốc trong kho khi bán
                if (medicine.Quantity >= model.Quantity)
                {
                    medicine.Quantity -= model.Quantity; // Cập nhật số lượng thuốc trong kho
                    _context.SaveChanges();

                    // Ghi lại số lượng thuốc bán được vào metrics
                    _medicineSalesCounter.Add(model.Quantity ?? 0, new KeyValuePair<string, object>("medicine_name", medicine.Name));

                    return Ok(new { message = "Sale completed successfully.", medicine });
                }
                else
                {
                    return BadRequest(new { message = "Not enough stock available." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while completing the sale: " + ex.Message });
            }




        }
    }
}