using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.UI.Phone.Messages;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using System.Collections;
using UnityEngine;

namespace TestBot
{
    public class TestDialogue
    {
        private IEnumerator DelayedShowResponses(float delay, DialogueResponse[] next)
        {
            yield return new WaitForSeconds(delay);
            ShowResponses(next,0.1f);
        }

        private readonly NPC _npc;
        private readonly MSGConversation _conversation;

        public TestDialogue(NPC npc)
        {
            _npc = npc;

            _conversation = new MSGConversation(npc, npc.fullName);
            _npc.MSGConversation = _conversation;

            _conversation.messageHistory = new Il2CppSystem.Collections.Generic.List<Message>();
            _conversation.messageChainHistory = new Il2CppSystem.Collections.Generic.List<MessageChain>();
            _conversation.bubbles = new Il2CppSystem.Collections.Generic.List<MessageBubble>();
            _conversation.EntryVisible = true;

            if (_npc.ConversationCategories == null)
            {
                _npc.ConversationCategories = new Il2CppSystem.Collections.Generic.List<EConversationCategory>();
                _npc.ConversationCategories.Add(EConversationCategory.Customer);
            }

            _conversation.SetCategories(_npc.ConversationCategories);
        }

        public void SendNPCMessage(string text)
        {
            var message = new Message(text, Message.ESenderType.Other, true, -1);
            _conversation.SendMessage(message, true, false);
        }

        public void SendPlayerMessage(string text)
        {
            var message = new Message(text, Message.ESenderType.Player, true, -1);
            _conversation.SendMessage(message, true, false);
        }
        public void ShowResponses(DialogueResponse[] responses, float v)
        {
            var responseList = new Il2CppSystem.Collections.Generic.List<Response>();

            foreach (var response in responses)
            {
                var action = response.Action;

                // Wrap the action to optionally show follow-ups
                System.Action wrapped = () =>
                {
                    action?.Invoke();

                    if (response.NextResponses != null && response.NextResponses.Length > 0)
                    {
                        MelonCoroutines.Start(DelayedShowResponses(0.5f, response.NextResponses));
                    }
                };

                responseList.Add(new Response(response.Id, response.Text, wrapped, response.DisableDefaultBehavior));
            }

            _conversation.ClearResponses(false);
            _conversation.ShowResponses(responseList, 0f, false);
        }
    }

}
