using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZip
{
    class Args
    {
        public Args(string[] args)
        {
            if ((args?.Length ?? 0) < 3)
            {
                throw new ArgumentException("Invalid args!");
            }

            this.m_args = args;
        }

        public virtual CompressionMode CompressionMode
        {
            get
            {
                var mode = this.m_args[0].ToUpper();
                return mode == "COMPRESS" 
                    ? CompressionMode.Compress : CompressionMode.Decompress;
            }
        }

        public virtual string InputFile => this.m_args[1];
        
        public virtual string OutputFile => this.m_args[2];
        
        public virtual string MetaExt => ".oz.meta";

        public virtual string DataExt => ".oz.data";

        protected string[] m_args;
    }
}
