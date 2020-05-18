using System;
using UnityEngine;

namespace Alpha.Dol
{
    public class HexGridChunk :MonoBehaviour
    {
        private Canvas _gridCanvas;
        [SerializeField] protected HexMesh _terrain;
        [SerializeField] protected HexMesh _river;
        [SerializeField] protected HexMesh _road;
        [SerializeField] protected HexMesh _water;
        [SerializeField] protected HexMesh _waterShore;
        [SerializeField] protected HexMesh _estuary;
        private HexCell[] _cells;

        static Color sColor1 = new Color(1, 0, 0);
        static Color sColor2 = new Color(0, 1, 0);
        static Color sColor3 = new Color(0, 0, 1);
        
        private void Awake()
        {
            _gridCanvas = GetComponentInChildren<Canvas>();
            _cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
        }

        private void Start()
        {
            Triangulate();
        }

        public void AddCell(int index, HexCell cell)
        {
            _cells[index] = cell;
            cell.transform.SetParent(this.transform, false);
            cell.uiRect.SetParent(this._gridCanvas.transform, false);
            cell.Chunk = this;
        }

        public void Refresh()
        {
            enabled = true;
        }

        private void LateUpdate()
        {
            Triangulate();
            enabled = false;
        }

        private void Triangulate()
        {
            _terrain.Clear();
            _river.Clear();
            _road.Clear();
            _water.Clear();
            _waterShore.Clear();
            _estuary.Clear();
            
            foreach (var cell in _cells)
            {
                Triangulate(cell);
            }
            
            _terrain.Apply();
            _river.Apply();
            _road.Apply();
            _water.Apply();
            _waterShore.Apply();
            _estuary.Apply();
        }
        
        public void Triangulate(HexCell cell)
        {
            var center = cell.transform.position;
            for (var i = HexDirection.NE; i <= HexDirection.NW; i ++)
            {
                var edge = new EdgeVertices(center + HexMetrics.GetFirstSolidCorner(i), center + HexMetrics.GetSecondSolidCorner(i), HexMetrics.edgeOuterStep);

                if (cell.HasRiver)
                {
                    if (cell.HasRiverThroughEdge(i))
                    {
                        edge.v3.y = cell.StreamBedY;
                        
                        if (cell.HasRiverBeginOrEnd)
                        {
                            TriangulateWithRiverBeginOrEnd(i, cell, center, edge);
                        }
                        else
                        {
                            TriangulateWithRiver(i, cell, center, edge);
                        }
                    }
                    else
                    {
                        TriangulateAdjacentToRiver(i, cell, center, edge);
                    }
                }
                else
                {
                    TriangulateWithoutRiver(i, cell, center, edge);
                }

                if (i <= HexDirection.SE)
                {
                    TriangulateConnection(i, cell, edge);
                }

                if (cell.IsUnderWater)
                {
                    TriangulateWater(i, cell, center);
                }
            }
        }

        private void TriangulateWater(HexDirection direction, HexCell cell, Vector3 center)
        {
            center.y = cell.WaterSurfaceY;
            var neighbor = cell.GetNeighbor(direction);
            if (neighbor != null && !neighbor.IsUnderWater)
            {
                TriangulateWaterShore(direction, cell, neighbor , center);
            }
            else
            {
                TriangulateOpenWater(direction, cell, neighbor, center);
            }
        }

