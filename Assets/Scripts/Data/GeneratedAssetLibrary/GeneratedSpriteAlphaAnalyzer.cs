using System;

namespace EmberCrpg.Data.GeneratedAssets
{
    public static class GeneratedSpriteAlphaAnalyzer
    {
        public static GeneratedSpriteAlphaAnalysis Analyze(int width, int height, byte[] alpha, byte threshold, int minLargeComponentPixels)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (alpha == null || alpha.Length != width * height) throw new ArgumentException("Alpha buffer size does not match dimensions.", nameof(alpha));

            var visited = new bool[alpha.Length];
            var queue = new int[alpha.Length];
            var analysis = new GeneratedSpriteAlphaAnalysis();
            var bestCount = 0;
            var secondBestCount = 0;
            int[] bestIndices = null;

            for (var i = 0; i < alpha.Length; i++)
            {
                if (visited[i] || alpha[i] < threshold) continue;
                var component = Flood(width, height, alpha, visited, threshold, i, queue);
                analysis.opaquePixelCount += component.count;
                if (component.count >= minLargeComponentPixels) analysis.largeComponentCount++;
                if (component.count > bestCount)
                {
                    secondBestCount = bestCount;
                    bestCount = component.count;
                    bestIndices = new int[component.count];
                    Buffer.BlockCopy(queue, 0, bestIndices, 0, component.count * sizeof(int));
                    analysis.mainComponentPixels = component.count;
                    analysis.mainBounds = new PixelRect(component.minX, component.minY, component.maxX - component.minX + 1, component.maxY - component.minY + 1);
                }
                else if (component.count > secondBestCount)
                {
                    secondBestCount = component.count;
                }
            }

            if (bestIndices != null)
            {
                analysis.mainComponentMask = new byte[alpha.Length];
                for (var i = 0; i < bestIndices.Length; i++)
                    analysis.mainComponentMask[bestIndices[i]] = 255;
            }

            analysis.touchesEdge = analysis.mainBounds.x <= 0
                || analysis.mainBounds.y <= 0
                || analysis.mainBounds.x + analysis.mainBounds.width >= width
                || analysis.mainBounds.y + analysis.mainBounds.height >= height;
            analysis.aspectRatio = analysis.mainBounds.height <= 0 ? 0f : (float)analysis.mainBounds.width / analysis.mainBounds.height;
            if (analysis.largeComponentCount > 1) analysis.warnings.Add("multiple_large_components");
            if (analysis.touchesEdge) analysis.warnings.Add("main_component_touches_edge");
            if (analysis.aspectRatio > 1.2f) analysis.warnings.Add("main_component_is_wide");
            analysis.secondComponentPixels = secondBestCount;
            return analysis;
        }

        private static (int count, int minX, int minY, int maxX, int maxY) Flood(int width, int height, byte[] alpha, bool[] visited, byte threshold, int startIndex, int[] queue)
        {
            var head = 0;
            var tail = 0;
            queue[tail++] = startIndex;
            visited[startIndex] = true;

            var count = 0;
            var minX = width;
            var minY = height;
            var maxX = 0;
            var maxY = 0;

            while (head < tail)
            {
                var index = queue[head++];
                var x = index % width;
                var y = index / width;
                count++;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;

                Visit(width, height, alpha, visited, threshold, x - 1, y, queue, ref tail);
                Visit(width, height, alpha, visited, threshold, x + 1, y, queue, ref tail);
                Visit(width, height, alpha, visited, threshold, x, y - 1, queue, ref tail);
                Visit(width, height, alpha, visited, threshold, x, y + 1, queue, ref tail);
            }

            return (count, minX, minY, maxX, maxY);
        }

        private static void Visit(int width, int height, byte[] alpha, bool[] visited, byte threshold, int x, int y, int[] queue, ref int tail)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            var index = y * width + x;
            if (visited[index] || alpha[index] < threshold) return;
            visited[index] = true;
            queue[tail++] = index;
        }
    }
}
