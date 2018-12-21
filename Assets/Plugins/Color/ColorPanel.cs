using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class ColorPanel : MonoBehaviour, IPointerClickHandler, IDragHandler
{
    Texture2D tex2d;
    public RawImage ri;

    int TexPixelLength = 256;
    Color[,] arrayColor;

    RectTransform rt;
    public RectTransform circleRect;
    // Use this for initialization
    void Start()
    {
        arrayColor = new Color[TexPixelLength, TexPixelLength];
        tex2d = new Texture2D(TexPixelLength, TexPixelLength, TextureFormat.RGB24, true);
        ri.texture = tex2d;

        rt = GetComponent<RectTransform>();

        SetColorPanel(Color.red);
    }

    /*
    // Update is called once per frame
    void Update()
    { 
        //随机end color颜色
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Color end = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1.0f);
            SetColorPanel(end);
        }
    }
     */

    //获取到颜色数组后，就可以将他绘制tex2d上，然后显示在RawImage上
    public void SetColorPanel(Color endColor)
    {
        Color[] CalcArray = CalcArrayColor(endColor);
        tex2d.SetPixels(CalcArray);
        tex2d.Apply();
    }


    //底部为黑色，左上为白色，右上为自定义颜色，我这里称他为endColor。知道四个角落的颜色后就开始计算中间像素的颜色
    //Texture2D建议用数组绘制像素，我就没有一个个去绘制，先把颜色装数组里吧
    Color[] CalcArrayColor(Color endColor)
    {
        Color value = (endColor - Color.white) / (TexPixelLength - 1);
        for (int i = 0; i < TexPixelLength; i++)
        {
            arrayColor[i, TexPixelLength - 1] = Color.white + value * i;
        }
        for (int i = 0; i < TexPixelLength; i++)
        {
            value = (arrayColor[i, TexPixelLength - 1] - Color.black) / (TexPixelLength - 1);
            for (int j = 0; j < TexPixelLength; j++)
            {
                arrayColor[i, j] = Color.black + value * j;
            }
        }
        List<Color> listColor = new List<Color>();
        for (int i = 0; i < TexPixelLength; i++)
        {
            for (int j = 0; j < TexPixelLength; j++)
            {
                listColor.Add(arrayColor[j, i]);
            }
        }

        return listColor.ToArray();
    }


    /// <summary>
    /// 获取颜色by坐标
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public Color GetColorByPosition(Vector2 pos)
    {
        Texture2D tempTex2d = (Texture2D)ri.texture;
        Color getColor = tempTex2d.GetPixel((int)pos.x, (int)pos.y);

        return getColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Vector3 wordPos;
        //将UGUI的坐标转为世界坐标  
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out wordPos))
            circleRect.position = wordPos;

        circleRect.GetComponent<ColorCircle>().SetShowColor();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 wordPos;
        //将UGUI的坐标转为世界坐标  
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out wordPos))
            circleRect.position = wordPos;

        circleRect.GetComponent<ColorCircle>().SetShowColor();
    }
}
