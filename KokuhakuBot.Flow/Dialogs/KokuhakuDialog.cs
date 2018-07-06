using KokuhakuBot.Flow.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace KokuhakuBot.Flow.Dialogs
{
    [Serializable]
    public class KokuhakuDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            PromptDialog.Text(context, MessageReceiveAsync, "告白する人のメアド教えて");
            return Task.CompletedTask;
        }

        private async Task MessageReceiveAsync(IDialogContext context, IAwaitable<string> result)
        {
            var targetEmail = await result;
            var info = new KokuhakuInformation
            {
                Activity = (Activity)context.Activity,
                TargetEmail = targetEmail,
            };

            using (var client = new HttpClient())
            {
                var endpoint = ConfigurationManager.AppSettings["KokuhakuWorkflowEndpoint"];
                var res = await client.PostAsJsonAsync(endpoint, info);
                if (res.IsSuccessStatusCode)
                {
                    await context.PostAsync("メール送っといてやったから少し待ってろよ");
                }
                else
                {
                    await context.PostAsync("ごめん。なんかエラー起きたわ。もう一回やって");
                }
            }

            context.Done<object>(null);
        }
    }
}
