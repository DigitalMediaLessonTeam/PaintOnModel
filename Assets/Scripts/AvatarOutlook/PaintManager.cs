using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
//防UI穿透，新加
using UnityEngine.EventSystems;
using UnityEngine.UI;

class DrawLineCommand {//command模式
    const int albedoWidth = 512;
    const int albedoHeight = 512;
    public Texture2D before;
    public Texture2D after;
    public DrawLineCommand(Texture2D beforeCanvas,Texture2D afterCanvas)
    {
        before = new Texture2D(albedoWidth, albedoHeight); after = new Texture2D(albedoWidth, albedoHeight);
        before.SetPixels32(beforeCanvas.GetPixels32());before.Apply();
        after.SetPixels32(afterCanvas.GetPixels32());after.Apply();
    }
    public void Execute(ref Texture2D canvas)
    {
        canvas.SetPixels32(after.GetPixels32());canvas.Apply();
    }
    public void Undo(ref Texture2D canvas)
    {
        canvas.SetPixels32(before.GetPixels32());canvas.Apply();
    }
}


// This script allows you to paint the scene using raycasts
// NOTE: This requires the paint targets have the P3D_Paintable component
//单例脚本
public class PaintManager : MonoBehaviour {

    //单例
    private PaintManager() { }
    private static PaintManager instance = null;
    public static PaintManager Instance
    {
        get
        {
            return instance;
        }
    }
    private void Awake()
    {
        if (instance != null) { return; }
        else
        {
            instance = this;
        }
    }

    private Texture2D originTexture;//cache 进入paint界面前的原纹理
    private string albedoPath;//原纹理路径
    private int albedoWidth, albedoHeight;//原纹理大小
    public ColorManager CM;//用来方便的访问CM对象
    private Camera mainCamera;
    private P3D_Paintable paintable;
    private Texture2D canvas;
    private P3D_Painter painter;
    private Vector2 oldMousePosition;
    //public Texture2D cursor_brush;//画笔的预览，以鼠标样式的形式
    private Text currentBrushMode;
    private Slider brushSize;
    private Slider brushSolid;

    public enum NearestOrAll{ Nearest, All }

    [Tooltip("The GameObject layers you want to be able to paint")]
    public LayerMask LayerMask = -1;

    [Tooltip("The paintable texture groups you want to be able to paint")]
    public P3D_GroupMask GroupMask = -1;

    [Tooltip("The maximum amount of pixels between ")]
    private float StepSize = 1.0f;

    [Tooltip("Which surfaces it should hit")]
    public NearestOrAll Paint;

    [Tooltip("The settings for the brush we will paint with")]
    public P3D_Brush Brush;

    [Tooltip("The key that must be held down to mouse look")]
    private readonly KeyCode RequiresForDraw = KeyCode.Mouse0;

    //调整笔刷size用
    private readonly KeyCode RequiresForSize1 = KeyCode.LeftControl, RequiresForSize2 = KeyCode.RightControl;

    //调整笔刷透明度用
    private readonly KeyCode RequiresForOpacity1 = KeyCode.LeftAlt, RequiresForOpacity2 = KeyCode.RightAlt;

    //开启/关闭橡皮擦模式用
    private readonly KeyCode RequiresForErase = KeyCode.E;

    //吸色用
    //private KeyCode RequiresForPipette1 = KeyCode.LeftAlt, RequiresForPipette2 = KeyCode.RightAlt;
    private readonly KeyCode RequiresForPipette = KeyCode.Mouse2;

    //画直线用
    private readonly KeyCode RequiresForDrawStraightLine1 = KeyCode.LeftShift, RequiresForDrawStraightLine2 = KeyCode.RightShift;

    //撤销重做用
    private readonly KeyCode RequiresForUndoRedo = KeyCode.Z;

    //撤销重做用
    private int maxUndoLevels = 10; //可撤销的步数，-1时无限步
    List<DrawLineCommand> undoList;//列表实现，可以限制步数
    List<DrawLineCommand> redoList;
    
    private uint lastCanvasUpateCount = 1;
    private Texture2D lastCanvas;

