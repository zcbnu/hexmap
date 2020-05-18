using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Dol
{
    public class HexCamera : MonoBehaviour
    {
        [SerializeField] protected HexGrid _hexGrid;
        [SerializeField] public float StickMinZoom;
        [SerializeField] public float StickMaxZoom;
        [SerializeField] public float SwivelMinZoom;
        [SerializeField] public float SwivelMaxZoom;
        [SerializeField] public float MoveSpeedMinZoom;
        [SerializeField] public float MoveSpeedMaxZoom;
        [SerializeField] public float RotateSpeed;
        private static HexCamera _instance;
        private Vector3 _position;
        private Quaternion _quaternion;
        private Stack<Tuple<Vector3, Quaternion>> _stack = new Stack<Tuple<Vector3, Quaternion>>();

        private Transform _swivel, _stick;
        private float _zoom = 1f;
        private float _rotateAngle;
        private void Awake()
        {
            _instance = this;
            _swivel = transform.GetChild(0);
            _stick = _swivel.GetChild(0);
        }

        public static void ValidatePosition()
        {
            _instance.AdjustPosition(0,0);
        }

        public static bool Locked
        {
            set { _instance.enabled = !value; }
        }

        private void Update()
        {
            var zoomDelta = Input.GetAxis("Mouse ScrollWheel");
            if (Math.Abs(zoomDelta) > Mathf.Epsilon)
            {
                AdjustZoom(zoomDelta);
            }
            
            var rotate = Input.GetAxis("Rotation");
            if (Math.Abs(rotate) > Mathf.Epsilon)
            {
                AdjustRotation(rotate);
            }

            var x = Input.GetAxis("Horizontal");
            var z = Input.GetAxis("Vertical");
            if (Math.Abs(x) > Mathf.Epsilon || Math.Abs(z) > Mathf.Epsilon)
            {
                AdjustPosition(x, z);
            }
        }

        private void AdjustPosition(float xDelta, float zDelta)
        {
            var direction = transform.localRotation * new Vector3(xDelta, 0, zDelta).normalized;
            var damping = Math.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
            var distance = Mathf.Lerp(MoveSpeedMinZoom, MoveSpeedMaxZoom, _zoom) * damping * Time.deltaTime;
            var pos = transform.localPosition + direction * distance;
            transform.localPosition = ClampPosition(pos);
        }

        private Vector3 ClampPosition(Vector3 pos)
        {
            var xMax = (_hexGrid.CellCountX - 0.5f) * 2f * HexMetrics.innerRadius;
            var zMax = (_hexGrid.CellCountZ - 0.5f) * 1.5f * HexMetrics.outerRadius;
            pos.x = Mathf.Clamp(pos.x, 0, xMax);
            pos.z = Mathf.Clamp(pos.z, 0, zMax);
            return pos;
        }

        private void AdjustRotation(float rotate)
        {
            _rotateAngle += rotate * Time.deltaTime * RotateSpeed;
            if (_rotateAngle < 0f)
            {
                _rotateAngle += 360f;
            }
            else if (_rotateAngle > 360f)
            {
                _rotateAngle -= 360f;
            }
            
            transform.localRotation = Quaternion.Euler(0, _rotateAngle, 0);
        }

        private void AdjustZoom(float zoomDelta)
        {
            _zoom = Mathf.Clamp01(_zoom + zoomDelta);

            var distance = Mathf.Lerp(StickMinZoom, StickMaxZoom, _zoom);
            _stick.localPosition = new Vector3(0, 0, distance);

            var angle = Mathf.Lerp(SwivelMinZoom, SwivelMaxZoom, _zoom);
            _swivel.localRotation = Quaternion.Euler(angle, 0, 0);
        }
    }
}