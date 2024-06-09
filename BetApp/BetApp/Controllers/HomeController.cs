using Microsoft.AspNetCore.Mvc;
using BetApp.Models;
using BetApp.Util;
using System.Collections.Generic;
using System.Threading.Tasks;
using Camunda.Api.Client;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BetApp.Controllers
{
    public class HomeController : Controller
    {
        private const string AdminsGroup = "Admini";

        public async Task<IActionResult> Index(string user)
        {
            DashboardData data = new DashboardData();
            var allBets = await CamundaUtil.GetBets();
            var isAdmin = await CamundaUtil.IsUserInGroup(user, AdminsGroup);

            if (isAdmin)
            {
                data.Bets = allBets;
            }
            else
            {
                data.Bets = allBets.Where(bet => bet.User == user).ToList();
            }

            data.MyTasks = await CamundaUtil.GetTasks(user);

            if (isAdmin)
            {
                data.AdminTasks = await CamundaUtil.UnAssignedGroupTasks(AdminsGroup);
            }

            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> PickTask(string user, string taskId)
        {
            await CamundaUtil.PickTask(taskId, user);
            return RedirectToAction(nameof(Index), new { user });
        }

        [HttpPost]
        public async Task<IActionResult> FinishTask(string user, string taskId) //ovo nikad ne ide, mogu obrisat
        {
            await CamundaUtil.FinishTask(taskId);
            return RedirectToAction(nameof(Index), new { user });
        }

        [HttpPost]
        public async Task<IActionResult> ApproveBet(string user, string taskId, bool approved)
        {
            await CamundaUtil.ApproveBet(taskId, approved);
            return RedirectToAction(nameof(Index), new { user });
        }

        [HttpPost]
        public async Task<IActionResult> SetBetResult(string user, string taskId, string result)
        {
            await CamundaUtil.SetBetResult(taskId, result);
            return RedirectToAction(nameof(Index), new { user });
        }

        [HttpPost]
        public async Task<IActionResult> EnterBetAmount(string user, string taskId, long betAmount)
        {
            await CamundaUtil.EnterBetAmount(taskId, betAmount);
            return RedirectToAction(nameof(Index), new { user });
        }

        [HttpPost]
        public async Task<IActionResult> ApplyForBetReview(string user, string pid)
        {
            if (await CamundaUtil.IsUserInGroup(user, AdminsGroup))
            {
                await CamundaUtil.ApplyForBetReview(pid, user);
            }
            return RedirectToAction(nameof(Index), new { user });
        }


        [HttpGet]
        public IActionResult Start(string user)
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Start(string user, int betId)
        {
            var pid = await CamundaUtil.StartBetProcess(betId, user);
            return RedirectToAction(nameof(Index), new { user });
        }
    }
}
