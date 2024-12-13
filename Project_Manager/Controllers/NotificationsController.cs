﻿using Microsoft.AspNetCore.Mvc;
using Project_Manager.Services.Interfaces;
using Project_Manager.Helpers;
using Microsoft.CodeAnalysis;
using Project_Manager.Services;


namespace Project_Manager.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await _notificationService.GetAvailableUserNotificationsAsync(User.GetUserId()));
        }

/*        [HttpPost]
        public async Task<IActionResult> MarkAsRead()
        {

        }*/

        [HttpPost]
        public async Task<IActionResult> Delete(int notificationId)
        {
            if (await _notificationService.DeleteAsync(notificationId))
                return RedirectToAction("Index", "Projects");

            TempData["ErrorMessage"] = "Ошибка при удалении уведомления.";
            return RedirectToAction("Index", "Error");
        }
    }
}
