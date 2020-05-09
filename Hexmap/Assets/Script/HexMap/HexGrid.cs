using UnityEngine;
using UnityEngine.UI;

namespace Alpha.Dol
{
    public class HexGrid : MonoBehaviour
    {
        [SerializeField] public Color DefaultColor;
        [SerializeField] public HexCell HexCellPrefab;
        [SerializeField] public Text HexLabelPrefab;
        [SerializeField] public HexGridChunk HexGridChunkPrefab;
        [SerializeField] public Texture2D noiseSource;
        [SerializeField] public int ChunkCountX;
        [SerializeField] public int ChunkCountZ;

        private HexCell[] _cells;
        private HexGridChunk[] _chunks;
        private void Awake()
        {
            HexMetrics.noiseSource = noiseSource;

            CreateChunks();
            _cells = new HexCell[ChunkCountX * ChunkCountZ * HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
            for (int z = 0, i = 0; z < ChunkCountZ * HexMetrics.chunkSizeZ; z++)
            {
                for (int x = 0; x < ChunkCountX * HexMetrics.chunkSizeX; x++)
                {
                    CreateCell(x, z, i++);
                }
            }
        }

        private void OnEnable()
        {
            HexMetrics.noiseSource = noiseSource;
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

        private void CreateCell(int x, int z, int i)
        {
            Vector3 pos;
            pos.x = (x * 2 + z % 2) * HexMetrics.innerRadius;
            pos.y = 0;
            pos.z = z * 1.5f * HexMetrics.outerRadius;
            var cell = _cells[i] = Instantiate<HexCell>(HexCellPrefab);
            cell.transform.localPosition = pos;
            cell.HexCoordinate = HexCoordinate.CreateFromOffset(x, z);
            cell.Color = DefaultColor;
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
    }
}