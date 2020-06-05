using UnityEngine;

namespace Alpha.Dol
{
    public class HexCellShaderData : MonoBehaviour
    {
        private Texture2D _cellTexture;
        private Color32[] _cellTextureData;

        public void Initialize(int x, int z)
        {
            if (_cellTexture)
            {
                _cellTexture.Resize(x, z);
            }
            else
            {
                _cellTexture = new Texture2D(x, z, TextureFormat.RGBA32, false, true);
                _cellTexture.filterMode = FilterMode.Point;
                _cellTexture.wrapMode = TextureWrapMode.Clamp;
            }

            if (_cellTextureData == null || _cellTextureData.Length != x * z)
            {
                _cellTextureData = new Color32[x * z];
            }
            else
            {
                for (var i = 0; i < _cellTextureData.Length; i++)
                {
                    _cellTextureData[i] = new Color32(0,0,0,0);
                }
            }
        }
    }
}