    // Use this for initialization
    void Start()
    {
        if (mainCamera == null) { mainCamera = Camera.main; }
        albedoPath = PresetsManager.rootPath + "/" + PresetsManager.currentPresetIndex + "/albedo.png";
        albedoWidth = albedoHeight = 512;

        //备份一个原纹理
        //创建文件读取流
        FileStream fileStream = new FileStream(albedoPath, FileMode.Open, FileAccess.Read);
        fileStream.Seek(0, SeekOrigin.Begin);
        //创建文件长度缓冲区
        byte[] bytes = new byte[fileStream.Length];
        //读取文件
        fileStream.Read(bytes, 0, (int)fileStream.Length);
        //释放文件读取流
        fileStream.Close();
        fileStream.Dispose();
        fileStream = null;
        //创建Texture
        originTexture = new Texture2D(albedoWidth, albedoHeight);
        originTexture.LoadImage(bytes);

        //确保橡皮擦正常工作（原纹理作为detail图，所以要保持原比例）
        Brush.DetailScale = new Vector2(1.0f, 1.0f);

        paintable = GameObject.FindGameObjectWithTag("Model").GetComponentInChildren<P3D_Paintable>();
        painter = paintable.Textures[0].Painter;
        canvas = painter.Canvas;
        currentBrushMode = GameObject.Find("Text_CurrentBrushMode").GetComponent<Text>();
        brushSize = GameObject.Find("Slider_BrushSize").GetComponent<Slider>();
        brushSolid = GameObject.Find("Slider_BrushSolid").GetComponent<Slider>();
        brushSize.onValueChanged.AddListener(delegate { Brush.Size.x = Brush.Size.y = brushSize.value; });
        brushSolid.onValueChanged.AddListener(delegate { Brush.Opacity = brushSolid.value; StepSize = (1 / Brush.Opacity); });//越透明步长越长，防止笔触快速堆叠导致透明消失

        undoList = new List<DrawLineCommand>();
        redoList = new List<DrawLineCommand>();
        maxUndoLevels = 10;
        lastCanvas = new Texture2D(albedoWidth, albedoHeight);
    }
    
    // Update is called once per frame
    protected virtual void Update ()
    {
        //if (mainCamera == null) mainCamera = Camera.main;
        //if (mainCamera != null && StepSize > 0.0f) { }
        // The required key is down?
        //短击绘画键时
        if (Input.GetKeyDown(RequiresForDraw))
        {
            //画直线功能，机缘巧合发现的
            if (!(Input.GetKey(RequiresForDrawStraightLine1) || Input.GetKey(RequiresForDrawStraightLine2))) { oldMousePosition = Input.mousePosition; }

            //用来标记笔画的开始点，为了实现一笔画一存储当前纹理，以实现undo/redo。一笔画可以从任何屏幕位置开始。
            lastCanvas.SetPixels32(canvas.GetPixels32()); lastCanvas.Apply();
        }
        
        //检测一笔画完，存个图，用于undo,redo
        if (Input.GetKeyUp(RequiresForDraw))
        {
            if (lastCanvasUpateCount != canvas.updateCount)//检测画布内容是否发生改变，改变才存
            {
                redoList.Clear();//清空redoList，因为这些操作不能恢复了
                undoList.Add(new DrawLineCommand(lastCanvas, canvas));
                //保留最近maxUndoLevels次操作，删除最早操作  
                if (maxUndoLevels != -1 && undoList.Count > maxUndoLevels)
                {
                    undoList.RemoveAt(0);
                }

                lastCanvasUpateCount = canvas.updateCount;
            }
        }

        // The required key is set?
        //绘画键正被按下时，要画了。一般只是轻触也会触发几帧的此函数
        if (Input.GetKey(RequiresForDraw))
        {
            //更改笔刷颜色，从调色板
            Brush.Color = ColorManager.currentColor;

            // Find the ray for this screen position
            var newMousePosition = (Vector2)Input.mousePosition;
            var stepCount = Vector2.Distance(oldMousePosition, newMousePosition) / StepSize + 1;

            //细分，数次执行Paint
            for (var i = 0; i < stepCount; i++)
            {
                var subMousePosition = Vector2.Lerp(oldMousePosition, newMousePosition, i / stepCount);
                var ray = mainCamera.ScreenPointToRay(subMousePosition);
                var start = ray.GetPoint(mainCamera.nearClipPlane);
                var end = ray.GetPoint(mainCamera.farClipPlane);

                // This will both use Physics.Raycast and search P3D_Paintables
                switch (Paint)
                {
                    case NearestOrAll.Nearest:
                        {
                            P3D_Paintable.ScenePaintBetweenNearest(Brush, start, end, LayerMask, GroupMask);
                        }
                        break;

                    case NearestOrAll.All:
                        {
                            P3D_Paintable.ScenePaintBetweenAll(Brush, start, end, LayerMask, GroupMask);
                        }
                        break;
                }
            }

            oldMousePosition = newMousePosition;
        }

        //短击吸色键时
        if (Input.GetKeyDown(RequiresForPipette))
        {
            //if (Input.GetKey(RequiresForPipette1) || Input.GetKey(RequiresForPipette2)) { }
            var ray = mainCamera.ScreenPointToRay((Vector2)Input.mousePosition);
            var start = ray.GetPoint(mainCamera.nearClipPlane);
            var end = ray.GetPoint(mainCamera.farClipPlane);
            var maxDistance = Vector3.Distance(start, end);
            if (maxDistance != 0.0f)
            {
                var hit = default(RaycastHit);
                var texture = paintable.Textures[0];
                if (Physics.Raycast(start, end - start, out hit, maxDistance, LayerMask))
                {
                    //防止UI穿透
                    if (!EventSystem.current.IsPointerOverGameObject())//返回结果为假，则点击在UI上
                    {
                        //获取当前canvas上对应点的像素坐标
                        var xy = P3D_Helper.CalculatePixelFromCoord(P3D_Helper.GetUV(hit, texture.Coord), painter.Tiling, painter.Offset, canvas.width, canvas.height);
                        Color pipetteColor = canvas.GetPixel((int)xy.x, (int)xy.y);

                        //更改笔刷颜色
                        Brush.Color = pipetteColor;

                        //RGB转HSV
                        float H = 0, S = 0, V = 0;
                        Color.RGBToHSV(pipetteColor, out H, out S, out V);

                        //更改调色盘
                        CM.CC.rt.anchoredPosition = new Vector2(255.0f * S, 255.0f * V);//rt.anchoredPosition，对应ColorPanel左下角为（0,0）右上角为（255,255）
                        CM.CP.SetColorPanel(Color.HSVToRGB(H, 1, 1));//H值用于此
                        CM.CC.SetShowColor();//依据rt.anchoredPosition更新小图片和currentColor
                        //ColorManager.currentColor = pipetteColor;
                    }
                }
            }
        }

        //开/闭笔刷的橡皮擦模式
        if (Input.GetKeyDown(RequiresForErase))
        {
            if (Brush.Blend == P3D_BlendMode.Replace)//AlphaErase?
            {
                Brush.Blend = P3D_BlendMode.AlphaBlend;
                Brush.Detail = null;
                currentBrushMode.text = "画笔";
            }
            else
            {
                Brush.Blend = P3D_BlendMode.Replace;
                Brush.Detail = originTexture;
                currentBrushMode.text = "橡皮擦";
            }
        }

        //变更笔刷尺寸、透明度。以滚轮为主导的写法：
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            if (Input.GetKey(RequiresForSize1) || Input.GetKey(RequiresForSize2))
            {
                //变大
                if (brushSize.value < 50.0f)
                {
                    brushSize.value += 2.0f;
                }
            }
            else if (Input.GetKey(RequiresForOpacity1) || Input.GetKey(RequiresForOpacity2))
            {
                //变实心
                if (brushSolid.value < 1.0f)
                {
                    brushSolid.value += 0.1f;
                }
            }
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            if (Input.GetKey(RequiresForSize1) || Input.GetKey(RequiresForSize2))
            {
                //变小
                if (brushSize.value > 2.0f)
                {
                    brushSize.value -= 1.0f;
                }
            }
            else if (Input.GetKey(RequiresForOpacity1) || Input.GetKey(RequiresForOpacity2))
            {
                //变透明
                if (brushSolid.value > 0.1f)
                {
                    brushSolid.value -= 0.1f;
                }
            }
        }