        private void TriangulateOpenWater(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
        {
            var c1 = center + HexMetrics.GetFirstWaterCorner(direction);
            var c2 = center + HexMetrics.GetSecondWaterCorner(direction);
            _water.AddTriangle(center, c1, c2);
            if (direction <= HexDirection.SE)
            {
                var bridge = HexMetrics.GetWaterBridge(direction);
                var c3 = bridge + c1;
                var c4 = bridge + c2;
                _water.AddQuad(c1, c2, c3, c4);
            
                if (direction <= HexDirection.E)
                {
                    _water.AddTriangle(c2, c4, c2 + HexMetrics.GetWaterBridge(direction.Next()));
                }
            }
        }

        private void TriangulateWaterShore(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
        {
            var edge = new EdgeVertices(
                center + HexMetrics.GetFirstWaterCorner(direction), 
                center + HexMetrics.GetSecondWaterCorner(direction), 
                HexMetrics.edgeOuterStep);
            _water.AddTriangle(center, edge.v1, edge.v2);
            _water.AddTriangle(center, edge.v2, edge.v3);
            _water.AddTriangle(center, edge.v3, edge.v4);
            _water.AddTriangle(center, edge.v4, edge.v5);

            var center2 = neighbor.Position;
            center2.y = center.y;
            var edge2 = new EdgeVertices(
                center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()), 
                center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite()), 
                HexMetrics.edgeOuterStep);

            if (cell.HasRiverThroughEdge(direction))
            {
                TriangulateEstuary(edge, edge2, cell.IncomingRiver == direction);
            }
            else
            {
                _waterShore.AddQuad(edge.v1, edge.v2, edge2.v1, edge2.v2);
                _waterShore.AddQuad(edge.v2, edge.v3, edge2.v2, edge2.v3);
                _waterShore.AddQuad(edge.v3, edge.v4, edge2.v3, edge2.v4);
                _waterShore.AddQuad(edge.v4, edge.v5, edge2.v4, edge2.v5);
                _waterShore.AddQuadUV(0, 0 ,0 , 1);
                _waterShore.AddQuadUV(0, 0 ,0 , 1);
                _waterShore.AddQuadUV(0, 0 ,0 , 1);
                _waterShore.AddQuadUV(0, 0 ,0 , 1);
            }
            var nextNeighbor = cell.GetNeighbor(direction.Next());
            if (nextNeighbor != null)
            {
                var v3 = nextNeighbor.Position + (nextNeighbor.IsUnderWater
                             ? HexMetrics.GetFirstWaterCorner(direction.Previous())
                             : HexMetrics.GetFirstSolidCorner(direction.Previous()));
                v3.y = center.y;
                _waterShore.AddTriangle(edge.v5, edge2.v5, v3);
                _waterShore.AddTriangleUV(Vector2.zero, Vector2.up, nextNeighbor.IsUnderWater ? Vector2.zero : Vector2.up);
            }
        }

        private void TriangulateEstuary(EdgeVertices edge, EdgeVertices edge2, bool isIncomingRiver)
        {
            _waterShore.AddTriangle(edge2.v1, edge.v2, edge.v1);
            _waterShore.AddTriangle(edge2.v5, edge.v5, edge.v4);
            _waterShore.AddTriangleUV(Vector2.up, Vector2.zero, Vector2.zero);
            _waterShore.AddTriangleUV(Vector2.up, Vector2.zero, Vector2.zero);
            
            _estuary.AddQuad(edge2.v1, edge.v2, edge2.v2, edge.v3);
            _estuary.AddTriangle(edge.v3, edge2.v2, edge2.v4);
            _estuary.AddQuad(edge.v3, edge.v4, edge2.v4, edge2.v5);
            //foam uv
            _estuary.AddQuadUV(Vector2.up, Vector2.zero, Vector2.one, Vector2.zero);
            _estuary.AddTriangleUV(Vector2.zero, Vector2.one, Vector2.one);
            _estuary.AddQuadUV(Vector2.zero, Vector2.zero, Vector2.one, Vector2.up);

            //river uv
            if (isIncomingRiver)
            {
                _estuary.AddQuadUV2(
                    new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f), 
                    new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f));
             
                _estuary.AddTriangleUV2(
                    new Vector2(0.5f, 1.1f), 
                    new Vector2(1f, 0.8f), 
                    new Vector2(0f, 0.8f));
                
