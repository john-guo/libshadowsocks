using Shadowsocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace libshadowsocks_test.sites
{
    public class ishadowsocks
    {
        public IList<Server> GetServers()
        {
            var web = new HtmlWeb();
            var page = web.Load(@"http://www.ishadowsocks.com/");
            var nodes = page.DocumentNode.SelectNodes("//section[@id='free']//div[@class='row'][2]/div");

            List<Server> svcs = new List<Server>();

            foreach (var node in nodes)
            {
                var svcInfo = node.Descendants("H4").ToArray();
                
                Server svc = new Server();
                svc.server = svcInfo[0].InnerText.Split(':')[1];
                svc.server_port = Int32.Parse(svcInfo[1].InnerText.Split(':')[1]);
                svc.password = svcInfo[2].InnerText.Split(':')[1];
                svc.method = svcInfo[3].InnerText.Split(':')[1];

                svcs.Add(svc);
            }

            return svcs;
        }
    }
}
