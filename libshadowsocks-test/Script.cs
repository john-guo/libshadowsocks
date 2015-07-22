using Shadowsocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libshadowsocks_test.sites
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SSAttribute : Attribute
    {
    }

    public interface ISSSite
    {
        IList<Server> GetServers();
    }
}
