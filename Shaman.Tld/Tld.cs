using System;
using System.Collections.Generic;
using System.Text;

namespace Shaman.Runtime
{
    public static class Tld
    {
        public static string GetDomainFromUrl(Uri url)
        {
            return GetDomainFromHost(url.Host);
        }

        public static string GetDomainFromHost(string host)
        {
            var z = new DomainName.Library.DomainName(host);
            if (string.IsNullOrEmpty(z.Domain)) return z.TLD;
            return z.Domain + (!string.IsNullOrEmpty(z.TLD) ? "." + z.TLD : null);

        }

        public static string GetFullDomainWithoutWww(string host)
        {
            const string prefix = "www.";
            if (host.StartsWith(prefix)) return host.Substring(prefix.Length);
            return host;
        }


        public static Func<string> GetTldRulesCallback { get; set; }

    }
}