                _estuary.AddQuadUV2(
                    new Vector2(0.5f, 1f), new Vector2(0.3f, 1.15f), 
                    new Vector2(0f, 00.8f), new Vector2(-0.5f, 1f));
            }
            else
            {
                _estuary.AddQuadUV2(
                    new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f),
                    new Vector2(0f, 0f), new Vector2(0.5f, -0.3f)
                );
                _estuary.AddTriangleUV2(
                    new Vector2(0.5f, -0.3f),
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f)
                );
                _estuary.AddQuadUV2(
                    new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f),
                    new Vector2(1f, 0f), new Vector2(1.5f, -0.2f)
                );
            }
        }

        private void TriangulateWithoutRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices edge)
        {
            // TriangulateEdgeFan(edge, center, cell.Color);
            TriangulateEdgeFan(edge, center, cell.TerrainTypeIndex);

            if (cell.HasRoads)
            {
                var interpolators = GetRoadInterpolators(direction, cell);
                TriangulateRoad(center, 
                    Vector3.Lerp(center, edge.v1, interpolators.x), 
                    Vector3.Lerp(center, edge.v5, interpolators.y), 
                    edge, cell.HasRoadThroughEdge(direction));
            }
        }

        private void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices edge)
        {
            if (cell.HasRoads)
            {
                TriangulateRoadAdjacentToRiver(direction, cell, center, edge);
            }
            if (cell.HasRiverThroughEdge(direction.Next()))
            {
                if (cell.HasRiverThroughEdge(direction.Previous()))
                {
                    center += HexMetrics.GetSolidEdgeMiddle(direction) * (HexMetrics.innerToOuter * 0.5f);
                }
                else if (cell.HasRiverThroughEdge(direction.Previous().Previous()))
                {
                    center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
                }
            }
            else if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                if (cell.HasRiverThroughEdge(direction.Next().Next()))
                {
                    center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
                }
            }
            var mEdge = new EdgeVertices(
                Vector3.Lerp(center, edge.v1, 0.5f),
                Vector3.Lerp(center, edge.v5, 0.5f), HexMetrics.edgeOuterStep);
            // TriangulateEdgeStrip(mEdge, cell.Color, edge, cell.Color);
            // TriangulateEdgeFan(mEdge, center, cell.Color);
            TriangulateEdgeStrip(mEdge, sColor1, cell.TerrainTypeIndex, edge, sColor1, cell.TerrainTypeIndex);
            TriangulateEdgeFan(mEdge, center, cell.TerrainTypeIndex);
        }

        private void TriangulateRoadAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices edge)
        {
            var hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);
            var previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
            var nextHasRiver = cell.HasRiverThroughEdge(direction.Next());
            var interpolators = GetRoadInterpolators(direction, cell);
            var roadCenter = center;
            if (cell.HasRiverBeginOrEnd)
            {
                roadCenter += HexMetrics.GetSolidEdgeMiddle(cell.RiverBeginOrEndDirection.Opposite()) * 1f / 3f;
            }
            else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite())
            {
                Vector3 corner;
                if (previousHasRiver)
                {
                    if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Next()))
                    {
                        return;
                    }

                    corner = HexMetrics.GetSecondSolidCorner(direction);
                }
                else
                {
                    if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Previous()))
                    {
                        return;
                    }

                    corner = HexMetrics.GetFirstSolidCorner(direction);
                }

                roadCenter += corner * 0.5f;
                center += corner * 0.25f;
            }
            else if (cell.IncomingRiver == cell.OutgoingRiver.Previous())
            {
                roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * 0.2f;
            }
            else if (cell.IncomingRiver == cell.OutgoingRiver.Next())
            {
                roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * 0.2f;
            }
            else if (previousHasRiver && nextHasRiver)
            {
                if (!hasRoadThroughEdge) return;

                var offset = HexMetrics.GetSolidEdgeMiddle(direction) * HexMetrics.innerToOuter;
                roadCenter += offset * 0.7f;
                center += offset * 0.5f;
            }
            else
            {
                HexDirection middle;
                if (previousHasRiver)
                {
                    middle = direction.Next();
                }
                else if (nextHasRiver)
                {
                    middle = direction.Previous();
                }
                else
                {
                    middle = direction;
                }

                if (!cell.HasRoadThroughEdge(middle) &&
                    !cell.HasRoadThroughEdge(middle.Previous()) &&
                    !cell.HasRiverThroughEdge(middle.Next()))
                {
                    return;
                }

                roadCenter += HexMetrics.GetSolidEdgeMiddle(middle) * 0.25f;
            }
            var mL = Vector3.Lerp(roadCenter, edge.v1, interpolators.x);
            var mR = Vector3.Lerp(roadCenter, edge.v5, interpolators.y);
            TriangulateRoad(roadCenter, mL, mR, edge, hasRoadThroughEdge);
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                TriangulateRoadEdge(roadCenter, center, mL);
            }

            if (cell.HasRiverThroughEdge(direction.Next()))
            {
                TriangulateRoadEdge(roadCenter, mR, center);
            }
        }

        private void TriangulateWithRiverBeginOrEnd(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices edge)
        {
            
            var mEdge = new EdgeVertices(
                Vector3.Lerp(center, edge.v1, 0.5f),
                Vector3.Lerp(center, edge.v5, 0.5f), 
                HexMetrics.edgeOuterStep);
            mEdge.v3.y = edge.v3.y;
            // TriangulateEdgeStrip(mEdge, cell.Color, edge, cell.Color);
            // TriangulateEdgeFan(mEdge, center, cell.Color);
            TriangulateEdgeStrip(mEdge, sColor1, cell.TerrainTypeIndex, edge, sColor1, cell.TerrainTypeIndex);
            TriangulateEdgeFan(mEdge, center, cell.TerrainTypeIndex);

            if (!cell.IsUnderWater)
            {
                bool isReverse = cell.HasInComingRiver;
                TriangulateRiverQuad(mEdge.v2, mEdge.v4, edge.v2, edge.v4, cell.RiverSurfaceY, 0.6f, isReverse);
                center.y = mEdge.v2.y = mEdge.v4.y = cell.RiverSurfaceY;
                _river.AddTriangle(center, mEdge.v2, mEdge.v4);
                if (isReverse)
                {
                    _river.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(1,0.2f), new Vector2(0,0.2f));
                }
                else
                {
                    _river.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(0, 0.6f), new Vector2(1,0.6f));
                }    
            }
        }

        private void TriangulateEdgeFan(EdgeVertices edge, Vector3 center, float type)
        {
            _terrain.AddTriangle(center, edge.v1, edge.v2);
            // _terrain.AddTriangleColor(color);
            _terrain.AddTriangle(center, edge.v2, edge.v3);
            // _terrain.AddTriangleColor(color);
            _terrain.AddTriangle(center, edge.v3, edge.v4);
            // _terrain.AddTriangleColor(color);
            _terrain.AddTriangle(center, edge.v4, edge.v5);
            // _terrain.AddTriangleColor(color);
            _terrain.AddTriangleColor(sColor1);
            _terrain.AddTriangleColor(sColor1);
            _terrain.AddTriangleColor(sColor1);
            _terrain.AddTriangleColor(sColor1);
            Vector3 types = new Vector3(type, type, type);
            _terrain.AddTriangleTerrainTypes(types);
            _terrain.AddTriangleTerrainTypes(types);
            _terrain.AddTriangleTerrainTypes(types);
            _terrain.AddTriangleTerrainTypes(types);
        }

        private void TriangulateEdgeStrip(EdgeVertices edge1, Color color1, float type1, EdgeVertices edge2, Color color2, float type2, bool hasRoad = false)
        {
            _terrain.AddQuad(edge1.v1, edge1.v2, edge2.v1, edge2.v2);
            _terrain.AddQuadColor(color1, color2);
            _terrain.AddQuad(edge1.v2, edge1.v3, edge2.v2, edge2.v3);
            _terrain.AddQuadColor(color1, color2);
            _terrain.AddQuad(edge1.v3, edge1.v4, edge2.v3, edge2.v4);
            _terrain.AddQuadColor(color1, color2);
            _terrain.AddQuad(edge1.v4, edge1.v5, edge2.v4, edge2.v5);
            _terrain.AddQuadColor(color1, color2);
            
            var types = new Vector3(type1, type2, type1);
            _terrain.AddQuadTerrainTypes(types);
            _terrain.AddQuadTerrainTypes(types);
            _terrain.AddQuadTerrainTypes(types);
            _terrain.AddQuadTerrainTypes(types);

            if (hasRoad)
            {
                TriangulateRoadSegment(edge1.v2, edge1.v3, edge1.v4, edge2.v2, edge2.v3, edge2.v4);
            }
        }

        private void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float v, bool isReverse)
        {
            v1.y = v2.y = y1;
            v3.y = v4.y = y2;
            _river.AddQuad(v1, v2, v3, v4);
            if (isReverse)
            {
                _river.AddQuadUV(1, 0, 0.8f - v, 0.6f - v);
            }
            else
            {
                _river.AddQuadUV(0, 1, v, v + 0.2f);
            }
        }
        private void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y, float v, bool isReverse)
        {
            TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, isReverse);
        }

        private Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell)
        {
            Vector2 interpolators;
            if (cell.HasRoadThroughEdge(direction))
            {
                interpolators.x = interpolators.y = 0.5f;
            }
            else
            {
                interpolators.x = cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
                interpolators.y = cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
            }

            return interpolators;
        }

        private void TriangulateRoadSegment(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6)
        {
            _road.AddQuad(v1, v2, v4, v5);
            _road.AddQuad(v2, v3, v5, v6);
            _road.AddQuadUV(0, 1, 0, 0);
            _road.AddQuadUV(1, 0, 0, 0);
        }

        private void TriangulateRoad(Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices edge, bool hasRoadThroughCellEdge)
        {
            if (hasRoadThroughCellEdge)
            {
                var mc = Vector3.Lerp(mL, mR, 0.5f);
                TriangulateRoadSegment(mL, mc, mR, edge.v2, edge.v3, edge.v4);
                _road.AddTriangle(center, mL, mc);
                _road.AddTriangle(center, mc, mR);
                _road.AddTriangleUV(new Vector2(1, 0), new Vector2(0, 0), new Vector2(1, 0));
                _road.AddTriangleUV(new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, 0));
            }
            else
            {
                TriangulateRoadEdge(center, mL, mR);
            }
        }

        private void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR)
        {
            _road.AddTriangle(center, mL, mR);
            _road.AddTriangleUV(new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 0));
        }

        private void TriangulateWithRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices edge)
        {
            Vector3 centerL, centerR;

            if (cell.HasRiverThroughEdge(direction.Opposite()))
            {
                centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
                centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
            }
            else if (cell.HasRiverThroughEdge(direction.Next()))
            {
                centerL = center;
                centerR = Vector3.Lerp(center, edge.v5, 2f / 3f);
            }
            else if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                centerL = Vector3.Lerp(center, edge.v1, 2f / 3f);
                centerR = center;
            }
            else if (cell.HasRiverThroughEdge(direction.Next().Next()))
            {
                centerL = center;
                centerR = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.innerToOuter);
            }
            else if (cell.HasRiverThroughEdge(direction.Previous().Previous()))
            {
                centerL = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.innerToOuter);
                centerR = center;
            }
            else
            {
                centerL = centerR = center;
            }
            var mEdge = new EdgeVertices(Vector3.Lerp(centerL, edge.v1, 0.5f), Vector3.Lerp(centerR, edge.v5, 0.5f), HexMetrics.edgeOuterStep);
            mEdge.v3.y = center.y = edge.v3.y = cell.StreamBedY;
            
            // TriangulateEdgeStrip(mEdge, cell.Color, edge, cell.Color);
            TriangulateEdgeStrip(mEdge, sColor1, cell.TerrainTypeIndex, edge, sColor1, cell.TerrainTypeIndex);
            _terrain.AddTriangle(centerL, mEdge.v1, mEdge.v2);
            // _terrain.AddTriangleColor(cell.Color);
            _terrain.AddTriangleColor(sColor1);
            _terrain.AddQuad(centerL, center, mEdge.v2, mEdge.v3);
            // _terrain.AddQuadColor(cell.Color);
            _terrain.AddQuadColor(sColor1);
            _terrain.AddQuad(center, centerR, mEdge.v3, mEdge.v4);
            // _terrain.AddQuadColor(cell.Color);
            _terrain.AddQuadColor(sColor1);
            _terrain.AddTriangle(centerR, mEdge.v4, mEdge.v5);
            // _terrain.AddTriangleColor(cell.Color);
            _terrain.AddTriangleColor(sColor1);

            var types = new Vector3(cell.TerrainTypeIndex, cell.TerrainTypeIndex, cell.TerrainTypeIndex);
            _terrain.AddTriangleTerrainTypes(types);
            _terrain.AddQuadTerrainTypes(types);
            _terrain.AddQuadTerrainTypes(types);
            _terrain.AddTriangleTerrainTypes(types);
            
            if (!cell.IsUnderWater)
            {
                var reverse = cell.IncomingRiver == direction;
                TriangulateRiverQuad(centerL, centerR, mEdge.v2, mEdge.v4, cell.RiverSurfaceY, 0.4f, reverse);
                TriangulateRiverQuad(mEdge.v2, mEdge.v4, edge.v2, edge.v4, cell.RiverSurfaceY, 0.6f, reverse);
            }
        }

        private void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices edge)
        {
            var neighbor = cell.GetNeighbor(direction);
            
            if (neighbor == null) return;

            var bridge = HexMetrics.GetBridge(direction);
            var edgeType = HexEdgeTypeExtension.GetEdgeType(cell.Evaluation, neighbor.Evaluation);
            bridge.y = neighbor.Position.y - cell.Position.y;
            var neighborEdge = new EdgeVertices(edge.v1 + bridge, edge.v5 + bridge, HexMetrics.edgeOuterStep);

            if (cell.HasRiverThroughEdge(direction))
            {
                neighborEdge.v3.y = neighbor.StreamBedY;

                if (!cell.IsUnderWater)
                {
                    if (!neighbor.IsUnderWater)
                    {
                        var isReverse = cell.HasInComingRiver && cell.IncomingRiver == direction;
                        TriangulateRiverQuad(edge.v2, edge.v4, neighborEdge.v2, neighborEdge.v4, cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f, isReverse);
                    }
                    else if (cell.Evaluation > neighbor.WaterLevel)
                    {
                        TriangulateWaterFall(edge.v2, edge.v4, neighborEdge.v2, neighborEdge.v4, cell.RiverSurfaceY, neighbor.RiverSurfaceY, neighbor.WaterSurfaceY);
                    }
                }
                else if (!neighbor.IsUnderWater && neighbor.Evaluation > cell.WaterLevel)
                {
                    TriangulateWaterFall(neighborEdge.v4, neighborEdge.v2, edge.v4, edge.v2, neighbor.RiverSurfaceY, cell.RiverSurfaceY, cell.WaterSurfaceY);
                }
            }

            if (edgeType == HexEdgeType.Slop)
            {
                TriangulateEdgeTerraces(edge, cell, neighborEdge, neighbor, cell.HasRoadThroughEdge(direction));
            }
            else
            {
                // TriangulateEdgeStrip(edge, cell.Color, neighborEdge, neighbor.Color, cell.HasRoadThroughEdge(direction));
                TriangulateEdgeStrip(edge, sColor1, cell.TerrainTypeIndex, neighborEdge, sColor2, neighbor.TerrainTypeIndex, cell.HasRoadThroughEdge(direction));
            }

            
            var nextNeighbor = cell.GetNeighbor(direction.Next());
            if (direction <= HexDirection.E && nextNeighbor != null)
            {
                var v5 = edge.v5 + HexMetrics.GetBridge(direction.Next());
                v5.y = nextNeighbor.Position.y;
            
                if (cell.Evaluation <= neighbor.Evaluation)
                {
                    if (cell.Evaluation <= nextNeighbor.Evaluation)
                    {
                        TriangulateCorner(edge.v5, cell, neighborEdge.v5, neighbor, v5, nextNeighbor);
                    }
                    else
                    {
                        TriangulateCorner(v5, nextNeighbor, edge.v5, cell, neighborEdge.v5, neighbor);
                    }
                }
                else
                {
                    if (neighbor.Evaluation <= nextNeighbor.Evaluation)
                    {
                        TriangulateCorner(neighborEdge.v5, neighbor, v5, nextNeighbor, edge.v5, cell);
                    }
                    else
                    {
                        TriangulateCorner(v5, nextNeighbor, edge.v5, cell, neighborEdge.v5, neighbor);
                    }
                }
            }
        }

        private void TriangulateWaterFall(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float cellWaterSurfaceY)
        {
            v2.y = v1.y = y1;
            v4.y = v3.y = y2;
            v1 = HexMetrics.Perturb(v1);
            v2 = HexMetrics.Perturb(v2);
            v3 = HexMetrics.Perturb(v3);
            v4 = HexMetrics.Perturb(v4);
            var t = (cellWaterSurfaceY - y2) / (y1 - y2);
            v3 = Vector3.Lerp(v3, v1, t);
            v4 = Vector3.Lerp(v4, v2, t);
            _river.AddQuadUnperturb(v1, v2, v3, v4);
            _river.AddQuadUV(0f, 1f, 0.8f, 1f);
        }

        private void TriangulateEdgeTerraces(EdgeVertices edge1, HexCell beginCell, EdgeVertices edge2, HexCell endCell, bool hasRoad)
        {
            float t1 = beginCell.TerrainTypeIndex;
            float t2 = endCell.TerrainTypeIndex;
            var e2 = HexMetrics.GetTerracePosition(edge1, edge2, 1);
            // var c2 = HexMetrics.GetTerraceColor(beginCell.Color, endCell.Color, 1);
            var c2 = HexMetrics.GetTerraceColor(sColor1, sColor2, 1);
            // TriangulateEdgeStrip(edge1, beginCell.Color, e2, c2, hasRoad);
            TriangulateEdgeStrip(edge1, sColor1, t1, e2, c2, t2, hasRoad);

            for (var i = 2; i < HexMetrics.terraceSteps; i ++)
            {
                var e1 = e2;
                var c1 = c2;
                e2 = HexMetrics.GetTerracePosition(edge1, edge2, i);
                // c2 = HexMetrics.GetTerraceColor(beginCell.Color, endCell.Color, i);
                c2 = HexMetrics.GetTerraceColor(sColor1, sColor2, i);
                TriangulateEdgeStrip(e1, c1, t1, e2, c2, t2, hasRoad);
            }
            
            // TriangulateEdgeStrip(e2, c2, edge2, endCell.Color, hasRoad);
            TriangulateEdgeStrip(e2, c2, t1, edge2, sColor2, t2, hasRoad);
        }

        private void TriangulateCorner(
            Vector3 bottom, HexCell bottomCell, 
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell)
        {
            var leftEdge = bottomCell.GetEdgeType(leftCell);
            var rightEdge = bottomCell.GetEdgeType(rightCell);
            var topEdge = leftCell.GetEdgeType(rightCell);

            if (leftEdge == HexEdgeType.Slop)
            {
                if (rightEdge == HexEdgeType.Slop)
                {
                    TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
                }
                else if (rightEdge == HexEdgeType.Flat)
                {
                    TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
                }
                else
                {
                    TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
                }
            }
            else if (rightEdge == HexEdgeType.Slop)
            {
                if (leftEdge == HexEdgeType.Slop)
                {
                    TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
                }
                else if (leftEdge == HexEdgeType.Flat)
                {
                    TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
                }
                else
                {
                    TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
                }
            }
            else if (topEdge == HexEdgeType.Slop)
            {
                if (leftCell.Evaluation < rightCell.Evaluation)
                {
                    TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
                }
                else
                {
                    TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
                }
            }
            else
            {
                _terrain.AddTriangle(bottom, left, right);
                // _terrain.AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
                _terrain.AddTriangleColor(sColor1, sColor2, sColor3);
                var types = new Vector3(bottomCell.TerrainTypeIndex, leftCell.TerrainTypeIndex, rightCell.TerrainTypeIndex);
                _terrain.AddTriangleTerrainTypes(types);
            }
        }

        private void TriangulateBoundaryTriangle(Vector3 begin, Color beginColor, Vector3 left, Color leftColor,
            Vector3 boundary, Color boundaryColor, Vector3 types)
        {
            var v2 = HexMetrics.Perturb(HexMetrics.GetTerracePosition(begin, left, 1));
            var c2 = HexMetrics.GetTerraceColor(beginColor, leftColor, 1);
            _terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
            _terrain.AddTriangleColor(beginColor, c2, boundaryColor);
            _terrain.AddTriangleTerrainTypes(types);

            for (var i = 2; i < HexMetrics.terraceSteps; i++)
            {
                var v1 = v2;
                var c1 = c2;
                v2 = HexMetrics.Perturb(HexMetrics.GetTerracePosition(begin, left, i));
                c2 = HexMetrics.GetTerraceColor(beginColor, leftColor, i);
                _terrain.AddTriangleUnperturbed(v1, v2, boundary);
                _terrain.AddTriangleColor(c1, c2, boundaryColor);
                _terrain.AddTriangleTerrainTypes(types);
            }
            
            _terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
            _terrain.AddTriangleColor(c2, leftColor, boundaryColor);
            _terrain.AddTriangleTerrainTypes(types);
        }
        
        private void TriangulateCornerCliffTerraces(
            Vector3 begin,
            HexCell beginCell,
            Vector3 endLeft,
            HexCell endLeftCell,
            Vector3 endRight,
            HexCell endRightCell)
        {
            var b = Math.Abs(1f / (endLeftCell.Evaluation - beginCell.Evaluation));
            var boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(endLeft), b);
            // var boundaryColor = Color.Lerp(beginCell.Color, endLeftCell.Color, b);
            var boundaryColor = Color.Lerp(sColor1, sColor3, b);
            var types = new Vector3(beginCell.TerrainTypeIndex, endLeftCell.TerrainTypeIndex, endRightCell.TerrainTypeIndex);
            
            // TriangulateBoundaryTriangle(endRight, endRightCell, begin, beginCell, boundary, boundaryColor);
            TriangulateBoundaryTriangle(endRight, sColor1, begin, sColor2, boundary, boundaryColor, types);

            if (endLeftCell.GetEdgeType(endRightCell) == HexEdgeType.Slop)
            {
                // TriangulateBoundaryTriangle(endLeft, endLeftCell, endRight, endRightCell, boundary, boundaryColor);
                TriangulateBoundaryTriangle(endLeft, sColor2, endRight, sColor3, boundary, boundaryColor, types);
            }
            else
            {
                _terrain.AddTriangleUnperturbed(HexMetrics.Perturb(endLeft), HexMetrics.Perturb(endRight), boundary);
                // _terrain.AddTriangleColor(endLeftCell.Color, endRightCell.Color, boundaryColor);
                _terrain.AddTriangleColor(sColor2, sColor3, boundaryColor);
                _terrain.AddTriangleTerrainTypes(types);
            }
        }

        private void TriangulateCornerTerracesCliff(
            Vector3 begin,
            HexCell beginCell,
            Vector3 endLeft,
            HexCell endLeftCell,
            Vector3 endRight,
            HexCell endRightCell)
        {
            var b = Math.Abs(1f / (endRightCell.Evaluation - beginCell.Evaluation));
            var boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(endRight), b);
            // var boundaryColor = Color.Lerp(beginCell.Color, endRightCell.Color, b);
            var boundaryColor = Color.Lerp(sColor1, sColor2, b);
            var types = new Vector3(beginCell.TerrainTypeIndex, endLeftCell.TerrainTypeIndex, endRightCell.TerrainTypeIndex);
            
            // TriangulateBoundaryTriangle(begin, beginCell, endLeft, endLeftCell, boundary, boundaryColor);
            TriangulateBoundaryTriangle(begin, sColor3, endLeft, sColor1, boundary, boundaryColor, types);

            if (endLeftCell.GetEdgeType(endRightCell) == HexEdgeType.Slop)
            {
                // TriangulateBoundaryTriangle(endLeft, endLeftCell, endRight, endRightCell, boundary, boundaryColor);
                TriangulateBoundaryTriangle(endLeft, sColor2, endRight, sColor3, boundary, boundaryColor, types);
            }
            else
            {
                _terrain.AddTriangleUnperturbed(HexMetrics.Perturb(endLeft), HexMetrics.Perturb(endRight), boundary);
                // _terrain.AddTriangleColor(endLeftCell.Color, endRightCell.Color, boundaryColor);
                _terrain.AddTriangleColor(sColor2, sColor3, boundaryColor);
                _terrain.AddTriangleTerrainTypes(types);
            }
        }

        private void TriangulateCornerTerraces(
            Vector3 begin,
            HexCell beginCell,
            Vector3 endLeft,
            HexCell endLeftCell,
            Vector3 endRight,
            HexCell endRightCell)
        {
            var v3 = HexMetrics.GetTerracePosition(begin, endLeft, 1);
            var v4 = HexMetrics.GetTerracePosition(begin, endRight, 1);
            // var c3 = HexMetrics.GetTerraceColor(beginCell.Color, endLeftCell.Color, 1);
            // var c4 = HexMetrics.GetTerraceColor(beginCell.Color, endRightCell.Color, 1);
            var c3 = HexMetrics.GetTerraceColor(sColor1, sColor2, 1);
            var c4 = HexMetrics.GetTerraceColor(sColor1, sColor2, 1);
            var types = new Vector3(beginCell.TerrainTypeIndex, endLeftCell.TerrainTypeIndex, endRightCell.TerrainTypeIndex);
            _terrain.AddTriangle(begin, v3, v4);
            // _terrain.AddTriangleColor(beginCell.Color, c3, c4);
            _terrain.AddTriangleColor(sColor1, c3, c4);
            _terrain.AddTriangleTerrainTypes(types);

            for (var i = 2; i < HexMetrics.terraceSteps; i++)
            {
                var v1 = v3;
                var v2 = v4;
                var c1 = c3;
                var c2 = c4;
                v3 = HexMetrics.GetTerracePosition(begin, endLeft, i);
                v4 = HexMetrics.GetTerracePosition(begin, endRight, i);
                // c3 = HexMetrics.GetTerraceColor(beginCell.Color, endLeftCell.Color, i);
                // c4 = HexMetrics.GetTerraceColor(beginCell.Color, endRightCell.Color, i);
                c3 = HexMetrics.GetTerraceColor(sColor1, sColor2, i);
                c4 = HexMetrics.GetTerraceColor(sColor1, sColor3, i);
                _terrain.AddQuad(v1,v2,v3,v4);
                _terrain.AddQuadColor(c1, c2, c3, c4);
                _terrain.AddQuadTerrainTypes(types);
            }
            
            _terrain.AddQuad(v3, v4, endLeft, endRight);
            // _terrain.AddQuadColor(c3, c4, endLeftCell.Color, endRightCell.Color);
            _terrain.AddQuadColor(c3, c4, sColor2, sColor3);
            _terrain.AddQuadTerrainTypes(types);
        }
    }
}