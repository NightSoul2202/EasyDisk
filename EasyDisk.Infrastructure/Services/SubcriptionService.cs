using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Extensions;
using EasyDisk.Application.Interfaces;
using EasyDisk.Domain.Constants;
using EasyDisk.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public SubscriptionService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<string> CreateCheckoutSessionAsync(string userId, string planName)
        {
            var user = await _userManager.FindByIdAsync(userId).EnsureExistsAsync(() => "User not found");

            var priceIds = new Dictionary<string, string>
            {
                { SubscriptionPlans.Basic, "price_1TMRmf84ij9ViWU87Adwbxej" },
                { SubscriptionPlans.Pro, "price_1TMRsT84ij9ViWU8nyFud3H1" },
                { SubscriptionPlans.Premium, "price_1TMRtG84ij9ViWU8R4R5n2WW" }
            };

            if (!priceIds.ContainsKey(planName))
            {
                throw new ValidationException("Invalid plan name");
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                CustomerEmail = user.Email,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceIds[planName],
                        Quantity = 1,
                    },
                },
                Mode = "subscription",

                //Change these URLs to frontend routes later
                SuccessUrl = "http://localhost:5173/success?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = "http://localhost:5173/pricing",

                Metadata = new Dictionary<string, string>
                {
                    { "UserId", user.Id },
                    { "PlanName", planName }
                }
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            return session.Url;
        }

        public async Task CancelSubscriptionAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId).EnsureExistsAsync(() => "User not found");

            if (string.IsNullOrEmpty(user.StripeSubscriptionId))
            {
                throw new ValidationException("No active subscription found");
            }

            var stripeSubscriptionService = new Stripe.SubscriptionService();

            var options = new Stripe.SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            };

            try
            {
                await stripeSubscriptionService.UpdateAsync(user.StripeSubscriptionId, options);
            }
            catch (Stripe.StripeException ex)
            {
                throw new ValidationException($"Failed to cancel subscription: {ex.Message}");
            }
        }
    }
}
