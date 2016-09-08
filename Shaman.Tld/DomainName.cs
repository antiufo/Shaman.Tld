using System;
using System.Collections.Generic;
using Shaman;
using Shaman.Runtime;

namespace DomainName.Library
{
    internal class DomainName
    {
        #region Private members

        private string _subDomain = string.Empty;
        private string _domain = string.Empty;
        private string _tld = string.Empty;
        private TLDRule _tldRule = default(TLDRule);

        #endregion

        #region Public properties

        /// <summary>
        /// The subdomain portion
        /// </summary>
        public string SubDomain
        {
            get
            {
                return _subDomain;
            }
        }

        /// <summary>
        /// The domain name portion, without the subdomain or the TLD
        /// </summary>
        public string Domain
        {
            get
            {
                return _domain;
            }
        }

        /// <summary>
        /// The domain name portion, without the subdomain or the TLD
        /// </summary>
        public string SLD
        {
            get
            {
                return _domain;
            }
        }

        /// <summary>
        /// The TLD portion
        /// </summary>
        public string TLD
        {
            get
            {
                return _tld;
            }
        }

        /// <summary>
        /// The matching TLD rule
        /// </summary>
        public TLDRule TLDRule
        {
            get
            {
                return _tldRule;
            }
        }

        #endregion

        #region Construction

        /// <summary>
        /// Constructs a DomainName object from the string representation of a domain. 
        /// </summary>
        /// <param name="domainString"></param>
        public DomainName(string domainString)
        {
            //  If an exception occurs it should bubble up past this
            ParseDomainName(domainString, out _tld, out _domain, out _subDomain, out _tldRule);
        }

        /// <summary>
        /// Constructs a DomainName object from its 3 parts
        /// </summary>
        /// <param name="TLD">The top-level domain</param>
        /// <param name="SLD">The second-level domain</param>
        /// <param name="SubDomain">The subdomain portion</param>
        /// <param name="TLDRule">The rule used to parse the domain</param>
        private DomainName(string TLD, string SLD, string SubDomain, TLDRule TLDRule)
        {
            this._tld = TLD;
            this._domain = SLD;
            this._subDomain = SubDomain;
            this._tldRule = TLDRule;
        }

        #endregion

        #region Parse domain - private static method

       internal static void ParseDomainName(string domainStringStr, out string TLD, out string SLD, out string SubDomain, out TLDRule MatchingRule)
       {
	        ValueString tld;
            ValueString sld;
            ValueString subDomain;
            ParseDomainName(domainStringStr, out tld, out sld, out subDomain, out MatchingRule);
            TLD = tld.ToString();
            SLD = sld.ToString();
            SubDomain = subDomain.ToString();
       }
 

        /// <summary>
        /// Converts the string representation of a domain to it's 3 distinct components: 
        /// Top Level Domain (TLD), Second Level Domain (SLD), and subdomain information
        /// </summary>
        /// <param name="domainString">The domain to parse</param>
        /// <param name="TLD"></param>
        /// <param name="SLD"></param>
        /// <param name="SubDomain"></param>
        /// <param name="MatchingRule"></param>
        internal static void ParseDomainName(string domainStringStr, out ValueString TLD, out ValueString SLD, out ValueString SubDomain, out TLDRule MatchingRule)
        {
            TLD = ValueString.Empty;
            SLD = ValueString.Empty;
            SubDomain = ValueString.Empty;
            MatchingRule = default(TLDRule);

            //  If the fqdn is empty, we have a problem already
            if (string.IsNullOrWhiteSpace(domainStringStr))
                throw new ArgumentException("The domain cannot be blank");
            domainStringStr = domainStringStr.ToLowerFast();
            var domainString = domainStringStr.AsValueString();

            //  Next, find the matching rule:
            MatchingRule = FindMatchingTLDRule(domainStringStr);

            //  At this point, no rules match, we have a problem
            if (MatchingRule.Name == null)
            {
                var lastdot = domainString.LastIndexOf('.');
                if (lastdot != -1 && char.IsDigit(domainString[lastdot + 1]))
                {
                    // IP Address
                    SLD = domainString;
                    return;
                }
                else
                {
                    if (lastdot == -1)
                    {
                        SLD = domainString;
                    }
                    else
                    {
                        TLD = domainString.Substring(lastdot + 1);
                        var prev = domainString.LastIndexOf('.', lastdot - 1);
                        if (prev != -1)
                        {
                            SubDomain = domainString.Substring(0, prev);
                            SLD = domainString.Substring(prev + 1, lastdot - prev - 1);
                        }
                        else
                        {
                            SLD = domainString.Substring(0, lastdot);
                        }
                    }
                    return;
                }
            }

            //  Based on the tld rule found, get the domain (and possibly the subdomain)
            var tempSudomainAndDomain = ValueString.Empty;
            int tldIndex = 0;

            //  First, determine what type of rule we have, and set the TLD accordingly
            switch (MatchingRule.Type)
            {
                case TLDRule.RuleType.Normal:
                    if (domainString.Length == MatchingRule.Name.Length)
                    {
                        tempSudomainAndDomain = ValueString.Empty;
                        TLD = domainString;
                    }
                    else
                    {
                        tldIndex = domainString.Length - MatchingRule.Name.Length - 1;
                        tempSudomainAndDomain = domainString.Substring(0, tldIndex);
                        TLD = domainString.Substring(tldIndex + 1);
                    }
                    break;
                case TLDRule.RuleType.Wildcard:
                    //  This finds the last portion of the TLD...
                    tldIndex = domainString.Length - MatchingRule.Name.Length - 1;
                    tempSudomainAndDomain = domainString.Substring(0, tldIndex);

                    //  But we need to find the wildcard portion of it:
                    tldIndex = tempSudomainAndDomain.LastIndexOf('.');
                    tempSudomainAndDomain = domainString.Substring(0, tldIndex);
                    TLD = domainString.Substring(tldIndex + 1);
                    break;
                case TLDRule.RuleType.Exception:
                    tldIndex = domainString.LastIndexOf('.');
                    tempSudomainAndDomain = domainString.Substring(0, tldIndex);
                    TLD = domainString.Substring(tldIndex + 1);
                    break;
            }


            //  See if we have a subdomain:
            var lstRemainingParts = tempSudomainAndDomain.Split('.');

            //  If we have 0 parts left, there is just a tld and no domain or subdomain
            //  If we have 1 part, it's the domain, and there is no subdomain
            //  If we have 2+ parts, the last part is the domain, the other parts (combined) are the subdomain
            if (lstRemainingParts.Length > 0)
            {
                //  Set the domain:
                SLD = lstRemainingParts[lstRemainingParts.Length - 1];

                //  Set the subdomain, if there is one to set:
                if (lstRemainingParts.Length > 1)
                {
                    //  We strip off the trailing period, too
                    SubDomain = tempSudomainAndDomain.Substring(0, tempSudomainAndDomain.Length - SLD.Length - 1);
                }
            }
        }

