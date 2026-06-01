using EasyDisk.Application.Interfaces.Audit;
using EasyDisk.Application.Interfaces.Payment;
using EasyDisk.Domain.Constants;
using EasyDisk.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using Stripe.V2.Core;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly string _stripeWebhookSecret;
        private readonly ILogger<PaymentService> _logger;
        private readonly IAuditService _auditService;

        public PaymentService(UserManager<ApplicationUser> userManager, IConfiguration config, ILogger<PaymentService> logger, IAuditService auditService)
        {
            _userManager = userManager;
            _stripeWebhookSecret = config["Stripe:WebhookSecret"] ?? string.Empty;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task ProcessWebhookAsync(string json, string signature)
        {
            var stripeEvent = Stripe.EventUtility.ConstructEvent(json, signature, _stripeWebhookSecret);

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompleted(stripeEvent.Data.Object as Session);
                    break;
                case "customer.subscription.updated":
                    await HandleSubscriptionUpdated(stripeEvent.Data.Object as Subscription);
                    break;
                case "customer.subscription.deleted":
                    await HandleSubscriptionDeleted(stripeEvent.Data.Object as Subscription);
                    break;
                default:
                    _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                    break;
            }
        }

        private async Task HandleSubscriptionDeleted(Subscription? subscription)
        {
            if (subscription == null)
            {
                return;
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == subscription.CustomerId);
            if (user != null)
            {
                var previousPlan = user.SubscriptionPlan;

                user.SubscriptionPlan = SubscriptionPlans.Free;
                user.MaxStorageBytes = SubscriptionPlans.GetBytesForPlan(SubscriptionPlans.Free);
                user.SubscriptionEndDate = null;
                user.StripeSubscriptionId = null;

                await _userManager.UpdateAsync(user);

                string auditDetails = JsonSerializer.Serialize(new { Reason = "Stripe webhook: subscription deleted", PreviousPlan = previousPlan });

                await _auditService.LogAsync(
                    action: "Subscription.DowngradedToFree",
                    entityType: "Subscription",
                    entityId: user.Id,
                    details: auditDetails,
                    isSuccess: true
                );

                _logger.LogInformation("Subscription cancelled for user {Email}", user.Email);
            }
        }

        private async Task HandleSubscriptionUpdated(Subscription? subscription)
        {
            if (subscription == null) return;

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == subscription.CustomerId);
            if (user != null)
            {
                var priceId = subscription.Items.Data[0].Price.Id;

                var newPlan = MapPriceIdToPlanName(priceId);

                user.SubscriptionPlan = newPlan;
                user.MaxStorageBytes = SubscriptionPlans.GetBytesForPlan(newPlan);
                await _userManager.UpdateAsync(user);

                string auditDetails = JsonSerializer.Serialize(new { NewPlan = newPlan });

                await _auditService.LogAsync(
                    action: "Subscription.Updated",
                    entityType: "Subscription",
                    entityId: user.Id,
                    details: auditDetails,
                    isSuccess: true
                );
            }
        }

        private async Task HandleCheckoutSessionCompleted(Session? session)
        {
            if (session == null || session.PaymentStatus != "paid")
            {
                return;
            }

            var userId = session.Metadata["UserId"];
            var planName = session.Metadata["PlanName"];

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var previousPlan = user.SubscriptionPlan;

                user.SubscriptionPlan = planName;
                user.MaxStorageBytes = SubscriptionPlans.GetBytesForPlan(planName);
                user.SubscriptionEndDate = DateTime.UtcNow.AddMonths(1);
                user.StripeCustomerId = session.CustomerId;
                user.StripeSubscriptionId = session.SubscriptionId;

                await _userManager.UpdateAsync(user);

                string auditDetails = JsonSerializer.Serialize(new { NewPlan = planName, PreviousPlan = previousPlan, StripeSessionId = session.Id });

                await _auditService.LogAsync(
                    action: "Subscription.Purchased",
                    entityType: "Subscription",
                    entityId: user.Id,
                    details: auditDetails,
                    isSuccess: true
                );

                _logger.LogInformation("Updated subscription for user {Email} to plan {PlanName}", user.Email, planName);
            }
        }

        private string MapPriceIdToPlanName(string priceId)
        {
            return priceId switch
            {
                "price_1TMRmf84ij9ViWU87Adwbxej" => SubscriptionPlans.Basic,
                "price_1TMRsT84ij9ViWU8nyFud3H1" => SubscriptionPlans.Pro,
                "price_1TMRtG84ij9ViWU8R4R5n2WW" => SubscriptionPlans.Premium,
                _ => SubscriptionPlans.Free
            };
        }
    }
}