using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System;

namespace BufferSorter
{
    /// <summary>
    /// This is just a class to run a bunch of tests on the sorter, comparing the results to a cpu-side sort using Linq. You can safely delete this.
    /// </summary>
    public class SorterDemo : MonoBehaviour
    {
        [SerializeField]
        private ComputeShader m_sorterComputeShader = null;
        
        [SerializeField]
        private int m_randomNumberRange = 100;

        [SerializeField]
        [Min(1)]
        private int m_firstNCount = 20;

        [SerializeField]
        [Min(1)]
        private int m_testStartCount = 10;

        [SerializeField]
        [Min(1)]
        private int m_testEndCount = 100;

        [SerializeField]
        [Min(1)]
        private int m_testRunsPerCount = 5;
        
        [ContextMenu("Run Test")]
        private void RunManyTests()
        {
            for (int i = m_testStartCount; i < m_testEndCount; i++)
            {
                Debug.Log("----- " + i + " -----");
                RunManyTests(i, m_testRunsPerCount);
            }
        }

        private void RunManyTests(int count, int testRuns)
        {
            void TestSettings(bool powerOfTwo, bool reverse, bool useNegatives, bool seperateSortList, int firstN = -1)
            {
                int failed = -1;
                for (int i = 0; i < testRuns; i++)
                {
                    if (!RunTest(count, powerOfTwo, reverse, useNegatives, seperateSortList, firstN))
                    {
                        failed = i;
                        break;
                    }
                }

                if (failed < 0)
                {
                    Debug.Log(AddColour("Success on settings: ", Color.green) + $"Power Of Two = {powerOfTwo}, Reverse = {reverse}, Negatives = {useNegatives}, SeperateSortList = {seperateSortList}, FirstN = {firstN}");
                }
                else
                {
                    Debug.Log(AddColour($"Failure at attempt {failed} on settings: ", Color.red) + $"Power Of Two = {powerOfTwo}, Reverse = {reverse}, Negatives = {useNegatives}, SeperateSortList = {seperateSortList}, FirstN = {firstN}");
                }
            }

            TestSettings(true, true, true, true);
            TestSettings(true, true, false, true);
            TestSettings(true, false, true, true);
            TestSettings(true, false, false, true);
            TestSettings(false, true, true, true);
            TestSettings(false, true, false, true);
            TestSettings(false, false, true, true);
            TestSettings(false, false, false, true);

            TestSettings(true, true, true, false);
            TestSettings(true, true, false, false);
            TestSettings(true, false, true, false);
            TestSettings(true, false, false, false);
            TestSettings(false, true, true, false);
            TestSettings(false, true, false, false);
            TestSettings(false, false, true, false);
            TestSettings(false, false, false, false);

            TestSettings(true, true, true, true, m_firstNCount);
            TestSettings(true, true, false, true, m_firstNCount);
            TestSettings(true, false, true, true, m_firstNCount);
            TestSettings(true, false, false, true, m_firstNCount);
            TestSettings(false, true, true, true, m_firstNCount);
            TestSettings(false, true, false, true, m_firstNCount);
            TestSettings(false, false, true, true, m_firstNCount);
            TestSettings(false, false, false, true, m_firstNCount);

            TestSettings(true, true, true, false, m_firstNCount);
            TestSettings(true, true, false, false, m_firstNCount);
            TestSettings(true, false, true, false, m_firstNCount);
            TestSettings(true, false, false, false, m_firstNCount);
            TestSettings(false, true, true, false, m_firstNCount);
            TestSettings(false, true, false, false, m_firstNCount);
            TestSettings(false, false, true, false, m_firstNCount);
            TestSettings(false, false, false, false, m_firstNCount);
        }

        private int[] Sort(int[] input, int[] keys, bool reverse)
        {
            int[] result = new int[input.Length];
            Array.Copy(input, result, input.Length);

            IComparer<int> comparer = reverse ? new NegativeComparer() as IComparer<int> : new PositiveComparer() as IComparer<int>;
            Array.Sort(keys, result, 0, Mathf.Min(keys.Length, result.Length), comparer);

            return result;
        }

