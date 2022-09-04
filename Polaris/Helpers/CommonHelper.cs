using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Helpers
{
    public static class CommonHelper
    {
        public static void RemoveNullEntries<T>(IList<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                    list.RemoveAt(i);
            }
        }

        public static void RemoveDuplicates<T>(IList<T> list, Func<T, bool> duplicateChecker)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list.Count(duplicateChecker) > 1)
                {
                    while (list.Count(duplicateChecker) > 1)
                        list.Remove(list[i]);
                }
            }
        }
    }
}