using System;
using System.Collections.Generic;
using Shaman;
using Shaman.Runtime;
using System.Linq;

namespace DomainName.Library
{
    internal sealed class TLDRulesCache
    {
        private static volatile TLDRulesCache _uniqueInstance;
        private static object _syncObj = new object();
        private static object _syncList = new object();

        private TLDRulesCache()
        {
            //  Initialize our internal list:
            GetTLDRules();
        }

        /// <summary>
        /// Returns the singleton instance of the class
        /// </summary>
        public static TLDRulesCache Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_syncObj)
                    {
                        if (_uniqueInstance == null)
                            _uniqueInstance = new TLDRulesCache();
                    }
                }
                return (_uniqueInstance);
            }
        }

        public HashSet<string> NormalRules { get; private set; }
        public HashSet<string> WildcardRules { get; private set; }
        public HashSet<string> ExceptionRules { get; private set; }

        /// <summary>
        /// Resets the singleton class and flushes all the cached 
        /// values so they will be re-cached the next time they are requested
        /// </summary>
        public static void Reset()
        {
            lock (_syncObj)
            {
                _uniqueInstance = null;
            }
        }

        /// <summary>
        /// Gets the list of TLD rules from the cache
        /// </summary>
        /// <returns></returns>
        private void GetTLDRules()
        {
            List<TLDRule> results = new List<TLDRule>();

            var ruleStrings = ReadRulesData();
            NormalRules = new HashSet<string>();
            ExceptionRules = new HashSet<string>();
            WildcardRules = new HashSet<string>();

            //  Strip out any lines that are:
            //  a.) A comment
            //  b.) Blank
            IEnumerable<TLDRule> lstTLDRules = from ruleString in ruleStrings
                                               where
                                               !ruleString.StartsWith("//", Utils.InvariantCultureIgnoreCase)
                                               &&
                                               !(ruleString.Trim().Length == 0)
                                               select new TLDRule(ruleString);

            foreach (var item in lstTLDRules)
            {
                if (item.Type == TLDRule.RuleType.Normal) NormalRules.Add(item.Name);
                else if (item.Type == TLDRule.RuleType.Exception) ExceptionRules.Add(item.Name);
                else if (item.Type == TLDRule.RuleType.Wildcard) WildcardRules.Add(item.Name);
            }

        }

        private IEnumerable<string> ReadRulesData()
        {
            var rules = Tld.GetTldRulesCallback;
            if (rules == null) throw new InvalidOperationException("Tld.TldRules must be set to a delegate that returns the contents of effective_tld_names.dat.");
            return rules().SplitFast('\n');
            /*if (File.Exists(Settings.Default.SuffixRulesFileLocation))
            {
                //  Load the rules from the cached text file
                foreach (var line in File.ReadAllLines(Settings.Default.SuffixRulesFileLocation, Encoding.UTF8))
                    yield return line;
            }
            else
            {
                // read the files from the web directly.
                var datFile = new HttpClient().GetStreamAsync("https://publicsuffix.org/list/effective_tld_names.dat").Result;
                using (var reader = new StreamReader(datFile))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                        yield return line;
                }
            }*/
        }
    }
}
