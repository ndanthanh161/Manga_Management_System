using Microsoft.AspNetCore.Authorization;
using MMS.API.Authorization.Requirements;
using MMS.Application.Interfaces;
using MMS.Domain.Entities;
using System.Security.Claims;

namespace MMS.API.Authorization.Handlers
{
    public class TaskOwnerHandler : AuthorizationHandler<TaskOwnerRequirement>
    {
        private readonly IUnitOfWork _uow;

        public TaskOwnerHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            TaskOwnerRequirement requirement)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return;

            var userGuid = Guid.Parse(userId);

            if (context.Resource is not WorkTask task)
            {
                context.Succeed(requirement);
                return;
            }

            // Truy vết: Task -> Region -> Page -> Chapter -> Series
            if (task.RegionId.HasValue)
            {
                var region = await _uow.Regions.GetByIdAsync(task.RegionId.Value);
                if (region != null && region.PageId.HasValue)
                {
                    var page = await _uow.Pages.GetByIdAsync(region.PageId.Value);
                    if (page != null)
                    {
                        var chapter = await _uow.Chapters.GetByIdAsync(page.ChapterId);
                        if (chapter != null)
                        {
                            var series = await _uow.Series.GetByIdAsync(chapter.SeriesId);
                            // Tác giả của bộ truyện chứa Task này có trùng khớp không?
                            if (series != null && series.AuthorId == userGuid)
                            {
                                context.Succeed(requirement);
                            }
                        }
                    }
                }
            }
        }
    }
}
