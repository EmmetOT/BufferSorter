using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Text;

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
            void TestSettings(bool powerOfTwo, bool reverse, bool useNegatives, int firstN = -1)
            {
                int failed = -1;
                for (int i = 0; i < testRuns; i++)
                {
                    if (!RunTest(count, powerOfTwo, reverse, useNegatives, firstN))
                    {
                        failed = i;
                        break;
                    }
                }

                if (failed < 0)
                {
                    Debug.Log(AddColour("Success on settings: ", Color.green) + $"Power Of Two = {powerOfTwo}, Reverse = {reverse}, Negatives = {useNegatives}, FirstN = {firstN}");
                }
                else
                {
                    Debug.Log(AddColour($"Failure at attempt {failed} on settings: ", Color.red) + $"Power Of Two = {powerOfTwo}, Reverse = {reverse}, Negatives = {useNegatives}, FirstN = {firstN}");
                }
            }

            TestSettings(true, true, true);
            TestSettings(true, true, false);
            TestSettings(true, false, true);
            TestSettings(true, false, false);
            TestSettings(false, true, true);
            TestSettings(false, true, false);
            TestSettings(false, false, true);
            TestSettings(false, false, false);

            TestSettings(true, true, true, m_firstNCount);
            TestSettings(true, true, false, m_firstNCount);
            TestSettings(true, false, true, m_firstNCount);
            TestSettings(true, false, false, m_firstNCount);
            TestSettings(false, true, true, m_firstNCount);
            TestSettings(false, true, false, m_firstNCount);
            TestSettings(false, false, true, m_firstNCount);
            TestSettings(false, false, false, m_firstNCount);
        }

        private bool RunTest(int count, bool powerOfTwo, bool reverse, bool useNegatives, int firstN)
        {
            count = powerOfTwo ? Mathf.NextPowerOfTwo(count) : count;
            ComputeBuffer values = new ComputeBuffer(count, sizeof(int));
            values.SetCounterValue(0);

            int[] data = new int[count];

            for (int i = 0; i < count; i++)
                data[i] = Random.Range(useNegatives ? -m_randomNumberRange : 0, m_randomNumberRange);

            values.SetData(data);

            using (Sorter sorter = new Sorter(m_sorterComputeShader))
            {
                sorter.Sort(values, reverse, firstN);
            }

            int[] gpuResult = new int[count];
            values.GetData(gpuResult);

            int[] cpuResult;

            if (firstN < 0)
            {
                cpuResult = data.OrderBy(v => reverse ? -v : v).ToArray();
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

                firstHalf = firstHalf.OrderBy(v => reverse ? -v : v).ToArray();

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
                Debug.Log($"GPU: {ToFormattedString(gpuResult)} ({gpuResult.Length}) is not equal to \n CPU: {ToFormattedString(cpuResult)} ({cpuResult.Length})");

            values.Dispose();
            values.Release();

            return match;
        }

        #region Helper Methods

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