        //执行重做与否
        if (Input.GetKeyDown(RequiresForUndoRedo))
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    Redo();
                }
                else
                {
                    Undo();
                }
            }
        }         
    }

    void Undo()
    {
        if (undoList.Count <= 0) { return; }
        var command = undoList[undoList.Count - 1];
        command.Undo(ref canvas);
        undoList.RemoveAt(undoList.Count - 1);
        redoList.Add(command);
    }

    void Redo()
    {
        if (redoList.Count <= 0) { return; }
        var command = redoList[redoList.Count - 1];
        command.Execute(ref canvas);
        redoList.RemoveAt(redoList.Count - 1);
        undoList.Add(command);
    }

    public void SaveCurrentTex()
    {
        SaveTexAsPng(albedoPath, canvas);
    }

    public void SaveOriginTex()
    {
        SaveTexAsPng(albedoPath, originTexture);
    }

    void SaveTexAsPng(string path, Texture2D texture2D)
    {
        //Debug.Log("Save Path:" + path);
        var bytes = texture2D.EncodeToPNG();
        //var bytes = texture2D.EncodeToJPG();
        System.IO.File.WriteAllBytes(path, bytes);
    }
    /*
    void ScaleCursorBrush(int targetWidth,int targetHeight)
    {
        if (targetWidth == 0 || targetHeight == 0) { return; }
        Texture2D temp = new Texture2D(cursor_brush.width, cursor_brush.height);
        temp.SetPixels32(cursor_brush.GetPixels32());temp.Apply();

        cursor_brush.Resize(targetWidth, targetHeight);
        for (int i = 0; i < targetHeight; ++i)
        {
            for (int j = 0; j < targetWidth; ++j)
            {
                //Color newColor = temp.GetPixelBilinear((float)j / (float)targetWidth, (float)i / (float)targetHeight);
                Color newColor = temp.GetPixel(j / targetWidth, i / targetHeight);
                cursor_brush.SetPixel(j, i, newColor);
            }
        }

        cursor_brush.Apply();
        //Cursor.SetCursor(cursor_brush, new Vector2(cursor_brush.width / 2, cursor_brush.height / 2), CursorMode.Auto);
    }
     */
}