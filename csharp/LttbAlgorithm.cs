using System;
using System.Collections.Generic;

namespace LttbDownsample
{
    /// <summary>
    /// Provides Largest-Triangle-Three-Buckets downsampling for StationPoint data.
    /// </summary>
    public static class LttbAlgorithm
    {
        /// <summary>
        /// Downsamples the given StationPoint list to at most <paramref name="threshold" /> points.
        /// </summary>
        /// <param name="data">List of input points.</param>
        /// <param name="threshold">Maximum number of returned points.</param>
        /// <returns>Downsampled list with first and last point preserved.</returns>
        public static List<StationPoint> LargestTriangleThreeBuckets(List<StationPoint> data, int threshold)
        {
            if (data == null) throw new ArgumentNullException("data");

            // Remove points without height information
            var filtered = new List<StationPoint>(data.Count);
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].UTM_Height.HasValue)
                {
                    filtered.Add(data[i]);
                }
            }

            int len = filtered.Count;
            if (threshold >= len || threshold < 3)
            {
                return new List<StationPoint>(filtered);
            }

            // Convert DateTime to milliseconds since Unix epoch once
            var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var xData = new double[len];
            var yData = new double[len];
            for (int i = 0; i < len; i++)
            {
                StationPoint p = filtered[i];
                xData[i] = (p.Xms.ToUniversalTime() - unixEpoch).TotalMilliseconds;
                yData[i] = (double)(p.UTM_Height ?? 0m);
            }

            var sampled = new List<StationPoint>(threshold);
            sampled.Add(filtered[0]);

            double every = (double)(len - 2) / (threshold - 2);

            int a = 0;
            int nextA = 0;

            for (int i = 0; i < threshold - 2; i++)
            {
                int avgStart = (int)(Math.Floor((i + 1) * every) + 1);
                int avgEnd = (int)(Math.Floor((i + 2) * every) + 1);
                if (avgEnd > len) avgEnd = len;

                double avgX = 0;
                double avgY = 0;
                int avgCount = avgEnd - avgStart;
                for (int j = avgStart; j < avgEnd; j++)
                {
                    avgX += xData[j];
                    avgY += yData[j];
                }
                avgX /= avgCount;
                avgY /= avgCount;

                int rangeStart = (int)(Math.Floor(i * every) + 1);
                int rangeEnd = (int)(Math.Floor((i + 1) * every) + 1);

                double pointAx = xData[a];
                double pointAy = yData[a];

                double maxArea = -1d;
                int maxAreaIndex = rangeStart;

                for (int j = rangeStart; j < rangeEnd; j++)
                {
                    double area = Math.Abs((pointAx - avgX) * (yData[j] - pointAy) -
                                           (pointAx - xData[j]) * (avgY - pointAy)) * 0.5d;
                    if (area > maxArea)
                    {
                        maxArea = area;
                        maxAreaIndex = j;
                    }
                }

                sampled.Add(filtered[maxAreaIndex]);
                nextA = maxAreaIndex;
                a = nextA;
            }

            sampled.Add(filtered[len - 1]);
            return sampled;
        }
    }

    /// <summary>
    /// Represents a measurement point with UTM coordinates.
    /// </summary>
    public class StationPoint
    {
        public DateTime Xms;
        public decimal? UTM_East;
        public decimal? UTM_North;
        public decimal? UTM_Height;
    }

    /* Mini Unit-Test-Snippet
    var result = LttbAlgorithm.LargestTriangleThreeBuckets(data, 15000);
    Debug.Assert(result.Count <= 15000);
    Debug.Assert(result[0] == data[0]);
    Debug.Assert(result[result.Count - 1] == data[data.Count - 1]);
    */
}
