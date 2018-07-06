using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using KokuhakuBot.Flow.Dialogs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

namespace KokuhakuBot.Flow
{
    public static class Messages
    {
        [FunctionName("Messages")]
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, TraceWriter log)
        {
            try
            {
                using (BotService.Initialize())
                {
                    var activity = JsonConvert.DeserializeObject<Activity>(await req.Content.ReadAsStringAsync());
                    if (!await BotService.Authenticator.TryAuthenticateAsync(req, new[] { activity }, CancellationToken.None))
                    {
                        return BotAuthenticator.GenerateUnauthorizedResponse(req);
                    }

                    if (activity != null)
                    {
                        switch (activity.GetActivityType())
                        {
                            case ActivityTypes.Message:
                                await Conversation.SendAsync(activity, () => new RootDialog());
                                break;
                            case ActivityTypes.ConversationUpdate:
                                var client = new ConnectorClient(new Uri(activity.ServiceUrl));
                                IConversationUpdateActivity update = activity;
                                if (update.MembersAdded.Any())
                                {
                                    var reply = activity.CreateReply();
                                    var newMembers = update.MembersAdded?.Where(t => t.Id != activity.Recipient.Id);
                                    foreach (var newMember in newMembers)
                                    {
                                        reply.Text = "Welcome";
                                        if (!string.IsNullOrEmpty(newMember.Name))
                                        {
                                            reply.Text += $" {newMember.Name}";
                                        }
                                        reply.Text += "!";
                                        await client.Conversations.ReplyToActivityAsync(reply);
                                    }
                                }
                                break;
                            case ActivityTypes.ContactRelationUpdate:
                            case ActivityTypes.Typing:
                            case ActivityTypes.DeleteUserData:
                            case ActivityTypes.Ping:
                            default:
                                log.Error($"Unknown activity type ignored: {activity.GetActivityType()}");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Error", ex);
                throw;
            }

            return req.CreateResponse(HttpStatusCode.Accepted);
        }
    }
}
