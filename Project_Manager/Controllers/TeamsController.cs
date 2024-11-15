﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Manager.Data;
using Project_Manager.Data.DAO.Interfaces;
using Project_Manager.Data.DAO.Repository;
using Project_Manager.DTO.Team;
using Project_Manager.Mappers;
using Project_Manager.Models;
using Project_Manager.Services;

namespace Project_Manager.Controllers
{
    public class TeamsController : Controller
    {
        private readonly ITeamRepository _teamRepository;
        private readonly ITeamUserRepository _teamUserRepository;
        private readonly TeamUserService _teamUserService;
        private readonly UserManager<AppUser> _userManager;

        public TeamsController(ITeamRepository teamRepository, ITeamUserRepository teamUserRepository, 
            TeamUserService teamUserService, UserManager<AppUser> userManager)
        {
			_teamRepository = teamRepository;
            _teamUserRepository = teamUserRepository;
            _teamUserService = teamUserService;
            _userManager = userManager;
        }

        private async Task<string?> GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

		[HttpGet]
		public async Task<IActionResult> Index()
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            var userId = await GetUserId();
            if (userId == null)
                return Unauthorized("User is not authenticated.");

            var teams = await _teamUserService.GetUserTeamsAsync(userId);
            var teamsDTO = teams.Select(t => t.ToTeamDTO()).ToList();
            return View(teamsDTO);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateTeamRequestDTO teamDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetUserId();
            if (userId == null)
                return Unauthorized("User is not authenticated.");

            var createdTeam = await _teamRepository.CreateAsync(teamDTO.ToTeamFromCreateDTO());
            await _teamUserService.AddUserToTeamAsync(createdTeam.Id, userId, UserRoles.Admin);

            return RedirectToAction("Index", "Teams");
        }

        // GET: Teams/Details/5
        //public async Task<IActionResult> Details(string id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var team = await _context.Team
        //        .FirstOrDefaultAsync(m => m.Id == id);
        //    if (team == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(team);
        //}



        //// POST: Teams/Create
        //// To protect from overposting attacks, enable the specific properties you want to bind to.
        //// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create([Bind("Id,Name,AdminId,ExecutorsId,ManagersId")] Team team)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _context.Add(team);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(team);
        //}

        //// GET: Teams/Edit/5
        //public async Task<IActionResult> Edit(string id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var team = await _context.Team.FindAsync(id);
        //    if (team == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(team);
        //}

        //// POST: Teams/Edit/5
        //// To protect from overposting attacks, enable the specific properties you want to bind to.
        //// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(string id, [Bind("Id,Name,AdminId,ExecutorsId,ManagersId")] Team team)
        //{
        //    if (id != team.Id)
        //    {
        //        return NotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _context.Update(team);
        //            await _context.SaveChangesAsync();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!TeamExists(team.Id))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(team);
        //}

        //// GET: Teams/Delete/5
        //public async Task<IActionResult> Delete(string id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var team = await _context.Team
        //        .FirstOrDefaultAsync(m => m.Id == id);
        //    if (team == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(team);
        //}

        //// POST: Teams/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(string id)
        //{
        //    var team = await _context.Team.FindAsync(id);
        //    if (team != null)
        //    {
        //        _context.Team.Remove(team);
        //    }

        //    await _context.SaveChangesAsync();
        //    return RedirectToAction(nameof(Index));
        //}

        //private bool TeamExists(string id)
        //{
        //    return _context.Team.Any(e => e.Id == id);
        //}
    }
}
