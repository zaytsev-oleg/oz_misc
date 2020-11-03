using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;

namespace GZip
{
    class Program
    {
        static int Main(string[] args)
        {
            // var args1 = new string[] { "Compress", @"..\..\anna-karenina.fb2", @"..\..\anna" };
            // var args2 = new string[] { "Decompress", @"..\..\anna", @"..\..\ak.fb2" };

            var res = (int)new GZip().Run(args);
            // Console.ReadKey();

            return res;
        }
    }
}
