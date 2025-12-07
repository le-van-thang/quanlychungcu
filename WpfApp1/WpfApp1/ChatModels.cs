using System.Collections.Generic;
using System.Windows.Media;

namespace WpfApp1
{
    public class ChatMessage
    {
        public bool IsUser { get; set; }
        public string Message { get; set; } = "";
        public List<FileAttachment> Attachments { get; set; } = new List<FileAttachment>();
    }

    public class FileAttachment
    {
        public string FilePath { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public bool IsImage { get; set; }
        public ImageSource Thumbnail { get; set; } // có thể null
    }
}
