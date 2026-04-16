using diplomaProject.Data;
using diplomaProject.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;
using System.IO;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using Microsoft.EntityFrameworkCore;



namespace diplomaProject.Controllers
{
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

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = 50000, // 500.00 грн (в копійках)
                        Currency = "uah",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Повний доступ до курсу",
                            Description = "Відкриває всі модулі та лекції"
                        },
                    },
                    Quantity = 1,
                },
            },
                Mode = "payment",
                // Метадані допомагають ідентифікувати замовлення у Webhook
                Metadata = new Dictionary<string, string>
                {
                { "courseId", courseId.ToString() },
                { "userId", User.FindFirstValue(ClaimTypes.NameIdentifier) }
            },
                SuccessUrl = domain + "/api/Payment/Success",
                CancelUrl = domain + "/User/Dashboard?courseId=" + courseId,
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            //return Ok(new StripeResponseDTO
            //{
            //    SessionId = session.Id,
            //    PubKey = _configuration["Stripe:PublishableKey"]
            //});
            return Json(new { url = session.Url });
        }

        //[HttpPost("webhook")]
        //public async Task<IActionResult> Webhook()
        //{
        //    var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        //    try
        //    {
        //        var stripeEvent = EventUtility.ConstructEvent(
        //            json,
        //            Request.Headers["Stripe-Signature"],
        //            _configuration["Stripe:WebhookSecret"]
        //        );

        //        if (stripeEvent.Type == "checkout.session.completed")
        //        {
        //            var session = stripeEvent.Data.Object as Session;
        //            if (session.Metadata.ContainsKey("courseId") && session.Metadata.ContainsKey("userId"))
        //            {
        //                var userId = session.Metadata["userId"];
        //                var courseIdStr = session.Metadata["courseId"];

        //                if (int.TryParse(courseIdStr, out int courseId))
        //                {
        //                    var secondLesson = await _context.Lessons
        //                .Include(m => m.Module)
        //.Where(l => l.Module.CourseId == courseId)
        //.OrderBy(l => l.LessonIndex)
        //.Skip(1) // Пропускаємо першу (безкоштовну)
        //.FirstOrDefaultAsync();

        //                    if (secondLesson != null)
        //                    {
        //                        // Викликаємо ТВІЙ метод для відкриття другої лекції
        //                        await _progressService.OpenLessonAsync(userId, secondLesson.Id);
        //                    }
        //                }
        //            }
        //        }

        //        return Ok();
        //    }
        //    catch (StripeException e)
        //    {
        //        Console.WriteLine($"Stripe Webhook Error: {e.Message}");
        //        return BadRequest();
        //    }
        //}


      
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()   //Підтвердження оплати
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
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
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;

                    // Перевіряємо, чи є метадані
                    if (session != null && session.Metadata != null)
                    {
                        var userId = session.Metadata.ContainsKey("userId") ? session.Metadata["userId"] : null;
                        var courseIdStr = session.Metadata.ContainsKey("courseId") ? session.Metadata["courseId"] : null;


                        if (userId != null && int.TryParse(courseIdStr, out int courseId))
                        {
                            var registration = await _context.CourseRegistrations
                .FirstOrDefaultAsync(cr => cr.UserId == userId && cr.CourseId == courseId);

                            if (registration != null)
                            {
                                registration.IsPaid = true;
                                // Тут ми не робимо SaveChanges окремо, можна зробити один раз в кінці
                            }



                            if (await _progressService.IsFirstModuleCompletedAsync(userId,courseId))
                            {
                                await _progressService.UnlockNextModuleAsync(userId,courseId);
                            }
                            else
                            {
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
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
}
