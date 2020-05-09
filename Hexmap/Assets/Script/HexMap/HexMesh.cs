using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Dol
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HexMesh : MonoBehaviour
    {
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private Mesh _mesh;
        private MeshCollider _meshCollider;
        private List<int> _triangles;
        private List<Vector3> _vertices;
        private List<Color> _colors;
        private List<Vector2> _uvs;
        public bool UseCollider;
        public bool UseColor;
        public bool UseUV;
        private void Awake()
        {
            _mesh = GetComponent<MeshFilter>().mesh = new Mesh();
            if (UseCollider)
            {
                _meshCollider = gameObject.AddComponent<MeshCollider>();
            }
        }

        public void Clear()
        {
            _mesh.Clear();
            _triangles = ListPool<int>.Get();
            _vertices = ListPool<Vector3>.Get();
            if (UseColor)
            {
                _colors = ListPool<Color>.Get();
            }
            if (UseUV)
            {
                _uvs = ListPool<Vector2>.Get();
            }
        }

        public void Apply()
        {
            _mesh.SetVertices(_vertices);
            ListPool<Vector3>.Put(_vertices);
            if (UseColor)
            {
                _mesh.SetColors(_colors);
                ListPool<Color>.Put(_colors);
            }
            _mesh.SetTriangles(_triangles, 0);
            ListPool<int>.Put(_triangles);
            if (UseUV)
            {
                _mesh.SetUVs(0, _uvs);
                ListPool<Vector2>.Put(_uvs);
            }
            _mesh.RecalculateNormals();
            if (UseCollider)
            {
                _meshCollider.sharedMesh = _mesh;
            }
        }

        public void AddTriangleColor(Color color)
        {
            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);
        }
        public void AddTriangleColor(Color color1, Color color2, Color color3)
        {
            _colors.Add(color1);
            _colors.Add(color2);
            _colors.Add(color3);
        }
        public void AddQuadColor(Color color1)
        {
            _colors.Add(color1);
            _colors.Add(color1);
            _colors.Add(color1);
            _colors.Add(color1);
        }
        public void AddQuadColor(Color color1, Color color2)
        {
            _colors.Add(color1);
            _colors.Add(color1);
            _colors.Add(color2);
            _colors.Add(color2);
        }

        public void AddQuadColor(Color color1, Color color2, Color color3, Color color4)
        {
            _colors.Add(color1);
            _colors.Add(color2);
            _colors.Add(color3);
            _colors.Add(color4);
        }

        public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            var index = _vertices.Count;
            
            _vertices.Add(HexMetrics.Perturb(v1));
            _vertices.Add(HexMetrics.Perturb(v2));
            _vertices.Add(HexMetrics.Perturb(v3));
            _vertices.Add(HexMetrics.Perturb(v4));
            _triangles.Add(index);
            _triangles.Add(index + 2);
            _triangles.Add(index + 1);
            _triangles.Add(index + 1);
            _triangles.Add(index + 2);
            _triangles.Add(index + 3);
        }

        public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            _uvs.Add(uv1);
            _uvs.Add(uv2);
            _uvs.Add(uv3);
        }

        public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
        {
            _uvs.Add(uv1);
            _uvs.Add(uv2);
            _uvs.Add(uv3);
            _uvs.Add(uv4);
        }

        public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
        {
            _uvs.Add(new Vector2(uMin, vMin));
            _uvs.Add(new Vector2(uMax, vMin));
            _uvs.Add(new Vector2(uMin, vMax));
            _uvs.Add(new Vector2(uMax, vMax));
        }

        public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var index = _vertices.Count;
            _vertices.Add(HexMetrics.Perturb(v1));
            _vertices.Add(HexMetrics.Perturb(v2));
            _vertices.Add(HexMetrics.Perturb(v3));
            _triangles.Add(index);
            _triangles.Add(index + 1);
            _triangles.Add(index + 2);
        }
        
        public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var index = _vertices.Count;
            _vertices.Add(v1);
            _vertices.Add(v2);
            _vertices.Add(v3);
            _triangles.Add(index);
            _triangles.Add(index + 1);
            _triangles.Add(index + 2);
        }

    }
}