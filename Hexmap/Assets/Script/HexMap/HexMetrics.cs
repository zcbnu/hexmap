using System.Collections;
using UnityEngine;

namespace Alpha.Dol
{
    public class HexMetrics
    {
        public const float outerRadius = 10f;
        public const float innerRadius = outerRadius * outerToInner;
        public const float solidFactor = 0.75f;
        public const float waterFactor = 0.5f;
        public const float blendFactor = 1 - solidFactor;
        public const float waterBlendFactor = 1 - waterFactor;
        public const float elevationStep = 6f;
        public const int terracesPerSlop = 2;
        public const int terraceSteps = terracesPerSlop * 2 + 1;
        public const float horizontalTerraceStepSize = 1f / terraceSteps;
        public const float verticalTerraceStepSize = 1f / (terracesPerSlop + 1);
        public const float cellPerturbStrength = 1.5f;
        public const float elevationPerturbStrength = 1f;
        public const int chunkSizeX = 5;
        public const int chunkSizeZ = 5;
        public const float edgeOuterStep = 1f / 6;
        public const float streamBedElevationOffset = -1f;
        public const float waterSurfaceElevationOffset = -0.3f;
        public const float outerToInner = 0.866025404f;
        public const float innerToOuter = 1f / outerToInner;
        
        public static Vector3[] corners = new Vector3[] {
            new Vector3(0f, 0f, outerRadius),
            new Vector3(innerRadius, 0f, 0.5f * outerRadius),
            new Vector3(innerRadius, 0f, -0.5f * outerRadius),
            new Vector3(0f, 0f, -outerRadius),
            new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
            new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
            new Vector3(0f, 0f, outerRadius)
        };

        public static Texture2D noiseSource;

        public static Vector3 GetFirstCorner(HexDirection direction)
        {
            return corners[(int) direction];
        }

        public static Vector3 GetSecondCorner(HexDirection direction)
        {
            return corners[(int) direction + 1];
        }

        public static Vector3 GetFirstSolidCorner(HexDirection direction)
        {
            return corners[(int) direction] * solidFactor;
        }

        public static Vector3 GetSecondSolidCorner(HexDirection direction)
        {
            return corners[(int) direction + 1] * solidFactor;
        }

        public static Vector3 GetFirstWaterCorner(HexDirection direction)
        {
            return corners[(int) direction] * waterFactor;
        }

        public static Vector3 GetSecondWaterCorner(HexDirection direction)
        {
            return corners[(int) direction + 1] * solidFactor;
        }

        public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
        {
            return (corners[(int) direction] + corners[(int) direction + 1]) * (0.5f * solidFactor);
        }

        public static Vector3 GetBridge(HexDirection direction)
        {
            return (corners[(int) direction] + corners[(int) direction + 1]) * blendFactor;
        }

        public static Vector3 GetWaterBridge(HexDirection direction)
        {
            return (corners[(int) direction] + corners[(int) direction + 1]) * waterBlendFactor;
        }

        public static Vector3 GetTerracePosition(Vector3 startPosition, Vector3 endPosition, int step)
        {
            var hStep = horizontalTerraceStepSize * step;
            var vStep = verticalTerraceStepSize * ((step + 1) / 2);
            startPosition.x += (endPosition - startPosition).x * hStep;
            startPosition.z += (endPosition - startPosition).z * hStep;
            startPosition.y += (endPosition - startPosition).y * vStep;
            return startPosition;
        }

        public static EdgeVertices GetTerracePosition(EdgeVertices vertices1, EdgeVertices vertices2, int step)
        {
            EdgeVertices vertices;
            vertices.v1 = HexMetrics.GetTerracePosition(vertices1.v1, vertices2.v1, step);
            vertices.v2 = HexMetrics.GetTerracePosition(vertices1.v2, vertices2.v2, step);
            vertices.v3 = HexMetrics.GetTerracePosition(vertices1.v3, vertices2.v3, step);
            vertices.v4 = HexMetrics.GetTerracePosition(vertices1.v4, vertices2.v4, step);
            vertices.v5 = HexMetrics.GetTerracePosition(vertices1.v5, vertices2.v5, step);
            return vertices;
        }

        public static Color GetTerraceColor(Color startColor, Color endColor, int step)
        {
            var h = step * horizontalTerraceStepSize;
            return Color.Lerp(startColor, endColor, h);
        }

        public static Vector4 SampleNoise(Vector3 position)
        {
            return noiseSource.GetPixelBilinear(position.x, position.z);
        }
        
        public static Vector3 Perturb(Vector3 position)
        {
            var sample = HexMetrics.SampleNoise(position);
            position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
            position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;
            return position;
        }
    }
}