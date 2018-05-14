using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateClientIP
{
    class Program
    {
        static void Main(string[] args)
        {
            Downloader.GetHtml("http://www.huangyuanlei.com/api/checkinout/?ip=");
        }
    }
}
