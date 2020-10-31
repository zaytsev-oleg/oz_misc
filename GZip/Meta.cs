using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZip
{
    [Serializable]
    class Meta
    {
        public long[] Offsets { get; set; }
        public long TotalSize { get; set; }
        
        public long CalcOffset(int j)
        {
            if((this.Offsets?.Length ?? -1) < j)
            {
                throw new Exception("Invalid index");
            }

            var pos = 0L;

            for (int i = 0; i < j; i++)
            {
                pos += this.Offsets[i];
            }

            return pos;
        }
    }
}
