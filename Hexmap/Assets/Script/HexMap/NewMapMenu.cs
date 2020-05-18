using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Alpha.Dol
{
    public class NewMapMenu : MonoBehaviour
    {
        public HexGrid HexGrid;
        public void OnOpen()
        {
            gameObject.SetActive(true);
            HexCamera.Locked = true;
        }

        public void OnClose()
        {
            gameObject.SetActive(false);
            HexCamera.Locked = false;
        }

        private void CreateMap(int x, int z)
        {
            HexGrid.CreateMap(x, z);
            HexCamera.ValidatePosition();
            OnClose();
        }

        public void CreateBigMap()
        {
            var textField = GetComponentInChildren<InputField>();
            if (textField != null && !string.IsNullOrEmpty(textField.text))
            {
                var factors = textField.text.Split(',').ToList().ConvertAll(Convert.ToInt32);
                CreateMap(factors[0], factors[1]);
            }
            else
            {
                CreateMap(50, 50);
            }
        }
    }
}