using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Procore.Core
{
    // Config class to encapsulate API credentials and configuration
    public class Config
    {
        public string ClientId { get; }
        public string ClientSecret { get; }
        public bool IsSandbox { get; }
        public string Url { get; }

        public Config(string clientId, string clientSecret, bool isSandbox, string url)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            IsSandbox = isSandbox;
            Url = url;
        }
    }
}
