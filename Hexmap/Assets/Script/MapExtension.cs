using System.IO;
using Packages.Rider.Editor.Util;
using UnityEditor;
using UnityEngine;

namespace Script
{
    public static class MapExtension
    {
        [MenuItem("Map/General/Open map file path")]
        public static void OpenMapFilePath()
        {
            var path = Application.temporaryCachePath;
            EditorUtility.RevealInFinder(path);
        }
    }
}