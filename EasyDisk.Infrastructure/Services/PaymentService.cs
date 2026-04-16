using EasyDisk.Application.Interfaces;
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
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly string _stripeWebhookSecret;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(UserManager<ApplicationUser> userManager, IConfiguration config, ILogger<PaymentService> logger)
        {
            _userManager = userManager;
            _stripeWebhookSecret = config["Stripe:WebhookSecret"] ?? string.Empty;
            _logger = logger;
        }

        public async Task ProcessWebhookAsync(string json, string signature)
        {
            var stripeEvent = Stripe.EventUtility.ConstructEvent(json, signature, _stripeWebhookSecret);

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompleted(stripeEvent.Data.Object as Session);
                    break;
                case "invoice.paid":
                    await HandleInvoicePaid(stripeEvent.Data.Object as Invoice);
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
                user.SubscriptionPlan = SubscriptionPlans.Free;
                user.MaxStorageBytes = SubscriptionPlans.GetBytesForPlan(SubscriptionPlans.Free);
                user.SubscriptionEndDate = null;
                user.StripeSubscriptionId = null;

                await _userManager.UpdateAsync(user);
                _logger.LogInformation("Subscription cancelled for user {Email}", user.Email);
            }
        }

        private async Task HandleInvoicePaid(Invoice? invoice)
        {
            if (invoice == null)
            {
                return;
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == invoice.CustomerId);
            if (user != null)
            {
                user.SubscriptionEndDate = DateTime.UtcNow.AddMonths(1);
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("Extended subscription for user {Email} due to invoice payment", user.Email);
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
                user.SubscriptionPlan = planName;
                user.MaxStorageBytes = SubscriptionPlans.GetBytesForPlan(planName);
                user.SubscriptionEndDate = DateTime.UtcNow.AddMonths(1);
                user.StripeCustomerId = session.CustomerId;
                user.StripeSubscriptionId = session.SubscriptionId;

                await _userManager.UpdateAsync(user);
                _logger.LogInformation("Updated subscription for user {Email} to plan {PlanName}", user.Email, planName);
            }
        }
    }
}
