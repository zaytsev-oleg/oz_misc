using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GZip
{
    enum Result
    {
        Success,
        Failure
    }

    class GZip
    {
        public GZip() 
            : this(500 * 1024, 5)
        {
        }

        protected GZip(int chunk, int threads)
        {
            this.Chunk   = chunk;
            this.Threads = threads;
        }

        public virtual Result Run(Args args)
        {
            Action<Args> run = this.Compress;

            if (args.CompressionMode == CompressionMode.Decompress)
            {
                run = this.Decompress;
            }

            try
            {
                run(args);
                return Result.Success;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"\nError: {ex.GetBaseException().Message}");
                return Result.Failure;
            }
        }

        protected virtual void Compress(Args args)
        {
            Console.WriteLine("Compression");

            var offsets = new List<long>();

            using (var output = new FileInfo(args.OutputFile + args.DataExt).Create())
            {
                var obj = new object();

                var input = new FileInfo(args.InputFile);
                Console.WriteLine($"\nInput: {args.InputFile} ({Math.Round(input.Length / 1024d, 2)} kb)");

                var len = 0;
                var inc = 0;

                var threads = new List<Thread>();
                var queue   = new Queue<int>();
                
                for (int i = 0; i < this.Threads && len < input.Length; i++)
                {
                    var th = new Thread((object val) =>
                    {
                        using (var fs = input.OpenRead())
                        {
                            var skip = (int)val;

                            while (input.Length > skip)
                            {
                                var size = (int)Math.Min(input.Length - skip, this.Chunk);
                                fs.Seek(skip, SeekOrigin.Begin);

                                var ar = new byte[size];
                                fs.Read(ar, 0, size);

                                using (var ms = new MemoryStream())
                                {
                                    using (var gz = new GZipStream(ms, CompressionLevel.Optimal, true))
                                    {
                                        gz.Write(ar, 0, ar.Length);
                                    }

                                    Monitor.Enter(obj);

                                    while (queue.Count == 0 || queue.Peek() != Thread.CurrentThread.ManagedThreadId)
                                    {
                                        Monitor.Exit(obj);
                                        Thread.Yield();
                                        Monitor.Enter(obj);
                                    }

                                    ms.Seek(0, SeekOrigin.Begin);
                                    ms.CopyTo(output);
                                    
                                    offsets.Add(ms.Length);

                                    queue.Dequeue();

                                    Monitor.Exit(obj);
                                }

                                while(Interlocked.Add(ref inc, 0) == 0)
                                {
                                    Thread.Yield();
                                }

                                Monitor.Enter(obj);

                                skip = len;
                                len += this.Chunk;

                                if (input.Length > skip)
                                {
                                    queue.Enqueue(Thread.CurrentThread.ManagedThreadId);
                                }

                                Monitor.Exit(obj);
                            }
                        }
                    })
                    {
                        IsBackground = true
                    };

                    threads.Add(th);
                    queue.Enqueue(th.ManagedThreadId);

                    th.Start(len);

                    len += this.Chunk;
                }

                Interlocked.Increment(ref inc);

                for (int i = 0; i < threads.Count; i++)
                {
                    threads[i].Join();
                }

                Console.WriteLine($"Output:");
                Console.WriteLine($"{args.OutputFile + args.DataExt} ({Math.Round(output.Length / 1024d, 2)} kb)");
            }

            using (var fs = new FileInfo(args.OutputFile + args.MetaExt).Create())
            {
                var meta = new Meta
                {
                    TotalSize = offsets.Sum(),
                    Offsets   = offsets.ToArray()
                };

                var bf = new BinaryFormatter();
                bf.Serialize(fs, meta);

                Console.WriteLine($"{args.OutputFile + args.MetaExt} ({Math.Round(fs.Length / 1024d, 2)} kb)");
                Console.WriteLine("\nDone!");
            }
        }

        protected virtual void Decompress(Args args)
        {
            Console.WriteLine("Decompression");

            var meta = (Meta)null;

            using (var fs = new FileInfo(args.InputFile + args.MetaExt).OpenRead())
            {
                var bf = new BinaryFormatter();
                meta = (Meta)bf.Deserialize(fs);
            }

            Console.WriteLine($"\nInput: {args.InputFile}({args.MetaExt}, {args.DataExt}) ({Math.Round(meta.TotalSize / 1024d, 2)} kb)");

            using (var output = new FileInfo(args.OutputFile).Create())
            {
                var pos = 0;
                var inc = 0;
                var obj = new object();

                var thread_count = Math.Min(this.Threads, meta.Offsets.Length);

                var threads = new List<Thread>(thread_count);
                var queue   = new Queue<int>(thread_count);

                for (; pos < thread_count; pos++)
                {
                    var th = new Thread((object val) =>
                    {
                        var index = (int)val;

                        using (var fs = new FileInfo(args.InputFile + args.DataExt).OpenRead())
                        {
                            while (index < meta.Offsets.Length)
                            {
                                var offset = meta.CalcOffset(index);
                                fs.Seek(offset, SeekOrigin.Begin);

                                using (var gz = new GZipStream(fs, CompressionMode.Decompress, true))
                                {
                                    Monitor.Enter(obj);

                                    while (queue.Count == 0 || queue.Peek() != Thread.CurrentThread.ManagedThreadId)
                                    {
                                        Monitor.Exit(obj);
                                        Thread.Yield();
                                        Monitor.Enter(obj);
                                    }

                                    gz.CopyTo(output);
                                    queue.Dequeue();

                                    Monitor.Exit(obj);
                                }

                                while (Interlocked.Add(ref inc, 0) == 0)
                                {
                                    Thread.Yield();
                                }

                                Monitor.Enter(obj);

                                index = pos++;

                                if (index < meta.Offsets.Length)
                                {
                                    queue.Enqueue(Thread.CurrentThread.ManagedThreadId);
                                }

                                Monitor.Exit(obj);
                            }
                        }
                    })
                    {
                        IsBackground = true
                    };

                    threads.Add(th);
                    queue.Enqueue(th.ManagedThreadId);

                    th.Start(pos);
                }

                Interlocked.Increment(ref inc);

                for (int i = 0; i < threads.Count; i++)
                {
                    threads[i].Join();
                }

                Console.WriteLine($"Output: {args.OutputFile} ({Math.Round(output.Length / 1024d, 2)} kb)");
                Console.WriteLine("\nDone!");
            }
        }

        public int Chunk   { get; protected set; }
        public int Threads { get; protected set; }
    }
}
