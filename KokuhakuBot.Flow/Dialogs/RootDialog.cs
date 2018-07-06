using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokuhakuBot.Flow.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public static int YesId { get; } = 0;
        public static int NoId { get; } = 1;

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceiveAsync);
            return Task.CompletedTask;
        }

        private Task MessageReceiveAsync(IDialogContext context, IAwaitable<object> result)
        {
            InitialMessage(context);
            return Task.CompletedTask;
        }

        private async Task PostKokuhakuAsync(IDialogContext context, IAwaitable<object> result)
        {
            await context.PostAsync("結果が来たら教えるね！");
            context.Wait(MessageReceiveAsync);
        }

        private void InitialMessage(IDialogContext context)
        {
            PromptDialog.Confirm(context, ProcessAnswerAsync, "告白したい人がいるの？");
        }

        private async Task ProcessAnswerAsync(IDialogContext context, IAwaitable<bool> result)
        {
            if (await result)
            {
                context.Call(new KokuhakuDialog(), PostKokuhakuAsync);
                return;
            }

            await context.PostAsync("あんた何しに来たの？");
            context.Wait(MessageReceiveAsync);
        }
    }
}
