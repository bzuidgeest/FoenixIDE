using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoenixIDE.Simulator
{
    public static class QueueExtensions
    {
        public static IEnumerable<T> DequeueChunk<T>(this Queue<T> queue, int chunkSize)
        {
            for (int i = 0; i < chunkSize && queue.Count > 0; i++)
            {
                yield return queue.Dequeue();
            }
        }

        public static string DequeueString<T>(this Queue<byte> queue)
        {
            string result = "";

            for (int i = 0; queue.Count > 0; i++)
            {
                if (queue.Peek() > 0)
                    result += (char)queue.Dequeue();
                else
                    break;
            }

            return result;
        }
    }
}