        #endregion

        #region TryParse method(s)

        /// <summary>
        /// Converts the string representation of a domain to its DomainName equivalent.  A return value
        /// indicates whether the operation succeeded.
        /// </summary>
        /// <param name="domainString"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParse(string domainString, out DomainName result)
        {
            bool retval = false;

            //  Our temporary domain parts:
            string _tld = string.Empty;
            string _sld = string.Empty;
            string _subdomain = string.Empty;
            TLDRule _tldrule = default(TLDRule);
            result = null;

            try
            {
                //  Try parsing the domain name ... this might throw formatting exceptions
                ParseDomainName(domainString, out _tld, out _sld, out _subdomain, out _tldrule);

                //  Construct a new DomainName object and return it
                result = new DomainName(_tld, _sld, _subdomain, _tldrule);

                //  Return 'true'
                retval = true;
            }
            catch
            {
                //  Looks like something bad happened -- return 'false'
                retval = false;
            }

            return retval;
        }

        #endregion

        #region Rule matching
        /// <summary>
        /// Finds matching rule for a domain.  If no rule is found, 
        /// returns a null TLDRule object
        /// </summary>
        /// <param name="domainString"></param>
        /// <returns></returns>
        private static TLDRule FindMatchingTLDRule(string domainString)
        {
            //  Split our domain into parts (based on the '.')
            //  ...Put these parts in a list
            //  ...Make sure these parts are in reverse order (we'll be checking rules from the right-most pat of the domain)
            var lstDomainParts = domainString.SplitFast('.');
            Array.Reverse(lstDomainParts);

            //  Begin building our partial domain to check rules with:
            string checkAgainst = string.Empty;

            //  Our 'matches' collection:
            List<TLDRule> ruleMatches = new List<TLDRule>();

            foreach (string domainPart in lstDomainParts)
            {
                //  Add on our next domain part:
                checkAgainst = domainPart + "." + checkAgainst;

                //  If we end in a period, strip it off:
                if (checkAgainst.EndsWith("."))
                    checkAgainst = checkAgainst.Substring(0, checkAgainst.Length - 1);


                if (TLDRulesCache.Instance.ExceptionRules.Contains(checkAgainst))
                    ruleMatches.Add(new TLDRule(checkAgainst, TLDRule.RuleType.Exception));

                if (TLDRulesCache.Instance.WildcardRules.Contains(checkAgainst))
                    ruleMatches.Add(new TLDRule(checkAgainst, TLDRule.RuleType.Wildcard));

                if (TLDRulesCache.Instance.NormalRules.Contains(checkAgainst))
                    ruleMatches.Add(new TLDRule(checkAgainst, TLDRule.RuleType.Normal));

            }

            if (ruleMatches.Count == 0) return default(TLDRule);
            var maxLength = int.MinValue;
            TLDRule best = default(TLDRule);
            foreach (var item in ruleMatches)
            {
                if (item.Name.Length > maxLength)
                {
                    best = item;
                    maxLength = item.Name.Length;
                }
            }
            return best;
        }
        #endregion
    }
}
