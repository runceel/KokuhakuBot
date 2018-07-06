using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokuhakuBot.Flow.Models
{
    public class KokuhakuInformation
    {
        [JsonProperty("from")]
        public ChannelAccount From { get; set; }
        [JsonProperty("botAccount")]
        public ChannelAccount BotAccount { get; set; }
        [JsonProperty("targetEmail")]
        public string TargetEmail { get; set; }
        [JsonProperty("serviceUrl")]
        public string ServiceUrl { get; set; }
    }
}
