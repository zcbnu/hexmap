using System;
using UnityEngine;

namespace Alpha.Dol
{
    [Serializable]
    public class HexCoordinate
    {
        [SerializeField] private int x, z;
        public int X => x;
        public int Z => z;

        public int Y => -X - Z;
        public HexCoordinate(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        public static HexCoordinate CreateFromOffset(int x, int z)
        {
            return new HexCoordinate(x - z/2, z);
        }

        public static HexCoordinate CreateFromPosition(Vector3 position)
        {
            var x = position.x / (HexMetrics.innerRadius * 2f);
            var y = -x;
            var offset = position.z / (HexMetrics.outerRadius * 3f);
            x -= offset;
            y -= offset;
            var ix = Mathf.RoundToInt(x);
            var iy = Mathf.RoundToInt(y);
            var iz = Mathf.RoundToInt(-x -y);

            if (ix + iy + iz != 0)
            {
                var dx = Mathf.Abs(x - ix);
                var dy = Mathf.Abs(x - iy);
                var dz = Mathf.Abs(x - iz);
                if (dx > dy && dx > dz)
                {
                    ix = -iy - iz;
                }
                else if (dz > dy)
                {
                    iz = -ix - iy;
                }
            }
            
            return new HexCoordinate(ix, iz);
        }

        public override string ToString()
        {
            return $"({X},{Y},{Z})";
        }

        public string ToSplitString()
        {
            return $"{X}\n{Y}\n{Z}";
        }

        public int DistanceTo(HexCoordinate coordinate)
        {
            return ((x < coordinate.x ? coordinate.x - this.x : this.x - coordinate.x) 
                   + (Y < coordinate.Y ? coordinate.Y - this.Y : this.Y - coordinate.Y) 
                   + (Z < coordinate.z ? coordinate.z - this.z : this.z - coordinate.z)) / 2;
        }
    }
}