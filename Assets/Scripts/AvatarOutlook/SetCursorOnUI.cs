using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SetCursorOnUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        //Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        //var pic = GameObject.Find("PaintManager").GetComponent<PaintManager>().cursor_brush;
        //Cursor.SetCursor(pic, new Vector2(pic.width / 2, pic.height / 2), CursorMode.Auto);
    }
}