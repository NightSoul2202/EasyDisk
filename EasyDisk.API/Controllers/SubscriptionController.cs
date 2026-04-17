using EasyDisk.API.Filters;
using EasyDisk.Application.DTOs;
using EasyDisk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyDisk.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ICurrentUserService _currentUserService;

        public SubscriptionController(ISubscriptionService subscriptionService, ICurrentUserService currentUserService)
        {
            _subscriptionService = subscriptionService;
            _currentUserService = currentUserService;
        }

        [HttpPost]
        [Route("create-checkout-session")]
        [Audit("Subscription.Create", "User")]
        public async Task<IActionResult> CreateCheckoutSession([FromQuery] CheckoutRequestDto checkoutRequest)
        {
            var userId = _currentUserService.UserId ?? throw new Exception("User must be authenticated to create a subscription.");

            var sessionUrl = await _subscriptionService.CreateCheckoutSessionAsync(userId, checkoutRequest.PlanName);

            return Ok(new { Url = sessionUrl });
        }

        [HttpPost]
        [Route("cancel")]
        [Audit("Subscription.Cancel", "User")]
        public async Task<IActionResult> CancelSubscription()
        {
            var userId = _currentUserService.UserId ?? throw new Exception("User must be authenticated to cancel a subscription.");

            await _subscriptionService.CancelSubscriptionAsync(userId);

            return Ok(new { message = "Subscription cancelled successfully." });
        }
    }
}
