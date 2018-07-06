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
        private static string SlackEmailFormatPrefix { get; } = "<mailto:";
        [FunctionName(nameof(Start))]
        public static async Task<HttpResponseMessage> Start(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, 
            [OrchestrationClient] DurableOrchestrationClient client,
            TraceWriter log)
        {
            var info = JsonConvert.DeserializeObject<KokuhakuInformation>(await req.Content.ReadAsStringAsync());
            log.Info($"KokuhakuInformation: {JsonConvert.SerializeObject(info)}");
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
            [SendGrid] out Mail message,
            TraceWriter log)
        {
            var info = context.GetInput<KokuhakuInformation>();
            var approvalEndpoint = ConfigurationManager.AppSettings["KokuhakuApprovalEndpoint"];
            var fromEmail = ConfigurationManager.AppSettings["FromEmail"];
            message = new Mail
            {
                Subject = "あなたにラブレターが届いています",
            };

            var personalization = new Personalization();
            var toEmail = info.TargetEmail;
            if (toEmail.StartsWith(SlackEmailFormatPrefix)) // for slack email address format.
            {
                var endIndex = toEmail.IndexOf('|');
                toEmail = toEmail.Substring(SlackEmailFormatPrefix.Length, endIndex - SlackEmailFormatPrefix.Length);
            }

            log.Info($"To: {toEmail}, Org: {info.TargetEmail}");
            personalization.AddTo(new Email(toEmail));
            message.AddPersonalization(personalization);
            message.From = new Email(fromEmail);
            message.AddContent(new Content
            {
                Type = "text/html",
                Value = $@"<html>
<body>
<h2>要求</h2>
我君ヲ愛ス。{info.Activity.From.Name}ヨリ。
<hr />
<h2>回答</h2>
<a href='{approvalEndpoint}?instanceId={context.InstanceId}&approved=true'>受けて立つ場合はこちらをクリック</a>
<br/>
<a href='{approvalEndpoint}?instanceId={context.InstanceId}&approved=false'>受けて立たない場合はこちらをクリック</a>
</body>
</html>",
            });
        }

        [FunctionName(nameof(Approved))]
        public static async Task Approved(
            [ActivityTrigger] DurableActivityContext context,
            TraceWriter log)
        {
            await ReplyAsync(context, "告白成功したよ！", log);
        }

        [FunctionName(nameof(Reject))]
        public static async Task Reject(
            [ActivityTrigger] DurableActivityContext context,
            TraceWriter log)
        {
            await ReplyAsync(context, "まぁ…その…なんだ…ドンマイ。", log);
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

        private static async Task ReplyAsync(DurableActivityContext context, string message, TraceWriter log)
        {
            var info = context.GetInput<KokuhakuInformation>();
            log.Info($"KokuhakuInformation: {JsonConvert.SerializeObject(info)}");
            var client = new ConnectorClient(new Uri(info.Activity.ServiceUrl));
            var reply = info.Activity.CreateReply(message);
            await client.Conversations.ReplyToActivityAsync(reply);
        }

    }
}
