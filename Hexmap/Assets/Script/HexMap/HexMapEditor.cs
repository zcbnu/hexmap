using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Alpha.Dol
{
    public class HexMapEditor : MonoBehaviour
    {
        [SerializeField] public HexGrid HexGrid;
        [SerializeField] public Material TerrainMaterial;
        private int _activeColorIndex;
        private int _activeElevation;
        private int _activeWaterLevel;
        private HexCell _previousCell;
        public HexCell _searchFromCell;
        private HexDirection _dragDirection;
        private bool _isDrag;
        private bool _colorMode;
        private bool _evaluationMode;
        private bool _waterMode;
        private bool _editorMode;
        private OptionalToggle _riverMode = OptionalToggle.Invalid;
        private OptionalToggle _roadMode = OptionalToggle.Invalid;

        public enum OptionalToggle
        {
            Invalid,
            No,
            Yes,
        }
        private void Awake()
        {
            SelectColor(0);
            TerrainMaterial.DisableKeyword("GRID_ON");
            _editorMode = true;
        }

        private void Update()
        {
            if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                HandleInput();
            }
            else
            {
                _previousCell = null;
            }
        }

        private HexCell GetCellUnderCursor()
        {
            var inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(inputRay, out RaycastHit hitInfo))
            {
                return HexGrid.GetCell(hitInfo.point);
            }
            else
            {
                return null;
            }
        }
        
        private void HandleInput()
        {
            var cell = GetCellUnderCursor();
            if (!cell)
            {
                if (!_previousCell && _previousCell != cell)
                {
                    ValidateDrag(cell);
                }
                else
                {
                    _isDrag = false;
                }

                if (_editorMode)
                {
                    EditCell(cell);
                }
                else if (Input.GetKey(KeyCode.LeftShift))
                {
                    if (!_searchFromCell)
                    {
                        _searchFromCell.DisableHighlight();
                    }

                    _searchFromCell = cell;
                    _searchFromCell.EnableHighLight(Color.blue);
                }
                else if (!_searchFromCell && _searchFromCell != cell)
                {
                    HexGrid.FindPath(_searchFromCell, cell);
                }

                _previousCell = cell;
            }
            else
            {
                _previousCell = null;
            }
        }

        private void EditCell(HexCell cell)
        {
            if (_colorMode)
            {
                cell.TerrainTypeIndex = _activeColorIndex;
            }

            if (_evaluationMode)
            {
                cell.Evaluation = _activeElevation;
            }

            if (_waterMode)
            {
                cell.WaterLevel = _activeWaterLevel;
            }
            
            if (_riverMode == OptionalToggle.No)
            {
                cell.RemoveRiver();
            }

            if (_roadMode == OptionalToggle.No)
            {
                cell.RemoveRoads();
            }
            else if (_isDrag)
            {
                var otherCell = cell.GetNeighbor(_dragDirection.Opposite());
                if (otherCell != null)
                {
                    if (_riverMode == OptionalToggle.Yes)
                    {
                        otherCell.SetOutgoingRiver(_dragDirection);
                    }
                    if (_roadMode == OptionalToggle.Yes)
                    {
                        otherCell.AddRoad(_dragDirection);
                    }
                }
            }
        }

        private void ValidateDrag(HexCell cell)
        {
            for (_dragDirection = HexDirection.NE; _dragDirection <= HexDirection.NW; _dragDirection++)
            {
                if (_previousCell.GetNeighbor(_dragDirection) == cell)
                {
                    _isDrag = true;
                    return;
                }
            }

            _isDrag = false;
        }

        public void SetElevation(float elevation)
        {
            _activeElevation = (int)elevation;
        }

        public void SelectColor(int i)
        {
            _activeColorIndex = i;
        }

        public void SetRiverMode(int i)
        {
            _riverMode = (OptionalToggle) i;
        }

        public void SetRoadMode(int i)
        {
            _roadMode = (OptionalToggle) i;
        }

        public void SetColorMode(bool i)
        {
            _colorMode = i;
        }

        public void SetEvaluationMode(bool arg0)
        {
            _evaluationMode = arg0;
        }

        public void SetWaterMode(bool enable)
        {
            _waterMode = enable;
        }

        public void SetWaterLevel(float level)
        {
            _activeWaterLevel = (int)level;
        }

        public void Save()
        {
            var path = Path.Combine(Application.temporaryCachePath, "test.map");
            using (var writer = new BinaryWriter(File.Open(path, FileMode.Create)))
            {
                writer.Write(0);
                HexGrid.Save(writer);
                Debug.Log("Save map success");
                writer.Dispose();
            }
        }

        public void Load()
        {
            var path = Path.Combine(Application.temporaryCachePath, "test.map");
            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                var header = reader.ReadInt32();

                if (header == 0)
                {
                    HexGrid.Load(reader);
                    HexCamera.ValidatePosition();
                }
                else
                {
                    Debug.LogError($"Unknown map format {header}");
                }
                reader.Dispose();
                Debug.Log("Load map success");
            }
        }

        public void ShowGrid(bool show)
        {
            if (show)
            {
                TerrainMaterial.EnableKeyword("GRID_ON");
            }
            else
            {
                TerrainMaterial.DisableKeyword("GRID_ON");
            }
        }

        public void SetEditMode(bool active)
        {
            _editorMode = active;
            HexGrid.ShowUI(!active);
        }
    }
}