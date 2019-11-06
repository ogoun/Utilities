using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace FileMix
{
    public class FMix
    {
        public static string mask = "*.*";

        [MTAThread]
        private static void Main(params string[] args)
        {
            if (args.Length > 0)
            {
                if (args.Length > 1)
                {
                    mask = args[1];
                }
                StartMix(args[0]);
            }
        }

        static void StartMix(string path)
        {
            long counter = 0;
            var folders = new Stack<string>(Directory.GetDirectories(path));
            folders.Push(path);
            using (var sem = new Semaphore(Environment.ProcessorCount, Environment.ProcessorCount))
            {
                while (folders.Count > 0)
                {
                    var folder = folders.Pop();
                    Console.WriteLine($"Mix folder {folder}");
                    foreach (var file in Directory.GetFiles(folder, mask))
                    {
                        Interlocked.Increment(ref counter);
                        sem.WaitOne();
                        Task.Run(() =>
                        {
                            try
                            {
                                MixFile(file);
                            }
                            catch
                            {
                            }
                            finally
                            {
                                sem.Release();
                                Interlocked.Decrement(ref counter);
                            }
                        });
                    }
                    foreach (var dir in Directory.GetDirectories(folder))
                    {
                        folders.Push(dir);
                    }
                }
                while (counter != 0)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        static void MixFile(string path)
        {
            var b = new byte[8192];
            var rnd = new RNGCryptoServiceProvider();
            using (var raf = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                for (var i = 0; i < raf.Length; i += b.Length)
                {
                    rnd.GetBytes(b);
                    var count = (int)((i * b.Length + b.Length) > raf.Length
                        ? raf.Length - i * b.Length
                        : b.Length);
                    raf.Write(b, 0, count);
                }
                raf.Flush();
                raf.Close();
            }
        }
    }
}
