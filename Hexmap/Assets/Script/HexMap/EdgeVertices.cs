using UnityEngine;

namespace Alpha.Dol
{
    public struct EdgeVertices
    {
        public Vector3 v1, v2, v3, v4, v5;

        public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep)
        {
            v1 = corner1;
            v2 = Vector3.Lerp(corner1, corner2, outerStep);
            v3 = Vector3.Lerp(corner1, corner2, 0.5f);
            v4 = Vector3.Lerp(corner1, corner2, 1 - outerStep);
            v5 = corner2;
        }
    }
}