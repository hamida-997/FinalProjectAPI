using FinalProjectAPI.Helpers.DTOs.Discount;
using FinalProjectAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DiscountsController : ControllerBase
    {
        // Method-level role checks ADD to class-level ones, so the admin role is set
        // per action instead of on the class (otherwise "validate" would be admin-only too)
        private const string AdminRoles = "SuperAdmin,Admin";

        private readonly IDiscountService _discountService;

        public DiscountsController(IDiscountService discountService)
        {
            _discountService = discountService;
        }

        // POST: api/Discounts/validate  (any logged-in user, for the checkout preview)
        [HttpPost("validate")]
        public async Task<ActionResult<DiscountValidationResultDto>> Validate(ValidateDiscountDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 200 with IsValid=false for a bad code, so the page can show the reason
                return Ok(await _discountService.ValidateForItemsAsync(dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/Discounts
        [HttpGet]
        [Authorize(Roles = AdminRoles)]
        public async Task<ActionResult<IEnumerable<DiscountCodeDto>>> GetAll()
        {
            return Ok(await _discountService.GetAllAsync());
        }

        // GET: api/Discounts/5
        [HttpGet("{id:int}")]
        [Authorize(Roles = AdminRoles)]
        public async Task<ActionResult<DiscountCodeDto>> GetById(int id)
        {
            var code = await _discountService.GetByIdAsync(id);
            if (code == null)
            {
                return NotFound(new { message = $"Discount code with ID {id} not found." });
            }

            return Ok(code);
        }

        // POST: api/Discounts
        [HttpPost]
        [Authorize(Roles = AdminRoles)]
        public async Task<ActionResult<DiscountCodeDto>> Create(SaveDiscountCodeDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var created = await _discountService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/Discounts/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = AdminRoles)]
        public async Task<ActionResult<DiscountCodeDto>> Update(int id, SaveDiscountCodeDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updated = await _discountService.UpdateAsync(id, dto);
                if (updated == null)
                {
                    return NotFound(new { message = $"Discount code with ID {id} not found." });
                }

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/Discounts/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = AdminRoles)]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _discountService.DeleteAsync(id))
            {
                return NotFound(new { message = $"Discount code with ID {id} not found." });
            }

            return NoContent();
        }
    }
}
