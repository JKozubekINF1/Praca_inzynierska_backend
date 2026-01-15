using Market.DTOs;
using Market.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Market.Controllers
{
    [Route("api/transaction")]
    [ApiController]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public TransactionController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("balance")]
        public async Task<ActionResult<decimal>> GetBalance()
        {
            var balance = await _orderService.GetBalanceAsync(GetUserId());
            return Ok(new { balance });
        }

        [HttpPost("top-up")]
        public async Task<ActionResult> TopUp([FromBody] TopUpDto dto)
        {
            try
            {
                var newBalance = await _orderService.TopUpAsync(GetUserId(), dto.Amount);
                return Ok(new { newBalance });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("buy")]
        public async Task<ActionResult> BuyItem([FromBody] CreateOrderDto dto)
        {
            try
            {
                var result = await _orderService.CreateOrderAsync(GetUserId(), dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("history")] 
        public async Task<ActionResult<IEnumerable<OrderHistoryDto>>> GetHistory()
        {
            var history = await _orderService.GetUserOrdersAsync(GetUserId());
            return Ok(history);
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
    }
}