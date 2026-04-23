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
using Stripe.Tax;



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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"CREATE SESSION userId: '{userId}'");
            var registration = await _context.CourseRegistrations
    .FirstOrDefaultAsync(r => r.UserId == userId && r.CourseId == courseId);
            if (registration == null)
            {
                return BadRequest("Registration not found");
            }
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                ClientReferenceId = registration.Id.ToString(),
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
                //{ "userId", User.FindFirstValue(ClaimTypes.NameIdentifier) }
                //{ "userId", userId }
            },
                SuccessUrl = domain + "/api/Payment/Success",
                CancelUrl = domain + "/User/Dashboard?courseId=" + courseId,
            };

            try
            {
               
                var service = new SessionService();
                Session session = await service.CreateAsync(options);

                //return Ok(new StripeResponseDTO
                //{
                //    SessionId = session.Id,
                //    PubKey = _configuration["Stripe:PublishableKey"]
                //});
                return Json(new { url = session.Url });

            }
            catch (StripeException e)
            {
                Console.WriteLine("STRIPE ERROR: " + e.Message);
                return BadRequest(e.Message);
            }
           
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
        //            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;

        //            Console.WriteLine("=== WEBHOOK START ===");

        //            if (session == null)
        //            {
        //                Console.WriteLine("❌ Session is null");
        //                return Ok();
        //            }


        //            var registrationIdStr = session.ClientReferenceId;

        //            Console.WriteLine($"ClientReferenceId: '{registrationIdStr}'");

        //            if (string.IsNullOrEmpty(registrationIdStr) || !int.TryParse(registrationIdStr, out int registrationId))
        //            {
        //                Console.WriteLine(" Не вдалося отримати registrationId");
        //                return Ok();
        //            }


        //            var registration = await _context.CourseRegistrations
        //                .FirstOrDefaultAsync(r => r.Id == registrationId);

        //            if (registration == null)
        //            {
        //                Console.WriteLine(" Реєстрацію не знайдено в БД");
        //                return Ok();
        //            }

        //            Console.WriteLine(" Реєстрацію знайдено");


        //            registration.IsPaid = true;

        //            var courseId = registration.CourseId;
        //            var userId = registration.UserId;


        //            var secondModule = await _context.Modules
        //                .Where(m => m.CourseId == courseId)
        //                .OrderBy(m => m.OrderIndex)
        //                .Skip(1)
        //                .FirstOrDefaultAsync();

        //            if (secondModule != null)
        //            {
        //                var firstLessonOfSecondModule = await _context.Lessons
        //                    .Where(l => l.ModuleId == secondModule.Id)
        //                    .OrderBy(l => l.LessonIndex)
        //                    .FirstOrDefaultAsync();

        //                if (firstLessonOfSecondModule != null)
        //                {
        //                    var existingProgress = await _context.UserProgresses
        //                        .AnyAsync(up => up.UserId == userId && up.LessonId == firstLessonOfSecondModule.Id);

        //                    if (!existingProgress)
        //                    {
        //                        _context.UserProgresses.Add(new UserProgress
        //                        {
        //                            UserId = userId,
        //                            CourseId = courseId,
        //                            ModuleId = secondModule.Id,
        //                            LessonId = firstLessonOfSecondModule.Id,
        //                            Status = ProgressStatus.InProgress
        //                        });
        //                    }
        //                }
        //            }

        //            Console.WriteLine(">>> BEFORE SAVE");
        //            await _context.SaveChangesAsync();
        //            Console.WriteLine(">>> AFTER SAVE");

        //            Console.WriteLine(" БАЗА ОНОВЛЕНА УСПІШНО");
        //        }

        //        return Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($" Webhook Error: {ex.Message}");
        //        return BadRequest(); // краще ніж Ok()
        //    }
        //}
        public async Task<IActionResult> Webhook()   //Підтвердження оплати
        {
            //Request.EnableBuffering();

            //var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            //Request.Body.Position = 0;

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
                    //var sessionEvent = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    //var service = new SessionService();
                    //var session = await service.GetAsync(sessionEvent.Id);
                    //Console.WriteLine($"--- СЕСІЯ ID: {session.Id} ---");


                    //var session = await service.GetAsync(sessionEvent.Id, new SessionGetOptions
                    //{
                    //    Expand = new List<string> { "line_items", "payment_intent" }
                    //});
                    Console.WriteLine($"Stripe-Signature: {Request.Headers["Stripe-Signature"]}");
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    Console.WriteLine($"Metadata count: {session.Metadata?.Count}");

                    var registrationIdStr = session?.ClientReferenceId;
                    Console.WriteLine($"ClientReferenceId: '{registrationIdStr}'");

                    if (!int.TryParse(registrationIdStr, out int registrationId))
                    {
                        Console.WriteLine("❌ Invalid registrationId");
                        return Ok();
                    }

                    var registration = await _context.CourseRegistrations
           .FirstOrDefaultAsync(r => r.Id == registrationId);

                    if (registration == null)
                    {
                        Console.WriteLine("❌ Registration not found");
                        return Ok();
                    }

                    registration.IsPaid = true;
                    registration.PaymentDate = DateTime.Now;

                   
                    var courseId = registration.CourseId;
                    var userId = registration.UserId;

                    await _context.SaveChangesAsync();
                    await _progressService.SyncProgressAfterPayment(userId, courseId);

        //            await _progressService.UnlockNextModuleAsync(
        //registration.UserId,
        //_context.Modules
        //    .Where(m => m.CourseId == registration.CourseId)
        //    .OrderBy(m => m.OrderIndex)
        //    .First().Id);
          //          var secondModule = await _context.Modules
          //.Where(m => m.CourseId == courseId)
          //.OrderBy(m => m.OrderIndex)
          //.Skip(1)
          //.FirstOrDefaultAsync();
          //          if (secondModule != null)
          //          {
          //              var firstLesson = await _context.Lessons
          //                  .Where(l => l.ModuleId == secondModule.Id)
          //                  .OrderBy(l => l.LessonIndex)
          //                  .FirstOrDefaultAsync();

                    //              if (firstLesson != null)
                    //              {
                    //                  var exists = await _context.UserProgresses
                    //                      .AnyAsync(up => up.UserId == userId && up.LessonId == firstLesson.Id);

                    //                  if (!exists)
                    //                  {
                    //                      _context.UserProgresses.Add(new UserProgress
                    //                      {
                    //                          UserId = userId,
                    //                          CourseId = courseId,
                    //                          ModuleId = secondModule.Id,
                    //                          LessonId = firstLesson.Id,
                    //                          Status = ProgressStatus.InProgress
                    //                      });
                    //                  }
                    //              }
                    //          }


                    //await _context.SaveChangesAsync();
                    Console.WriteLine(" DB UPDATED SUCCESSFULLY");

                    // Перевіряємо, чи є метадані
                    //                if (session != null && session.Metadata != null)
                    //                {
                    //                    //var userId = session.Metadata.ContainsKey("userId") ? session.Metadata["userId"] : null;
                    //                    //var courseIdStr = session.Metadata.ContainsKey("courseId") ? session.Metadata["courseId"] : null;
                    //                    session.Metadata.TryGetValue("courseId", out string courseIdStr);
                    //                    session.Metadata.TryGetValue("userId", out string userId);
                    //                    Console.WriteLine($"DEBUG: Отримано userId: {userId}, courseIdStr: {courseIdStr}");
                    //                    Console.WriteLine($"WEBHOOK userId: '{userId}'");

                    //                    if (!string.IsNullOrEmpty(courseIdStr) && int.TryParse(courseIdStr, out int courseId) && !string.IsNullOrEmpty(userId))
                    //                    {



                    //                        //if (userId != null && int.TryParse(courseIdStr, out int courseId))

                    //                        Console.WriteLine($"--- WEBHOOK DEBUG START ---");
                    //                        Console.WriteLine($"UserId from Metadata: '{userId}'");
                    //                        Console.WriteLine($"CourseId from Metadata: {courseId}");

                    //                        var registration = await _context.CourseRegistrations
                    //            .FirstOrDefaultAsync(cr => cr.UserId.ToLower() == userId.ToLower() && cr.CourseId == courseId);
                    //                        Console.WriteLine("+++ РЕЄСТРАЦІЯ ОНОВЛЕНА +++");

                    //                        var dbUserIds = await _context.CourseRegistrations
                    //.Select(x => x.UserId)
                    //.ToListAsync();

                    //                        Console.WriteLine("DB userIds:");
                    //                        dbUserIds.ForEach(id => Console.WriteLine($"'{id}'"));
                    //                        if (registration == null)
                    //                        {
                    //                            Console.WriteLine("!!! ПОМИЛКА: Реєстрацію не знайдено в БД !!!");
                    //                            // Перевіримо, чи взагалі є такий користувач
                    //                            var anyReg = await _context.CourseRegistrations.AnyAsync(cr => cr.UserId == userId);
                    //                            Console.WriteLine($"Чи є в базі взагалі реєстрації для цього User: {anyReg}");
                    //                        }
                    //                        else
                    //                        {
                    //                            Console.WriteLine("+++ УСПІХ: Реєстрацію знайдено +++");
                    //                            registration.IsPaid = true;
                    //                        }
                    //                        //if (registration != null)
                    //                        //{
                    //                        //    registration.IsPaid = true;

                    //                        //}

                    //                        var secondModule = await _context.Modules
                    //     .Where(m => m.CourseId == courseId)
                    //     .OrderBy(m => m.OrderIndex)
                    //     .Skip(1) // Пропускаємо перший (безкоштовний) модуль
                    //     .FirstOrDefaultAsync();

                    //                        if (secondModule != null)
                    //                        {
                    //                            var firstLessonOfSecondModule = await _context.Lessons
                    //                                .Where(l => l.ModuleId == secondModule.Id)
                    //                                .OrderBy(l => l.LessonIndex)
                    //                                .FirstOrDefaultAsync();

                    //                            if (firstLessonOfSecondModule != null)
                    //                            {
                    //                                // Перевіряємо, чи немає вже запису (щоб не дублювати)
                    //                                var existingProgress = await _context.UserProgresses
                    //                                    .AnyAsync(up => up.UserId == userId && up.LessonId == firstLessonOfSecondModule.Id);

                    //                                if (!existingProgress)
                    //                                {
                    //                                    _context.UserProgresses.Add(new UserProgress
                    //                                    {
                    //                                        UserId = userId,
                    //                                        CourseId = courseId,
                    //                                        ModuleId = secondModule.Id,
                    //                                        LessonId = firstLessonOfSecondModule.Id,
                    //                                        Status = ProgressStatus.InProgress // Або ProgressStatus.Open

                    //                                    });
                    //                                }
                    //                            }
                    //                        }
                    //                        await _context.SaveChangesAsync();
                    //                        Console.WriteLine("!!! БАЗА ОНОВЛЕНА УСПІШНО !!!");
                    //                        //if (await _progressService.IsFirstModuleCompletedAsync(userId,courseId))
                    //                        //{
                    //                        //    await _progressService.UnlockNextModuleAsync(userId,courseId);
                    //                        //}
                    //                        //else
                    //                        //{
                    //                        //    await _context.SaveChangesAsync();
                    //                        //}
                    //                    }
                    //                }
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
