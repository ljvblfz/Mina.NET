using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// Estimates the amount of memory that the specified <see cref="IOEvent"/> occupies.
    /// </summary>
    public interface IOEventSizeEstimator
    {
        /// <summary>
        /// Estimate the IoEvent size in number of bytes.
        /// </summary>
        /// <param name="ioe">the event we want to estimate the size of</param>
        /// <returns>the estimated size of this event</returns>
        int EstimateSize(IOEvent ioe);
    }

    class DefaultIOEventSizeEstimator : IOEventSizeEstimator
    {
        static readonly Dictionary<Type, int> Type2Size = new Dictionary<Type, int>();

        static DefaultIOEventSizeEstimator()
        {
            Type2Size[typeof(bool)] = sizeof(bool);
            Type2Size[typeof(bool)] = sizeof(bool);
            Type2Size[typeof(bool)] = sizeof(bool);
            Type2Size[typeof(bool)] = sizeof(bool);
            Type2Size[typeof(bool)] = sizeof(bool);
            Type2Size[typeof(bool)] = sizeof(bool);
            Type2Size[typeof(bool)] = sizeof(bool);
            Type2Size[typeof(bool)] = sizeof(bool);
            Type2Size[typeof(bool)] = sizeof(bool);
        }

        public int EstimateSize(IOEvent ioe)
        {
            return EstimateSize((object) ioe) + EstimateSize(ioe.Parameter);
        }

        private int EstimateSize(object obj)
        {
            if (obj == null)
            {
                return 8;
            }

            var answer = 8 + EstimateSize(obj.GetType(), null);

            if (obj is IOBuffer)
            {
                answer += ((IOBuffer) obj).Remaining;
            }
            else if (obj is IWriteRequest)
            {
                answer += EstimateSize(((IWriteRequest) obj).Message);
            }
            else if (obj is string)
            {
                answer += ((string) obj).Length << 1;
            }
            else if (obj is IEnumerable)
            {
                foreach (var m in (IEnumerable) obj)
                {
                    answer += EstimateSize(m);
                }
            }

            return Align(answer);
        }

        private int EstimateSize(Type type, HashSet<Type> visitedTypes)
        {
            int answer;

            if (Type2Size.TryGetValue(type, out answer))
            {
                return answer;
            }

            if (visitedTypes == null)
            {
                visitedTypes = new HashSet<Type>();
            }
            else if (visitedTypes.Contains(type))
            {
                return 0;
            }

            visitedTypes.Add(type);

            answer = 8; // Basic overhead.

            for (var t = type; t != null; t = t.BaseType)
            {
                var fields =
                    t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                BindingFlags.DeclaredOnly);
                foreach (var fi in fields)
                {
                    answer += EstimateSize(fi.FieldType, visitedTypes);
                }
            }

            visitedTypes.Remove(type);

            // Some alignment.
            answer = Align(answer);

            // Put the final answer.
            lock (((ICollection) Type2Size).SyncRoot)
            {
                if (Type2Size.ContainsKey(type))
                {
                    answer = Type2Size[type];
                }
                else
                {
                    Type2Size[type] = answer;
                }
            }

            return answer;
        }

        private static int Align(int size)
        {
            if (size % 8 != 0)
            {
                size /= 8;
                size++;
                size *= 8;
            }
            return size;
        }
    }
}
