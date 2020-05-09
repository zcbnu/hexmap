using System;
using UnityEngine;

namespace Alpha.Dol
{
    public class HexGridChunk :MonoBehaviour
    {
        private Canvas _gridCanvas;
        [SerializeField] protected HexMesh _terrain;
        [SerializeField] protected HexMesh _river;
        private HexCell[] _cells;

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
            
            foreach (var cell in _cells)
            {
                // _terrain.Triangulate(cell);
                // _river.Triangulate(cell);
                Triangulate(cell);
            }
            
            _terrain.Apply();
            _river.Apply();
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
                    TriangulateEdgeFan(edge, center, cell.Color);
                }

                if (i <= HexDirection.SE)
                {
                    TriangulateConnection(i, cell, edge);
                }
            }
        }

        private void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices edge)
        {
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
            TriangulateEdgeStrip(mEdge, cell.Color, edge, cell.Color);
            TriangulateEdgeFan(mEdge, center, cell.Color);
        }

        private void TriangulateWithRiverBeginOrEnd(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices edge)
        {
            var mEdge = new EdgeVertices(
                Vector3.Lerp(center, edge.v1, 0.5f),
                Vector3.Lerp(center, edge.v5, 0.5f), 
                HexMetrics.edgeOuterStep);
            mEdge.v3.y = edge.v3.y;
            TriangulateEdgeStrip(mEdge, cell.Color, edge, cell.Color);
            TriangulateEdgeFan(mEdge, center, cell.Color);

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

        private void TriangulateEdgeFan(EdgeVertices edge, Vector3 center, Color color)
        {
            _terrain.AddTriangle(center, edge.v1, edge.v2);
            _terrain.AddTriangleColor(color);
            _terrain.AddTriangle(center, edge.v2, edge.v3);
            _terrain.AddTriangleColor(color);
            _terrain.AddTriangle(center, edge.v3, edge.v4);
            _terrain.AddTriangleColor(color);
            _terrain.AddTriangle(center, edge.v4, edge.v5);
            _terrain.AddTriangleColor(color);
        }

        private void TriangulateEdgeStrip(EdgeVertices edge1, Color color1, EdgeVertices edge2, Color color2)
        {
            _terrain.AddQuad(edge1.v1, edge1.v2, edge2.v1, edge2.v2);
            _terrain.AddQuadColor(color1, color2);
            _terrain.AddQuad(edge1.v2, edge1.v3, edge2.v2, edge2.v3);
            _terrain.AddQuadColor(color1, color2);
            _terrain.AddQuad(edge1.v3, edge1.v4, edge2.v3, edge2.v4);
            _terrain.AddQuadColor(color1, color2);
            _terrain.AddQuad(edge1.v4, edge1.v5, edge2.v4, edge2.v5);
            _terrain.AddQuadColor(color1, color2);
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
            
            TriangulateEdgeStrip(mEdge, cell.Color, edge, cell.Color);
            _terrain.AddTriangle(centerL, mEdge.v1, mEdge.v2);
            _terrain.AddTriangleColor(cell.Color);
            _terrain.AddQuad(centerL, center, mEdge.v2, mEdge.v3);
            _terrain.AddQuadColor(cell.Color);
            _terrain.AddQuad(center, centerR, mEdge.v3, mEdge.v4);
            _terrain.AddQuadColor(cell.Color);
            _terrain.AddTriangle(centerR, mEdge.v4, mEdge.v5);
            _terrain.AddTriangleColor(cell.Color);

            var reverse = cell.IncomingRiver == direction;
            TriangulateRiverQuad(centerL, centerR, mEdge.v2, mEdge.v4, cell.RiverSurfaceY, 0.4f, reverse);
            TriangulateRiverQuad(mEdge.v2, mEdge.v4, edge.v2, edge.v4, cell.RiverSurfaceY, 0.6f, reverse);
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

                var isReverse = cell.HasInComingRiver && cell.IncomingRiver == direction;
                TriangulateRiverQuad(edge.v2, edge.v4, neighborEdge.v2, neighborEdge.v4, cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f, isReverse);
            }

            if (edgeType == HexEdgeType.Slop)
            {
                TriangulateEdgeTerraces(edge, cell, neighborEdge, neighbor);
            }
            else
            {
                TriangulateEdgeStrip(edge, cell.Color, neighborEdge, neighbor.Color);
            }

            
            var nextNeighbor = cell.GetNeighbor(direction.Next());
            if (direction <= HexDirection.E && nextNeighbor != null)
            {
                var v5 = edge.v5 + HexMetrics.GetBridge(direction.Next());
                v5.y = nextNeighbor.Position.y * HexMetrics.elevationStep;
            
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

        private void TriangulateEdgeTerraces(EdgeVertices edge1, HexCell beginCell, EdgeVertices edge2, HexCell endCell)
        {
            var e2 = HexMetrics.GetTerracePosition(edge1, edge2, 1);
            var c2 = HexMetrics.GetTerraceColor(beginCell.Color, endCell.Color, 1);
            TriangulateEdgeStrip(edge1, beginCell.Color, e2, c2);

            for (var i = 2; i < HexMetrics.terraceSteps; i ++)
            {
                var e1 = e2;
                var c1 = c2;
                e2 = HexMetrics.GetTerracePosition(edge1, edge2, i);
                c2 = HexMetrics.GetTerraceColor(beginCell.Color, endCell.Color, i);
                TriangulateEdgeStrip(e1, c1, e2, c2);
            }
            
            TriangulateEdgeStrip(e2, c2, edge2, endCell.Color);
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
                _terrain.AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
            }
        }

        private void TriangulateBoundaryTriangle(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell,
            Vector3 boundary, Color boundaryColor)
        {
            var v2 = HexMetrics.Perturb(HexMetrics.GetTerracePosition(begin, left, 1));
            var c2 = HexMetrics.GetTerraceColor(beginCell.Color, leftCell.Color, 1);
            _terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
            _terrain.AddTriangleColor(beginCell.Color, c2, boundaryColor);

            for (var i = 2; i < HexMetrics.terraceSteps; i++)
            {
                var v1 = v2;
                var c1 = c2;
                v2 = HexMetrics.Perturb(HexMetrics.GetTerracePosition(begin, left, i));
                c2 = HexMetrics.GetTerraceColor(beginCell.Color, leftCell.Color, i);
                _terrain.AddTriangleUnperturbed(v1, v2, boundary);
                _terrain.AddTriangleColor(c1, c2, boundaryColor);
            }
            
            _terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
            _terrain.AddTriangleColor(c2, leftCell.Color, boundaryColor);
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
            var boundaryColor = Color.Lerp(beginCell.Color, endLeftCell.Color, b);
            
            TriangulateBoundaryTriangle(endRight, endRightCell, begin, beginCell, boundary, boundaryColor);

            if (endLeftCell.GetEdgeType(endRightCell) == HexEdgeType.Slop)
            {
                TriangulateBoundaryTriangle(endLeft, endLeftCell, endRight, endRightCell, boundary, boundaryColor);
            }
            else
            {
                _terrain.AddTriangleUnperturbed(HexMetrics.Perturb(endLeft), HexMetrics.Perturb(endRight), boundary);
                _terrain.AddTriangleColor(endLeftCell.Color, endRightCell.Color, boundaryColor);
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
            var boundaryColor = Color.Lerp(beginCell.Color, endRightCell.Color, b);
            
            TriangulateBoundaryTriangle(begin, beginCell, endLeft, endLeftCell, boundary, boundaryColor);

            if (endLeftCell.GetEdgeType(endRightCell) == HexEdgeType.Slop)
            {
                TriangulateBoundaryTriangle(endLeft, endLeftCell, endRight, endRightCell, boundary, boundaryColor);
            }
            else
            {
                _terrain.AddTriangleUnperturbed(HexMetrics.Perturb(endLeft), HexMetrics.Perturb(endRight), boundary);
                _terrain.AddTriangleColor(endLeftCell.Color, endRightCell.Color, boundaryColor);
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
            var c3 = HexMetrics.GetTerraceColor(beginCell.Color, endLeftCell.Color, 1);
            var c4 = HexMetrics.GetTerraceColor(beginCell.Color, endRightCell.Color, 1);
            _terrain.AddTriangle(begin, v3, v4);
            _terrain.AddTriangleColor(beginCell.Color, c3, c4);

            for (var i = 2; i < HexMetrics.terraceSteps; i++)
            {
                var v1 = v3;
                var v2 = v4;
                var c1 = c3;
                var c2 = c4;
                v3 = HexMetrics.GetTerracePosition(begin, endLeft, i);
                v4 = HexMetrics.GetTerracePosition(begin, endRight, i);
                c3 = HexMetrics.GetTerraceColor(beginCell.Color, endLeftCell.Color, i);
                c4 = HexMetrics.GetTerraceColor(beginCell.Color, endRightCell.Color, i);
                _terrain.AddQuad(v1,v2,v3,v4);
                _terrain.AddQuadColor(c1, c2, c3, c4);
            }
            
            _terrain.AddQuad(v3, v4, endLeft, endRight);
            _terrain.AddQuadColor(c3, c4, endLeftCell.Color, endRightCell.Color);
        }
    }
}