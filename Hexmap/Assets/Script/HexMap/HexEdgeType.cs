using System;

namespace Alpha.Dol
{
    public enum HexEdgeType
    {
        Flat, Slop, Cliff
    }

    public static class HexEdgeTypeExtension
    {
        public static HexEdgeType GetEdgeType(int evaluation1, int evaluation2)
        {
            if (evaluation1 == evaluation2) return HexEdgeType.Flat;

            if (Math.Abs(evaluation1 - evaluation2) == 1) return HexEdgeType.Slop;
            
            return HexEdgeType.Cliff;
        }
    }
}