using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp;

namespace bulk_image_downloader
{
    public class CachedCookies: CefSharp.ICookieVisitor
    {
        public DateTime CachedDate { get; private set; }
        public TimeSpan Age
        {
            get
            {
                return DateTime.Now - CachedDate;
            }
        }
        public bool Expired
        {
            get
            {
                if (Age > cookieLife)
                    return true;
                return false;
            }
        }

        private List<Cookie> cookies = new List<Cookie>();

        private TimeSpan cookieLife = new TimeSpan(1, 0, 0);

        public CachedCookies()
        {
            CachedDate = DateTime.Now;
        }

        public bool Visit(Cookie cookie, int count, int total, ref bool deleteCookie)
        {
            this.cookies.Add(cookie);

            return true;                
        }

        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            for(int i = 0; i < cookies.Count; i++) 
            {
                Cookie cookie = cookies[i];
                output.Append(cookie.Name);
                output.Append("=");
                output.Append(cookie.Value);
                if (i< cookies.Count - 1)
                        output.Append(";");
            }
            return output.ToString();
        }
    }
}
