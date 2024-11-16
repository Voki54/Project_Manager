﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Manager.Data;
using Project_Manager.DTO.ProjectTasks;
using Project_Manager.Models;
using Project_Manager.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;
using Project_Manager.Helpers;
using Microsoft.Build.Framework;

namespace Project_Manager.Controllers
{
    public class ProjectTasksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProjectTasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int? categoryId, string? sortColumn)
        {

            // Получаем список всех категорий
            var categories = _context.Categories.ToList();
            if (categories == null || !categories.Any())
            {
                Console.WriteLine("Список категорий пуст.");
            }
            else
            {
                Console.WriteLine("Список категорий:");
                foreach (var category in categories)
                {
                    Console.WriteLine($"- {category.Name} (ID: {category.Id})");
                }
            }

            List<ProjectTaskDTO> tasks;

            if (sortColumn != null)
            {
                // Определяем порядок сортировки
                var isAscending = !SortState.isColumnInProjectTaskViewSorted.GetValueOrDefault(sortColumn, false);
                var orderBy = isAscending ? sortColumn : sortColumn + " desc";

                tasks = _context.Tasks
                    .Include(t => t.AppUser)
                    .Include(t => t.Category)
                    .Select(t => new ProjectTaskDTO
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Status = t.Status.HasValue ? t.Status.ToString() : "Не указан",
                        Category = t.Category,
                        ExecutorName = t.AppUser != null ? t.AppUser.UserName : "Не назначен",
                        DueDateTime = t.DueDateTime,
                        Description = t.Description
                    })
                    .OrderBy(orderBy)
                    .ToList();

                // Обновляем состояние сортировки
                SortState.isColumnInProjectTaskViewSorted[sortColumn] = isAscending;
            }
            else
            {
                tasks = _context.Tasks
                    .Include(t => t.AppUser)
                    .Include(t => t.Category)
                    .Select(t => new ProjectTaskDTO
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Status = t.Status.HasValue ? t.Status.ToString() : "Не указан",
                        Category = t.Category,
                        ExecutorName = t.AppUser != null ? t.AppUser.UserName : "Не назначен",
                        DueDateTime = t.DueDateTime,
                        Description = t.Description
                    })
                    .ToList();
            }


            if (tasks == null || !tasks.Any())
            {
                Console.WriteLine("Список задач пуст.");
            }
            else
            {
                Console.WriteLine("Список задач:");
                foreach (var task in tasks)
                {
                    Console.WriteLine($"- {task.Title} (ID: {task.Id}, Статус: {task.Status})");
                }
            }

            Category selectedCategory = null;

            if (categoryId.HasValue)
            {
                selectedCategory = _context.Categories.Find(categoryId.Value);

                if (selectedCategory != null)
                {
                    tasks = tasks.Where(t => t.Category.Id == selectedCategory.Id).ToList();
                }
                else
                {
                    Console.WriteLine("Выбранная категория не найдена.");
                }
            }

            var model = new TaskCategoryVM
            {
                Categories = categories ?? new List<Category>(),
                SelectedCategory = selectedCategory ?? new Category(),
                Tasks = tasks ?? new List<ProjectTaskDTO>(),
                SortedColumn = sortColumn,
                IsAsc = sortColumn != null ? !SortState.isColumnInProjectTaskViewSorted.GetValueOrDefault(sortColumn, false) : null,
            };

            return View(model);
        }


        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.ToList(); 
            ViewBag.Users = _context.Users.ToList();          
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectTask task)
        {
            if (ModelState.IsValid)
            {
                await _context.Tasks.AddAsync(task); 
                await _context.SaveChangesAsync();
                return RedirectToAction("Index"); 
            }

            ViewBag.Categories = await _context.Categories.ToListAsync(); 
            ViewBag.Users = await _context.Users.ToListAsync();
            return View(task);
        }


        // GET: ProjectTasks/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var projectTask = await _context.Tasks.FindAsync(id);
            if (projectTask == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Users = await _context.Users.ToListAsync();
            return View(projectTask);
        }

        // POST: ProjectTasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProjectTask projectTask)
        {
            if (id != projectTask.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(projectTask);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectTaskExists(projectTask.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Users = await _context.Users.ToListAsync();
            return View(projectTask);
        }

        // POST: ProjectTasks/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var projectTask = await _context.Tasks.FindAsync(id);
            if (projectTask != null)
            {
                _context.Tasks.Remove(projectTask);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Метод для просмотра задачи
        public IActionResult ViewTask(int id)
        {
            var task = _context.Tasks
                .Include(t => t.Comments)
                .Include(t => t.AppUser)
                .Include(t => t.Category)
                .FirstOrDefault(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            var taskDAO = new ProjectTaskDTO
            {
                Id = task.Id,
                Title = task.Title,
                Status = task.Status.ToString(),
                Category = task.Category,
                ExecutorName = task.AppUser.UserName,
                DueDateTime = task.DueDateTime,
                Description = task.Description,
                Comments = task.Comments,
            };

            return View(taskDAO);
        }

        // Метод для добавления комментария
        [HttpPost]
        public IActionResult AddComment(int taskId, string content)
        {
            var task = _context.Tasks.Find(taskId);
            if (task == null)
            {
                return NotFound();
            }

            var comment = new Comment
            {
                Content = content,
                ProjectTaskId = taskId,
                CreatedAt = DateTime.Now
            };

            _context.Comments.Add(comment);
            _context.SaveChanges();

            return RedirectToAction("ViewTask", new { id = taskId });
        }


        private bool ProjectTaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }

    }
}