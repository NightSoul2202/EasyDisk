using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Extensions;
using EasyDisk.Application.Interfaces.Payment;
using EasyDisk.Domain.Constants;
using EasyDisk.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly string _frontendUrl;

        public SubscriptionService(UserManager<ApplicationUser> userManager, IConfiguration config)
        {
            _userManager = userManager;
            _frontendUrl = config["FrontendUrl"] ?? "http://localhost:5173";
        }

        public async Task<string> CreateCheckoutSessionAsync(string userId, string planName)
        {
            var user = await _userManager.FindByIdAsync(userId).EnsureExistsAsync(() => "User not found");

            if (!string.IsNullOrEmpty(user.StripeSubscriptionId))
            {
                return await CreateCustomerPortalSessionAsync(user.StripeCustomerId!);
            }

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
                Customer = user.StripeCustomerId,
                PaymentMethodTypes = new List<string> { "card" },
                CustomerEmail = string.IsNullOrEmpty(user.StripeCustomerId) ? user.Email : null,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceIds[planName],
                        Quantity = 1,
                    },
                },
                Mode = "subscription",

                SuccessUrl = $"{_frontendUrl}/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{_frontendUrl}/pricing",

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

        public async Task<string> CreateCustomerPortalSessionAsync(string stripeCustomerId)
        {
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = stripeCustomerId,
                ReturnUrl = $"{_frontendUrl}/profile",
            };

            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);
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
