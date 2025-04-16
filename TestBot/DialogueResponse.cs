using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBot
{
    public class DialogueResponse
    {
        public string Id;
        public string Text;
        public System.Action Action;
        public DialogueResponse[] NextResponses;
        public bool DisableDefaultBehavior;

        public DialogueResponse(string id, string text, System.Action action, bool disableDefaultBehavior = true, DialogueResponse[] next = null)
        {
            Id = id;
            Text = text;
            Action = action;
            DisableDefaultBehavior = disableDefaultBehavior;
            NextResponses = next;
        }
    }
}
