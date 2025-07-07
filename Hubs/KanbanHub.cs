using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace lol.Hubs
{
    public class KanbanHub : Hub
    {
        public async Task JoinBoard(int projectId, int teamId)
        {
            string groupName = GetGroupName(projectId, teamId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveBoard(int projectId, int teamId)
        {
            string groupName = GetGroupName(projectId, teamId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task BroadcastTaskUpdate(int projectId, int teamId, string action, object taskData)
        {
            string groupName = GetGroupName(projectId, teamId);
            await Clients.Group(groupName).SendAsync("TaskUpdated", action, taskData);
        }

        private string GetGroupName(int projectId, int teamId)
        {
            return $"KanbanBoard_{projectId}_{teamId}";
        }
    }
}
