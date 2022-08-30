using Nivera;
using System.Collections.Generic;

namespace Polaris.CustomCommands
{
    public class CommandMemory
    {
        private Dictionary<string, object> _mem = new Dictionary<string, object>();

        public T ReadMemory<T>(string variable)
        {
            Log.Verbose($"CommandMemory::ReadMemory<{typeof(T).FullName}>({variable})");

            return _mem.TryGetValue(variable, out var value) ? (value is T t ? t : default) : default;
        }

        public void WriteMemory(string variable, object value)
        { 
           _mem[variable] = value;

            Log.Verbose($"CommandMemory::WriteMemory({variable}, {value})");
        }

        public void ClearMemory()
        { 
            _mem.Clear();

            Log.Verbose("CommandMemory::ClearMemory()");
        }
    }
}