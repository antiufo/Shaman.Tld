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
            ValueString tld;
            ValueString domain;
            ValueString subdomain;
            DomainName.Library.TLDRule rule;
            DomainName.Library.DomainName.ParseDomainName(host, out tld, out domain, out subdomain, out rule);
            if (domain.Length == 0) return tld.ToString();
            if (tld.Length == 0) return domain.ToString();
            return domain.ToString() + "." + tld.ToString();
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