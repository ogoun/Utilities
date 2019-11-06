using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FileMix
{
    public class FMix
    {
        private static long counter = 0;
        public static string mask = "*.*";

        private static void Main(params string[] args)
        {
            if (args.Length > 0)
            {
                if (args.Length > 1)
                {
                    mask = args[1];
                }
                StartMix(args[0]);
                while (counter != 0)
                {
                    Thread.Sleep(1000);
                }
            }
        }


        static void StartMix(string path)
        {
            Interlocked.Increment(ref counter);

            var folders = new Stack<string>(Directory.GetDirectories(path));
            folders.Push(path);
            while (folders.Count > 0)
            {
                var folder = folders.Pop();
                Console.WriteLine($"Mix folder {folder}");
                foreach (var file in Directory.GetFiles(folder, mask))
                {
                    Interlocked.Increment(ref counter);
                    ThreadPool.QueueUserWorkItem(MixFile, file);
                }
                foreach (var dir in Directory.GetDirectories(folder))
                {
                    folders.Push(dir);
                }
            }
            Interlocked.Decrement(ref counter);
        }

        static void MixFile(object path)
        {            
            var rnd = new Random((int)Environment.TickCount);
            try
            {
                var raf = new RandomAccessFile(path.ToString(), "rw");
                var count = (int)(raf.Length() * .4);
                var b = new byte[10];
                var length = raf.Length() - 10;
                for (var i = 0; i < count; i++)
                {
                    rnd.NextBytes(b);
                    raf.Seek((long)((double)length * rnd.NextDouble()));
                    raf.Write(b, 0, 10);
                }
                raf.Sync();
                raf.Close();
            }
            finally
            {
                Interlocked.Decrement(ref counter);
            }
        }
    }
}
