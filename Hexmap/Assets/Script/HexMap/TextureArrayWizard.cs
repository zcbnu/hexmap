using System;
using UnityEditor;
using UnityEngine;

namespace Alpha.Dol
{
    public class TextureArrayWizard : ScriptableWizard
    {
        [MenuItem("Asset/Create/Texture Array")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<TextureArrayWizard>("Create Texture Array", "Create");
        }

        public Texture2D[] Texture2Ds;

        private void OnWizardCreate()
        {
            if (Texture2Ds.Length == 0) return;

            var path = EditorUtility.SaveFilePanelInProject("Save Texture Array", "Texture Array", "asset", "Save Texture Array");
            if (path.Length == 0)
            {
                Debug.LogWarning("Path no found");
                return;
            }

            var t = Texture2Ds[0];
            Texture2DArray texture2DArray = new Texture2DArray(t.width, t.height, Texture2Ds.Length, t.format, t.mipmapCount > 1);
            texture2DArray.anisoLevel = t.anisoLevel;
            texture2DArray.filterMode = t.filterMode;
            texture2DArray.wrapMode = t.wrapMode;

            for (var i = 0; i < Texture2Ds.Length; i++)
            {
                for (var m = 0; m < t.mipmapCount; m++)
                {
                    Graphics.CopyTexture(Texture2Ds[i], 0, m, texture2DArray, i, m);
                }
            }
            
            AssetDatabase.CreateAsset(texture2DArray, path);
        }
    }
}