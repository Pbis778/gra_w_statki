using System;
using System.Collections.Generic;

namespace BattleShip.Services
{
    public class ChatService
    {
        private List<string> chatHistory = new List<string>();

        public event Action<string> MessageAdded;

        public void AddMessage(string sender, string message)
        {
            string formattedMessage = $"[{DateTime.Now:HH:mm:ss}] {sender}: {message}";
            chatHistory.Add(formattedMessage);
            MessageAdded?.Invoke(formattedMessage);
        }

        public void AddSystemMessage(string message)
        {
            string formattedMessage = $"[{DateTime.Now:HH:mm:ss}] System: {message}";
            chatHistory.Add(formattedMessage);
            MessageAdded?.Invoke(formattedMessage);
        }

        public List<string> GetChatHistory()
        {
            return new List<string>(chatHistory);
        }

        public void ClearChat()
        {
            chatHistory.Clear();
        }
    }
}
