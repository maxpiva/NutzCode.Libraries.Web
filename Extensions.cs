using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NutzCode.Libraries.Web
{
    public static class Extensions
    {
        public static void CopyTo(this object s, object d)
        {
            foreach (PropertyInfo pis in s.GetType().GetProperties())
            {
                foreach (PropertyInfo pid in d.GetType().GetProperties())
                {
                    if (pid.Name == pis.Name)
                        (pid.GetSetMethod()).Invoke(d, new [] { pis.GetGetMethod().Invoke(s, null) });
                }
            }
        }



        //snippet from https://stackoverflow.com/a/39034315
        //by GregoryBrad https://stackoverflow.com/users/1017874/gregorybrad
        //Modified

        static Regex rxCookieParts = new Regex(@"(?<name>.*?)\=(?<value>.*?)\;|(?<name>\bsecure\b|\bhttponly\b)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        static Regex rxRemoveCommaFromDate = new Regex(@"\bexpires\b\=.*?(\;|$)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline);

        public static List<Cookie> GetHttpCookies(this IEnumerable<string> raw_cookies, string host)
        {


            try
            {
                List<string> rawcookieString = raw_cookies.Select(a => rxRemoveCommaFromDate.Replace(a, new MatchEvaluator(RemoveComma))).ToList();
                return rawcookieString.SelectMany(a => a.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)).Select(a => a.ToCookie(host)).ToList();
            }
            catch (Exception)
            {
                //ignored
            }
            return new List<Cookie>(); ;
        }
        public static Cookie ToCookie(this string rawCookie, string host)
        {

            if (!rawCookie.EndsWith(";")) rawCookie += ";";

            MatchCollection maches = rxCookieParts.Matches(rawCookie);

            Cookie cookie = new Cookie(maches[0].Groups["name"].Value.Trim(), maches[0].Groups["value"].Value.Trim());

            for (int i = 1; i < maches.Count; i++)
            {
                switch (maches[i].Groups["name"].Value.ToLower().Trim())
                {
                    case "domain":
                        cookie.Domain = maches[i].Groups["value"].Value;
                        break;
                    case "expires":

                        DateTime dt;

                        if (DateTime.TryParse(maches[i].Groups["value"].Value, out dt))
                        {
                            cookie.Expires = dt;
                        }
                        else
                        {
                            cookie.Expires = DateTime.Now.AddDays(2);
                        }
                        break;
                    case "path":
                        cookie.Path = maches[i].Groups["value"].Value;
                        break;
                    case "secure":
                        cookie.Secure = true;
                        break;
                    case "httponly":
                        cookie.HttpOnly = true;
                        break;
                }
            }
            if (string.IsNullOrEmpty(cookie.Path))
                cookie.Path = "/";
            if (string.IsNullOrEmpty(cookie.Domain))
                cookie.Domain = host;
            return cookie;
        }

        private static string RemoveComma(Match match)
        {
            return match.Value.Replace(',', ' ');
        }
    }
}
