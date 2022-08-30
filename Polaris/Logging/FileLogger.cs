using Nivera.Logging;

using System.IO;
using System;

using Polaris.Helpers;

namespace Polaris.Logging
{
    public class FileLogger : ILogger
    {
        private FileStream _stream;
        private StreamWriter _writer;
        private bool _isKilled;

        public LogBuilder Builder => new LogBuilder(this);

        public FileLogger()
        {
            string dateStr = DateTime.Now.ToString("O").Replace("-", "_").Replace(":", "_");

            dateStr = StringHelpers.RemoveAfterIndex(dateStr, dateStr.IndexOf("."));

            _stream = File.OpenWrite($"{Directory.GetCurrentDirectory()}/Logs/{dateStr}.txt");
        }

        ~FileLogger()
        {
            Kill();
        }

        public void WriteBuilder(LogBuilder logBuilder)
        {
            WriteLine(logBuilder.GetLine());
        }

        public void WriteLine(LogLine logLine)
        {
            _writer.WriteLine(GetMessageString(logLine));
        }

        private string GetMessageString(LogLine logLine)
        {
            string str = "";

            foreach (var tuple in logLine.ReadAllLines())
            {
                str += $"{tuple.Item1} ";
            }

            return str;
        }

        public void Kill()
        {
            if (_isKilled)
                return;

            _writer?.Flush();
            _writer?.Close();
            _writer?.Dispose();
            _writer = null;

            _stream?.Flush();
            _stream?.Close();
            _stream?.Dispose();
            _stream = null;

            _isKilled = true;
        }
    }
}