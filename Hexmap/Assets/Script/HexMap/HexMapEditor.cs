using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Alpha.Dol
{
    public class HexMapEditor : MonoBehaviour
    {
        [SerializeField] public Color[] Colors;
        [SerializeField] public HexGrid HexGrid;
        [SerializeField] public Toggle toggleColor;
        [SerializeField] public Toggle toggleEvaluation;
        private Color _activeColor;
        private int _activeElevation;
        private HexCell _previousCell;
        private HexDirection _dragDirection;
        private bool _isDrag;
        private bool _colorMode;
        private bool _evaluationMode;
        private OptionalToggle _riverMode = OptionalToggle.Invalid;

        public enum OptionalToggle
        {
            Invalid,
            No,
            Yes,
        }
        private void Awake()
        {
            SelectColor(0);
            toggleColor.onValueChanged.AddListener(SetColorMode);
            toggleEvaluation.onValueChanged.AddListener(SetEvaluationMode);
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

        private void HandleInput()
        {
            var inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(inputRay, out RaycastHit hitInfo))
            {
                var cell = HexGrid.GetCell(hitInfo.point);
                if (_previousCell != null && _previousCell != cell)
                {
                    ValidateDrag(cell);
                }
                else
                {
                    _isDrag = false;
                }
                EditCell(cell);

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
                cell.Color = _activeColor;
            }

            if (_evaluationMode)
            {
                cell.Evaluation = _activeElevation;
            }

            if (_riverMode == OptionalToggle.No)
            {
                cell.RemoveRiver();
            }
            else if (_isDrag && _riverMode == OptionalToggle.Yes)
            {
                var otherCell = cell.GetNeighbor(_dragDirection.Opposite());
                if (otherCell != null)
                {
                    otherCell.SetOutgoingRiver(_dragDirection);
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
            var slider = GetComponentInChildren<Slider>();
            _activeElevation = (int) slider.value;
        }

        public void SelectColor(int i)
        {
            _activeColor = Colors[i];
        }

        public void SetRiverMode(int i)
        {
            _riverMode = (OptionalToggle) i;
        }

        private void SetColorMode(bool i)
        {
            _colorMode = i;
        }

        private void SetEvaluationMode(bool arg0)
        {
            _evaluationMode = arg0;
        }
    }
}