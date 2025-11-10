using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace WpfApp1
{
    public class Conversation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = "Cuộc trò chuyện";
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }

    public class ConversationStore
    {
        private readonly string _file;

        public ConversationStore()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WpfApp1Assistant");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            _file = Path.Combine(dir, "conversations.json");
            if (!File.Exists(_file)) File.WriteAllText(_file, "[]");
        }

        public List<Conversation> LoadAll()
        {
            try
            {
                string json = File.ReadAllText(_file);
                var list = JsonConvert.DeserializeObject<List<Conversation>>(json);
                return list ?? new List<Conversation>();
            }
            catch { return new List<Conversation>(); }
        }

        public Conversation CreateNew()
        {
            var all = LoadAll();
            var c = new Conversation();
            all.Insert(0, c);
            File.WriteAllText(_file, JsonConvert.SerializeObject(all, Formatting.Indented));
            return c;
        }

        public void Save(Conversation cv)
        {
            var all = LoadAll();
            int idx = all.FindIndex(x => x.Id == cv.Id);
            if (idx >= 0) all[idx] = cv; else all.Insert(0, cv);
            // auto title
            string title = "Cuộc trò chuyện";
            foreach (var m in cv.Messages)
            {
                if (m.IsUser && !string.IsNullOrWhiteSpace(m.Message))
                {
                    title = m.Message.Length > 40 ? m.Message.Substring(0, 40) + "…" : m.Message;
                    break;
                }
            }
            cv.Title = title;
            File.WriteAllText(_file, JsonConvert.SerializeObject(all, Formatting.Indented));
        }
    }
}
