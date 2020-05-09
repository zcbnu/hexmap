using UnityEditor;
using UnityEngine;

namespace Alpha.Dol
{
    [CustomPropertyDrawer(typeof(HexCoordinate))]
    public class HexCoordinatesDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var coordinates = new HexCoordinate(
                property.FindPropertyRelative("x").intValue,
                property.FindPropertyRelative("z").intValue
                );
            GUI.Label(position, coordinates.ToString());
        }
    }
}