        private bool RunTest(int count, bool powerOfTwo, bool reverse, bool useNegatives, bool seperateSortList, int firstN)
        {
            count = powerOfTwo ? Mathf.NextPowerOfTwo(count) : count;
            ComputeBuffer values = new ComputeBuffer(count, sizeof(int));
            values.SetCounterValue(0);

            ComputeBuffer keys = new ComputeBuffer(count, sizeof(int));
            keys.SetCounterValue(0);

            int[] data = new int[count];

            for (int i = 0; i < count; i++)
                data[i] = UnityEngine.Random.Range(useNegatives ? -m_randomNumberRange : 0, m_randomNumberRange);

            int[] sortList;

            if (seperateSortList)
            {
                // generate an array of unique ints for the sort list, because if there are repeated ints,
                // it may sort correctly but still show as a failed test because of the ambiguity
                HashSet<int> uniqueInts = new HashSet<int>();

                for (int i = 0; i < count; i++)
                {
                    int randomInt;

                    do
                    {
                        randomInt = UnityEngine.Random.Range(0, 100000);
                    } while (uniqueInts.Contains(randomInt));

                    uniqueInts.Add(randomInt);
                }

                sortList = uniqueInts.ToArray();
            }
            else
            {
                sortList = data;
            }
            
            values.SetData(data);
            keys.SetData(sortList);
            
            using (Sorter sorter = new Sorter(m_sorterComputeShader))
            {
                sorter.Sort(values, keys, reverse, firstN);
            }

            int[] gpuResult = new int[count];
            values.GetData(gpuResult);

            int[] cpuResult;

            if (firstN < 0)
            {
                cpuResult = Sort(data, sortList, reverse);
            }
            else
            {
                firstN = Mathf.Min(firstN, count);

                int[] firstHalf = new int[firstN];
                int[] secondHalf = new int[count - firstN];

                for (int i = 0; i < firstN; i++)
                    firstHalf[i] = data[i];

                for (int i = 0; i < count - firstN; i++)
                    secondHalf[i] = data[i + firstN];

                firstHalf = Sort(firstHalf, sortList, reverse);
                cpuResult = firstHalf.Concat(secondHalf).ToArray();
            }
            
            bool match = cpuResult.Length == gpuResult.Length;

            if (match)
            {
                int valuesThatMatter = firstN < 0 ? cpuResult.Length : Mathf.Min(firstN, cpuResult.Length);

                for (int i = 0; i < valuesThatMatter; i++)
                {
                    if (cpuResult[i] != gpuResult[i])
                    {
                        match = false;
                        break;
                    }
                }
            }

            if (!match)
            {
                Debug.Log($"Input = {ToFormattedString(data)}\nSort List = {ToFormattedString(sortList)}");
                Debug.Log($"GPU: {ToFormattedString(gpuResult)} ({gpuResult.Length}) is not equal to \n CPU: {ToFormattedString(cpuResult)} ({cpuResult.Length})");
            }
            
            values.Dispose();
            values.Release();

            keys.Dispose();
            keys.Release();

            return match;
        }

        #region Helper Methods

        private struct PositiveComparer : IComparer<int>
        {
            public int Compare(int x, int y) => x - y;
        }

        private struct NegativeComparer : IComparer<int>
        {
            public int Compare(int x, int y) => y - x;
        }

        /// <summary>
        /// Add an RGB colour markcup code to the given string.
        /// </summary>
        public static string AddColour(string content, Color colour)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(colour) + ">" + content + "</color>";
        }
        
        /// <summary>
        /// Returns a string of the given array in the form [x, y, z...]
        /// </summary>
        public static string ToFormattedString<T>(IList<T> array) => ToFormattedString(array, 0, array.Count);

        /// <summary>
        /// Returns a string of the given array in the form [x, y, z...]
        /// </summary>
        public static string ToFormattedString<T>(IList<T> array, int startIndex) => ToFormattedString(array, startIndex, array.Count);

        /// <summary>
        /// Returns a string of the given array in the form [x, y, z...]
        /// </summary>
        public static string ToFormattedString<T>(IList<T> array, int startIndex, int endIndex)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");

            endIndex = Mathf.Min(endIndex, array.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                sb.Append(array[i].ToString());

                if (i < endIndex - 1)
                    sb.Append(", ");
            }

            sb.Append("]");

            return sb.ToString();
        }

        #endregion
    }
}
