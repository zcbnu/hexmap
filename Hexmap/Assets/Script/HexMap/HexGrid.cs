using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

namespace Alpha.Dol
{
    public class HexGrid : MonoBehaviour
    {
        [SerializeField] public int DefaultColorIndex;
        [SerializeField] public HexCell HexCellPrefab;
        [SerializeField] public Text HexLabelPrefab;
        [SerializeField] public HexGridChunk HexGridChunkPrefab;
        [SerializeField] public Texture2D noiseSource;
        [SerializeField] public int CellCountX;
        [SerializeField] public int CellCountZ;

        protected int ChunkCountX, ChunkCountZ;
        private HexCell[] _cells;
        private HexGridChunk[] _chunks;
        private void Awake()
        {
            HexMetrics.noiseSource = noiseSource;

            CreateMap(CellCountX, CellCountZ);
        }

        private void OnEnable()
        {
            HexMetrics.noiseSource = noiseSource;
        }

        public bool CreateMap(int x, int z)
        {
            if (_chunks != null)
            {
                foreach (var gridChunk in _chunks)
                {
                    Destroy(gridChunk.gameObject);
                }
            }
            if (x <= 0 || x % HexMetrics.chunkSizeX != 0 || z <= 0 || z % HexMetrics.chunkSizeZ != 0)
            {
                Debug.LogError("Unsupported map");
                return false;
            }
            CellCountX = x;
            CellCountZ = z;
            ChunkCountX = x / HexMetrics.chunkSizeX;
            ChunkCountZ = z / HexMetrics.chunkSizeZ;
            CreateChunks();
            CreateCells();
            return true;
        }

        private void CreateChunks()
        {
            _chunks = new HexGridChunk[ChunkCountX * ChunkCountZ];
            for (int z = 0, i = 0; z < ChunkCountZ; z++)
            {
                for (var x = 0; x < ChunkCountX; x++)
                {
                    var chunk = Instantiate(HexGridChunkPrefab);
                    _chunks[i ++] = chunk;
                    chunk.transform.SetParent(this.transform, false);
                }
            }
        }

