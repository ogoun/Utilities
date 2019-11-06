using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZeroLevel;

namespace Eraser
{
    class Program
    {
        static string _base_fld;
        static int _count = 0;
        static int _in_process = 0;
        static BlockingCollection<FileStream> _to_progress =
            new BlockingCollection<FileStream>();

        const int BUFFER_SIZE = 1024;
        const int MAX_MIX_FILES = 24;
        const int MAX_QUEUE_LENGTH = 512;

        const long INITIAL_SIZE = 3221225472;
        const long STOP_SIZE = 1024;


        static void Main(string[] args)
        {
            var cfg = Configuration.ReadFromCommandLine(args);
            if (!cfg.Contains("path")) return;
            _base_fld = cfg.First("path");

            var t = new Thread(MixProcess);
            t.Start();

            Sheduller.RemindEvery(TimeSpan.FromSeconds(5), () 
                => Console.WriteLine($"Proceed: {_count}.\r\nInProgress: {_in_process}"));

            long initial_size = INITIAL_SIZE;
            while (initial_size > STOP_SIZE)
            {
                FillWithSize(initial_size);
                initial_size >>= 1;
            }
            Console.WriteLine("Complete creation");
            _to_progress.CompleteAdding();
            while (!_to_progress.IsCompleted || _in_process > 0)
            {
                Thread.Sleep(500);
            }
            Console.WriteLine("Complete mixing");
            Sheduller.Dispose();
            Console.ReadKey();
        }

        static void MixProcess()
        {
            while (!_to_progress.IsCompleted)
            {
                while (_in_process > MAX_MIX_FILES)
                {
                    Thread.Sleep(100);
                }
                if (_to_progress.TryTake(out FileStream stream))
                {
                    Task.Run(() => MixFile(stream));
                }
            }
        }

        static void FillWithSize(long size)
        {
            FileStream file;
            while ((file = TryCreate(size)) != null)
            {
                while (_to_progress.Count > MAX_QUEUE_LENGTH)
                {
                    Thread.Sleep(100);
                }
                _to_progress.Add(file);
            }
        }

        static FileStream TryCreate(long size)
        {
            FileStream stream = null;
            string file_path = Path.Combine(_base_fld, Interlocked.Increment(ref _count).ToString() + ".bin");
            try
            {
                stream = new FileStream(file_path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                stream.SetLength(size);
                return stream;
            }
            catch
            {
                if (stream != null)
                {
                    stream.Close();
                    stream.Dispose();
                    try
                    {
                        if (File.Exists(file_path))
                        {
                            File.Delete(file_path);
                        }
                    }
                    catch { }
                }
            }
            return null;
        }

        static void MixFile(object stream)
        {
            Interlocked.Increment(ref _in_process);
            var rnd = new Random((int)Environment.TickCount);
            try
            {
                var raf = new RandomAccessFile((FileStream)stream);
                var b = new byte[BUFFER_SIZE];
                for (var i = 0; i < raf.Length(); i += b.Length)
                {
                    rnd.NextBytes(b);
                    raf.Seek(i * b.Length);
                    raf.Write(b, 0, b.Length);
                }
                raf.Sync();
                raf.Close();
            }
            catch
            {
            }
            finally
            {
                Interlocked.Decrement(ref _in_process);
            }
        }
    }
}
