using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarTray
{
    public class MyMessage
    {
        public string Content { get; }

        public MyMessage(string content)
        {
            Content = content;
        }
    }

    public class Msg_CloseMainWin
    {
        public bool CloseMainWin { get; set; }
    }
}
