using Domain.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IMessageRepository
    {
        Task<IEnumerable<Message>> GetByConversationIdAsync(string conversationId);
        Task<IEnumerable<Message>> GetUnreadMessagesAsync(string userId);
    }
}