        private void CreateCells()
        {
            _cells = new HexCell[ChunkCountX * ChunkCountZ * HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
            for (int z = 0, i = 0; z < ChunkCountZ * HexMetrics.chunkSizeZ; z++)
            {
                for (int x = 0; x < ChunkCountX * HexMetrics.chunkSizeX; x++)
                {
                    CreateCell(x, z, i++);
                }
            }
        }

        private void CreateCell(int x, int z, int i)
        {
            Vector3 pos;
            pos.x = (x * 2 + z % 2) * HexMetrics.innerRadius;
            pos.y = 0;
            pos.z = z * 1.5f * HexMetrics.outerRadius;
            var cell = _cells[i] = Instantiate<HexCell>(HexCellPrefab);
            cell.transform.localPosition = pos;
            cell.HexCoordinate = HexCoordinate.CreateFromOffset(x, z);
            cell.TerrainTypeIndex = this.DefaultColorIndex;
            if (x > 0)
            {
                cell.SetNeighbor(HexDirection.W, _cells[i-1]);
            }

            var width = ChunkCountX * HexMetrics.chunkSizeX;
            if (z > 0)
            {
                if ((z & 1) == 0)
                {
                    cell.SetNeighbor(HexDirection.SE, _cells[i - width]);
                    if (x > 0)
                    {
                        cell.SetNeighbor(HexDirection.SW, _cells[i - width - 1]);
                    }
                }
                else
                {
                    cell.SetNeighbor(HexDirection.SW, _cells[i - width]);
                    if (x < width - 1)
                    {
                        cell.SetNeighbor(HexDirection.SE, _cells[i - width + 1]);
                    }
                }
            }

            var label = Instantiate(HexLabelPrefab);
            label.rectTransform.anchoredPosition = new Vector2(pos.x, pos.z);
            label.text = cell.HexCoordinate.ToSplitString();

            cell.uiRect = label.rectTransform;
            cell.Evaluation = 0;

            AddCellToChunk(x, z, cell);
        }

        private void AddCellToChunk(int x, int z, HexCell cell)
        {
            var countX = x / HexMetrics.chunkSizeX;
            var countZ = z / HexMetrics.chunkSizeZ;
            var index = countX + countZ * ChunkCountZ;
            var chunk = _chunks[index];
            var dx = x - countX * HexMetrics.chunkSizeX;
            var dz = z - countZ * HexMetrics.chunkSizeZ;
            
            chunk.AddCell(dx + dz * HexMetrics.chunkSizeX, cell);
        }

        public HexCell GetCell(Vector3 position)
        {
            position = transform.InverseTransformPoint(position);
            var coordinates = HexCoordinate.CreateFromPosition(position);
            var index = coordinates.X + coordinates.Z * ChunkCountX * HexMetrics.chunkSizeX + coordinates.Z / 2;
            return _cells[index];
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(CellCountX);
            writer.Write(CellCountZ);
            for (var i = 0; i < _cells.Length; i++)
            {
                _cells[i].Save(writer);
            }
        }

        public void Load(BinaryReader reader)
        {
            StopAllCoroutines();
            var x = reader.ReadInt32();
            var z = reader.ReadInt32();
            var success = CreateMap(x, z);

            if (!success)
            {
                Debug.LogError("Map create failed");
                return;
            }
            
            for (var i = 0; i < _cells.Length; i++)
            {
                _cells[i].Load(reader);
            }

            foreach (var gridChunk in _chunks)
            {
                gridChunk.Refresh();
            }
        }

        public IEnumerator Search(HexCell fromCell, HexCell toCell)
        {
            foreach (var cell in _cells)
            {
                cell.Distance = Int32.MaxValue;
                cell.DisableHighlight();
            }
            fromCell.EnableHighLight(Color.blue);
            toCell.EnableHighLight(Color.red);
            var delay = new WaitForSeconds( 1 / 60f);
            
            var queue = new HexCellPriorityQueue();
            fromCell.Distance = 0;
            queue.Enqueue(fromCell);
            while (queue.Count > 0)
            {
                yield return delay;
                var cell = queue.Dequeue();
                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var cellNeighbor = cell.GetNeighbor(d);
                    if (cellNeighbor == null) continue;
                    var edgeType = cell.GetEdgeType(cellNeighbor);
                    if (cellNeighbor.IsUnderWater) continue;
                    if (edgeType == HexEdgeType.Cliff) continue;

                    var distance = cell.Distance;
                    if (cell.HasRoadThroughEdge(d))
                    {
                        distance = cell.Distance + 1;
                    }
                    else
                    {
                        distance = cell.Distance + edgeType == HexEdgeType.Flat ? 5 : 10;
                    }

                    if (cellNeighbor.Distance == Int32.MaxValue)
                    {
                        cellNeighbor.Distance = distance;
                        cellNeighbor.FromCell = cell;
                        cellNeighbor.SearchHeuristic = cellNeighbor.HexCoordinate.DistanceTo(toCell.HexCoordinate);
                        queue.Enqueue(cellNeighbor);
                    }
                    else if (distance < cellNeighbor.Distance)
                    {
                        var oldPriority = cellNeighbor.SearchPriority;
                        cellNeighbor.Distance = distance;
                        cellNeighbor.FromCell = cell;
                        queue.ChangePriority(cellNeighbor, oldPriority);
                    }

                    if (cell == toCell)
                    {
                        cell = cell.FromCell;
                        while (cell != fromCell)
                        {
                            cell.EnableHighLight(Color.white);
                            cell = cell.FromCell;
                        }

                        yield break;
                    }
                }
            }
        }

        public void ShowUI(bool enable)
        {
            foreach (var chunk in _chunks)
            {
                chunk.ShowUI(enable);
            }
        }

        public void FindPath(HexCell hexCell, HexCell toCell)
        {
            StopAllCoroutines();
            StartCoroutine(Search(hexCell, toCell));
        }
    }
}