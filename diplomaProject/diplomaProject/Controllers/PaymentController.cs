using System.Security.Claims;
using diplomaProject.Data;
using diplomaProject.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace diplomaProject.Controllers;

[Route("api/[controller]")]
public class PaymentController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly IProgressService _progressService;

    public PaymentController(IConfiguration configuration, AppDbContext context, IProgressService progressService)
    {
        _configuration = configuration;
        _context = context;
        _progressService = progressService;
    }

    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession(int courseId) //Створення оплати
    {
        var domain = $"{Request.Scheme}://{Request.Host}";
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine($"CREATE SESSION userId: '{userId}'");
        var registration = await _context.CourseRegistrations
            .FirstOrDefaultAsync(r => r.UserId == userId && r.CourseId == courseId);
        if (registration == null) return BadRequest("Registration not found");
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            ClientReferenceId = registration.Id.ToString(),
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = 450000, // 4500.00 грн (в копійках)
                        Currency = "uah",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Повний доступ до курсу",
                            Description = "Відкриває всі модулі та лекції"
                        }
                    },
                    Quantity = 1
                }
            },
            Mode = "payment",
            // Метадані допомагають ідентифікувати замовлення у Webhook
            Metadata = new Dictionary<string, string>
            {
                { "courseId", courseId.ToString() }
                //{ "userId", User.FindFirstValue(ClaimTypes.NameIdentifier) }
                //{ "userId", userId }
            },
            SuccessUrl = domain + "/api/Payment/Success",
            CancelUrl = domain + "/User/Dashboard?courseId=" + courseId
        };

        try
        {
            var service = new SessionService();
            var session = await service.CreateAsync(options);

        
            return Json(new { url = session.Url });
        }
        catch (StripeException e)
        {
            Console.WriteLine("STRIPE ERROR: " + e.Message);
            return BadRequest(e.Message);
        }
    }




    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook() //Підтвердження оплати
    {

        var json = await new StreamReader(Request.Body).ReadToEndAsync();
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _configuration["Stripe:WebhookSecret"]
            );

            // КРИТИЧНО: Обробляємо ТІЛЬКИ завершену сесію
            if (stripeEvent.Type == "checkout.session.completed")
            {
                
                Console.WriteLine($"Stripe-Signature: {Request.Headers["Stripe-Signature"]}");
                var session = stripeEvent.Data.Object as Session;
                Console.WriteLine($"Metadata count: {session?.Metadata?.Count}");

                var registrationIdStr = session?.ClientReferenceId;
                Console.WriteLine($"ClientReferenceId: '{registrationIdStr}'");

                if (!int.TryParse(registrationIdStr, out var registrationId))
                {
                    Console.WriteLine(" Invalid registrationId");
                    return Ok();
                }

                var registration = await _context.CourseRegistrations
                    .FirstOrDefaultAsync(r => r.Id == registrationId);

                if (registration == null)
                {
                    Console.WriteLine(" Registration not found");
                    return Ok();
                }

                registration.IsPaid = true;
                registration.PaymentDate = DateTime.Now;


                var courseId = registration.CourseId;
                var userId = registration.UserId;

                await _progressService.SyncProgressAfterPayment(userId, courseId);
                Console.WriteLine("--- WEBHOOK SUCCESS: ALL DATA SAVED IN ONE TRANSACTION ---");

                Console.WriteLine(" DB UPDATED SUCCESSFULLY");

                
            }


            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Webhook Error: {ex.Message}");
            return Ok();
        }
    }

    [HttpGet("Success")]
    public IActionResult Success()
    {
        return View();
    }
}