using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using KokuhakuBot.Flow.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using SendGrid.Helpers.Mail;

namespace KokuhakuBot.Flow
{
    public static class Kokuhaku
    {
        [FunctionName(nameof(Start))]
        public static async Task<HttpResponseMessage> Start(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, 
            [OrchestrationClient] DurableOrchestrationClient client,
            TraceWriter log)
        {
            var info = JsonConvert.DeserializeObject<KokuhakuInformation>(await req.Content.ReadAsStringAsync());
            var instanceId = await client.StartNewAsync(nameof(StartKokuhakuWorkflow), info);
            return client.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(StartKokuhakuWorkflow))]
        public static async Task StartKokuhakuWorkflow(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            TraceWriter log)
        {
            var info = context.GetInput<KokuhakuInformation>();
            await context.CallActivityAsync(nameof(SendEmail), info);
            var approved = await context.WaitForExternalEvent<bool>("Approval");
            if (approved)
            {
                await context.CallActivityAsync(nameof(Approved), info);
            }
            else
            {
                await context.CallActivityAsync(nameof(Reject), info);
            }
        }

        [FunctionName(nameof(SendEmail))]
        public static void SendEmail(
            [ActivityTrigger] DurableActivityContext context,
            [SendGrid] out Mail message)
        {
            var info = context.GetInput<KokuhakuInformation>();
            var approvalEndpoint = ConfigurationManager.AppSettings["KokuhakuApprovalEndpoint"];
            var fromEmail = ConfigurationManager.AppSettings["FromEmail"];
            message = new Mail
            {
                Subject = "���Ȃ��Ƀ��u���^�[���͂��Ă��܂�",
            };

            var personalization = new Personalization();
            personalization.AddTo(new Email(info.TargetEmail));
            message.AddPersonalization(personalization);
            message.From = new Email(fromEmail);
            message.AddContent(new Content
            {
                Type = "text/html",
                Value = $@"<html>
<body>
<h2>�v��</h2>
��N�����X�B{info.From.Name}�����B
<hr />
<h2>��</h2>
<a href='{approvalEndpoint}?instanceId={context.InstanceId}&approved=true'>�󂯂ė��ꍇ�͂�������N���b�N</a>
<br/>
<a href='{approvalEndpoint}?instanceId={context.InstanceId}&approved=false'>�󂯂ė����Ȃ��ꍇ�͂�������N���b�N</a>
</body>
</html>",
            });
        }

        [FunctionName(nameof(Approved))]
        public static async Task Approved(
            [ActivityTrigger] DurableActivityContext context,
            TraceWriter log)
        {
            var info = context.GetInput<KokuhakuInformation>();
            var client = new ConnectorClient(new Uri(info.ServiceUrl));
            var conversation = await client.Conversations.CreateDirectConversationAsync(info.BotAccount, info.From);
            var activity = new Activity
            {
                Text = "��������������I",
                Recipient = info.From,
                From = info.BotAccount,
                Conversation = new ConversationAccount(id: conversation.Id),
            };
            await client.Conversations.SendToConversationAsync(activity);
        }

        [FunctionName(nameof(Reject))]
        public static async Task Reject(
            [ActivityTrigger] DurableActivityContext context,
            TraceWriter log)
        {
            var info = context.GetInput<KokuhakuInformation>();
            var client = new ConnectorClient(new Uri(info.ServiceUrl));
            var activity = new Activity
            {
                Text = "�܂��c���́c�Ȃ񂾁c�h���}�C�B",
            };
            await client.Conversations.SendToConversationAsync(activity);
        }

        [FunctionName(nameof(Approval))]
        public static async Task Approval([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestMessage req, 
            [OrchestrationClient] DurableOrchestrationClient client,
            TraceWriter log)
        {
            var instanceId = req.GetQueryNameValuePairs()
                .First(x => x.Key == "instanceId")
                .Value;
            var approved = bool.Parse(req.GetQueryNameValuePairs().First(x => x.Key == "approved").Value);
            await client.RaiseEventAsync(instanceId, "Approval", approved);
        }
    }
}
