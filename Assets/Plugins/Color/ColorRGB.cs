using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ColorRGB : MonoBehaviour {
    Texture2D tex2d;
    public RawImage ri;
    int TexPixelWdith = 16;
    int TexPixelHeight = 256;
    Color[,] arrayColor;
    // Use this for initialization
    void Start () {
        arrayColor = new Color[TexPixelWdith, TexPixelHeight];
        tex2d = new Texture2D(TexPixelWdith, TexPixelHeight, TextureFormat.RGB24,true);

        Color[] calcArray = CalcArrayColor();
        tex2d.SetPixels(calcArray);
        tex2d.Apply();

        ri.texture = tex2d;
    }
    
    //右边的颜色作为endColor的值，右边的值呢，是RYGCBM六档色，其他颜色也是他们的混合出来的，然后选择一个endColor左边的颜色面板就会更新新的颜色
    Color[] CalcArrayColor()
    {
        //品质优化：增加了CMY色变化，addValue细分为6档（原为RGB三原色）
        int addValue = (TexPixelHeight - 1) / 6;//3
        for (int i = 0; i < TexPixelWdith; i++)
        {
            arrayColor[i, 0] = Color.red;
            arrayColor[i, addValue] = Color.yellow;
            arrayColor[i, addValue * 2] = Color.green;
            arrayColor[i, addValue * 3] = Color.cyan;
            arrayColor[i, addValue * 4] = Color.blue;
            arrayColor[i, addValue * 5] = Color.magenta;
            arrayColor[i, TexPixelHeight - 1] = Color.red;
        }
        for (int i = 0; i < TexPixelWdith; i++)
        {
            Color value = (Color.yellow - Color.red) / addValue;//计算离散化的增量，颜色是线性的？
            for (int j = 0; j < addValue; j++)
            {
                arrayColor[i, j] = Color.red + value * j;
            }
            value = (Color.green - Color.yellow) / addValue;
            for (int j = addValue; j < addValue * 2; j++)
            {
                arrayColor[i, j] = Color.yellow + value * (j - addValue);
            }
            value = (Color.cyan - Color.green) / addValue;
            for (int j = addValue * 2; j < addValue * 3; j++)
            {
                arrayColor[i, j] = Color.green + value * (j - addValue * 2);
            }
            value = (Color.blue - Color.cyan) / addValue;
            for (int j = addValue * 3; j < addValue * 4; j++)
            {
                arrayColor[i, j] = Color.cyan + value * (j - addValue * 3);
            }
            value = (Color.magenta - Color.blue) / addValue;
            for (int j = addValue * 4; j < addValue * 5; j++)
            {
                arrayColor[i, j] = Color.blue + value * (j - addValue * 4);
            }
            value = (Color.red - Color.magenta) / addValue;
            for (int j = addValue * 5; j < addValue * 6 + 2; j++)//TexPixelWdith
            {
                arrayColor[i, j] = Color.magenta + value * (j - addValue * 5);
            }
        }

        List<Color> listColor = new List<Color>();
        for (int i = 0; i < TexPixelHeight; i++)
        {
            for (int j = 0; j < TexPixelWdith; j++)
            {
                listColor.Add(arrayColor[j, i]);
            }
        }

        return listColor.ToArray();
    }

    /// <summary>
    /// 获取颜色 根据高度
    /// 在第一列取行像素值，返回color A总为1
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public Color GetColorBySliderValue(float value)
    {
        Color getColor=tex2d.GetPixel(0,(int)((TexPixelHeight-1)*(1.0f-value)));
        return getColor;
    }
}



