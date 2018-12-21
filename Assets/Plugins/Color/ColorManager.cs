//////http://lib.csdn.net/article/unity3d/38680 https://github.com/coding2233/UnityColor 原作者：qq992817263 github:coding2233

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

//HSV调色盘
public class ColorManager : MonoBehaviour//, IDragHandler
{
    //RectTransform rt;

    private ColorRGB CRGB;
    public ColorPanel CP;
    public ColorCircle CC;

    public Slider sliderCRGB;
    public Image colorShow;

    //新加的对外访问接口
    public static Color currentColor = Color.black;
    
    void OnDisable()
    {
        CC.GetPos -= CC_getPos;
    }

    private void CC_getPos(Vector2 pos)
    {
        Color getColor= CP.GetColorByPosition(pos);
        colorShow.color = getColor;

        //新加，即时刷新所拾取色
        currentColor = getColor;
    }
    
    // Use this for initialization
    void Start () {
        //rt = GetComponent<RectTransform>();

        CRGB = GetComponentInChildren<ColorRGB>();
        CP = GetComponentInChildren<ColorPanel>();
        CC = GetComponentInChildren<ColorCircle>();

        sliderCRGB.onValueChanged.AddListener(OnCRGBValueChanged);

        CC.GetPos += CC_getPos;
    }

    /*
    public void OnDrag(PointerEventData eventData)
    {
        //Vector3 wordPos;
        ////将UGUI的坐标转为世界坐标  
        //if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out wordPos))
        //    rt.position = wordPos;
    }
    */

    void OnCRGBValueChanged(float value)
    {
        Color endColor=CRGB.GetColorBySliderValue(value);
        CP.SetColorPanel(endColor);
        CC.SetShowColor();
    }
}
