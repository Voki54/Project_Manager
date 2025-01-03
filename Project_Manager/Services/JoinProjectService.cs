﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Project_Manager.Data.DAO.Interfaces;
using Project_Manager.Events.Notification;
using Project_Manager.Events.Notification.EventHandlers;
using Project_Manager.Models;
using Project_Manager.Models.Enums;
using Project_Manager.Services.Interfaces;
using Project_Manager.ViewModels;

namespace Project_Manager.Services
{
    public class JoinProjectService : IJoinProjectService
    {
        private readonly IProjectService _projectService;
        private readonly IProjectUserService _projectUserService;
        private readonly IJoinProjectRequestRepository _joinProjectRequestRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly EventPublisher _eventPublisher;
        private readonly NotificationEventHandler _notificationEventHandler;

        public JoinProjectService(IProjectService projectService, IProjectUserService projectUserService,
            UserManager<AppUser> userManager, IJoinProjectRequestRepository joinProjectRequestRepository,
            EventPublisher eventPublisher, NotificationEventHandler notificationHandler)
        {
            _projectService = projectService;
            _projectUserService = projectUserService;
            _userManager = userManager;
            _joinProjectRequestRepository = joinProjectRequestRepository;

            _eventPublisher = eventPublisher;
            _notificationEventHandler = notificationHandler;

            _eventPublisher.Subscribe(async e => await _notificationEventHandler.HandleAsync(e));
        }

        public async Task<JoinProjectVM?> JoinProjectAsync(int projectId, string userId)
        {
            var projectName = await _projectService.GetProjectName(projectId);
            if (projectName == null)
                return null;

            JoinProjectRequestStatus? requestStatus = null;
            var joinProjectRequest = await _joinProjectRequestRepository.GetJoinProjectRequestAsync(projectId, userId);
            if (joinProjectRequest != null)
                requestStatus = joinProjectRequest.Status;

            return (new JoinProjectVM(projectId, projectName, requestStatus));
        }

        public async Task<bool> SubmitJoinRequestAsync(int projectId, string userId)
        {
            if (!await _projectService.ExistProjectAsync(projectId))
                return false;

            if (await _joinProjectRequestRepository.GetJoinProjectRequestAsync(projectId, userId) != null)
                return true;

            if (await _projectUserService.IsUserInProjectAsync(userId, projectId))
            {
                await _joinProjectRequestRepository.CreateAsync(
                    new JoinProjectRequest(projectId, userId, JoinProjectRequestStatus.Accepted));
                return true;
            }

            await _joinProjectRequestRepository.CreateAsync(
                new JoinProjectRequest(projectId, userId, JoinProjectRequestStatus.Pending));

            await _eventPublisher.PublishAsync(NotificationSendingEvent.CreateWithSender(userId, projectId, NotificationType.JoinProject));

            return true;
        }

        public async Task<int?> SubmitJoinRequestAsync(string projectLink, string userId)
        {
            var projectId = ExtractProjectIdFromLink(projectLink);

            if (projectId == null)
                return null;

            if (await SubmitJoinRequestAsync(projectId!.Value, userId))
                return projectId;

            return null;
        }

        private int? ExtractProjectIdFromLink(string projectLink)
        {
            var link = new Uri(projectLink);
            var queryParams = QueryHelpers.ParseQuery(link.Query);

            if (queryParams.TryGetValue("projectId", out var projectIdValue) && int.TryParse(projectIdValue, out int projectId))
                return projectId;

            return null;
        }

        public async Task<IEnumerable<RespondVM>> GetJoiningRequestsAsync(int projectId)
        {
            var users = await _joinProjectRequestRepository.GetUsersWithUnprocessedRequestsAsync(projectId);
            List<RespondVM> respondVMs = new List<RespondVM>();

            foreach (AppUser user in users)
                respondVMs.Add(new RespondVM(user.Id, user.Email, user.UserName, projectId));

            return respondVMs;
        }

        public async Task<bool> AcceptApplicationAsync(string userId, int projectId, UserRoles userRole)
        {
            if (await _userManager.FindByIdAsync(userId) == null)
                return false;

            if (!await _projectUserService.IsUserInProjectAsync(userId, projectId))
                await _projectUserService.AddUserToProjectAsync(projectId, userId, userRole);

            await _joinProjectRequestRepository.UpdateAsync(
                new JoinProjectRequest(projectId, userId, JoinProjectRequestStatus.Accepted));

            await _eventPublisher.PublishAsync(NotificationSendingEvent.CreateWithRecipient(userId, projectId, NotificationType.AcceptJoin));

            return true;
        }

        public async Task<bool> RejectApplicationAsync(string userId, int projectId)
        {
            if (await _userManager.FindByIdAsync(userId) == null)
                return false;

            await _joinProjectRequestRepository.DeleteAsync(projectId, userId);
            return true;
        }
    }
}
