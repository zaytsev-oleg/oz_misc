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
            this.m_args = args;
        }

        public virtual CompressionMode CompressionMode 
            => (CompressionMode)Enum.Parse(typeof(CompressionMode), this.m_args[0]); 
        
        public virtual string InputFile => this.m_args[1];
        
        public virtual string OutputFile => this.m_args[2];
        
        public virtual string MetaExt => ".oz.meta";

        public virtual string DataExt => ".oz.data";

        protected string[] m_args;
    }
}
