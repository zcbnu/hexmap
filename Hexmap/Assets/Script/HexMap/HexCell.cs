using System.Linq;
using UnityEngine;

namespace Alpha.Dol
{
    public class HexCell : MonoBehaviour
    {
        public HexCoordinate HexCoordinate;
        public HexGridChunk Chunk;
        public RectTransform uiRect;

        public Color Color
        {
            get { return _color;  }

            set
            {
                if (_color == value) return;
                _color = value;
                Refresh();
            }
        }

        public int Evaluation
        {
            get { return _evaluation; }

            set
            {
                if (_evaluation == value) return;
                
                _evaluation = value;
                var pos = transform.localPosition;
                pos.y = value * HexMetrics.elevationStep;
                // pos.y += (HexMetrics.SampleNoise(pos).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
                transform.localPosition = pos;

                if (_hasOutgoingRiver && _evaluation < GetNeighbor(_outgoingRiver)._evaluation)
                {
                    RemoveOutgoingRiver();
                }

                if (_hasIncomingRiver && _evaluation > GetNeighbor(_incomingRiver)._evaluation)
                {
                    RemoveIncomingRiver();
                }

                for (var i = 0; i < roads.Length; i++)
                {
                    if (roads[i] && GetEdgeType(GetNeighbor((HexDirection) i)) == HexEdgeType.Cliff)
                    {
                        SetRoad(i, false);
                    }
                }
                
                Refresh();
            }
        }

        public int WaterLevel
        {
            get { return _waterLevel; }
            set
            {
                if (_waterLevel == value) return;
                _waterLevel = value;
                Refresh();
            }
        }

        public bool IsUnderWater => _waterLevel > _evaluation;

        public float StreamBedY => (_evaluation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;

        public float RiverSurfaceY => (_evaluation + HexMetrics.waterSurfaceElevationOffset) * HexMetrics.elevationStep;
        public float WaterSurfaceY => (_waterLevel + HexMetrics.waterSurfaceElevationOffset) * HexMetrics.elevationStep;
        
        [SerializeField] public HexCell[] neighbors;
        [SerializeField] public bool[] roads;
        public Vector3 Position => transform.localPosition;

        private int _waterLevel;
        private int _evaluation;
        private Color _color;
        private bool _hasIncomingRiver, _hasOutgoingRiver;
        private HexDirection _incomingRiver, _outgoingRiver;

        public HexCell GetNeighbor(HexDirection direction)
        {
            return neighbors[(int) direction];
        }

        public HexEdgeType GetEdgeType(HexCell cell)
        {
            return HexEdgeTypeExtension.GetEdgeType(this._evaluation, cell._evaluation);
        }

        public void SetNeighbor(HexDirection direction, HexCell cell)
        {
            neighbors[(int) direction] = cell;
            cell.neighbors[(int) direction.Opposite()] = this;
        }

        public void Refresh()
        {
            if (!Chunk) return;
            
            Chunk.Refresh();
            
            foreach (var neighbor in neighbors)
            {
                if (neighbor == null || neighbor.Chunk == null) continue;
                
                neighbor.Chunk.Refresh();
            }
        }

        public void RefreshSelf()
        {
            if (Chunk != null)
            {
                Chunk.Refresh();
            }
        }

        #region River 

        public bool HasInComingRiver => _hasIncomingRiver;
        public bool HasOutgoingRiver => _hasOutgoingRiver;
        public bool HasRiver => _hasIncomingRiver || _hasOutgoingRiver;
        public HexDirection IncomingRiver => _incomingRiver;
        public HexDirection OutgoingRiver => _outgoingRiver;
        public HexDirection RiverBeginOrEndDirection => _hasIncomingRiver ? _incomingRiver : _outgoingRiver;
        public bool HasRiverBeginOrEnd => _hasIncomingRiver != _hasOutgoingRiver;
        public bool HasRiverThroughEdge(HexDirection direction)
        {
            return _hasIncomingRiver && _incomingRiver == direction || _hasOutgoingRiver && _outgoingRiver == direction;
        }

        public void RemoveOutgoingRiver()
        {
            if (!_hasOutgoingRiver) return;

            _hasOutgoingRiver = false;
            RefreshSelf();

            var neighbor = GetNeighbor(_outgoingRiver);
            neighbor._hasIncomingRiver = false;
            neighbor.RefreshSelf();
        }

        public void RemoveIncomingRiver()
        {
            if (!_hasIncomingRiver) return;
            _hasIncomingRiver = false;
            RefreshSelf();

            var neighbor = GetNeighbor(_incomingRiver);
            neighbor._hasOutgoingRiver = false;
            neighbor.RefreshSelf();
        }

        public void RemoveRiver()
        {
            RemoveIncomingRiver();
            RemoveOutgoingRiver();
        }

        public void SetOutgoingRiver(HexDirection direction)
        {
            if (_hasOutgoingRiver && _outgoingRiver == direction) return;

            var neighbor = GetNeighbor(direction);
            if (neighbor == null || neighbor.Evaluation > Evaluation) return;
            
            RemoveOutgoingRiver();
            if (_hasIncomingRiver && _incomingRiver == direction)
            {
                RemoveIncomingRiver();
            }
            
            _hasOutgoingRiver = true;
            _outgoingRiver = direction;
            
            neighbor.RemoveIncomingRiver();
            neighbor._hasIncomingRiver = true;
            neighbor._incomingRiver = direction.Opposite();
            
            SetRoad((int) direction, false);
        }

        #endregion

        #region Road

        public bool HasRoadThroughEdge(HexDirection direction)
        {
            return roads[(int) direction];
        }

        public bool HasRoads => roads.Contains(true);

        public void RemoveRoads()
        {
            for (var i = 0; i < neighbors.Length; i++)
            {
                if (roads[i])
                {
                    SetRoad(i, false);
                }
            }
        }

        public void AddRoad(HexDirection direction)
        {
            if (!roads[(int) direction] && !HasRiverThroughEdge(direction) && GetEdgeType(GetNeighbor(direction)) != HexEdgeType.Cliff)
            {
                SetRoad((int) direction, true);
            }
        }

        private void SetRoad(int i, bool state)
        {
            roads[i] = state;
            var opposite = ((HexDirection) i).Opposite();
            if (neighbors[i] != null) neighbors[i].roads[(int) opposite] = state;
            neighbors[i].RefreshSelf();
            RefreshSelf();
        }
        
        #endregion
    }
}