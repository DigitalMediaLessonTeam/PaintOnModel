using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Runtime.InteropServices;
using LitJson;
using System;

//单例脚本
public class PresetsManager : MonoBehaviour {
    
    int presetsCount;
    public static string rootPath;
    public static int currentPresetIndex = 0;//代表界面中当前选中的预设，被编辑的也是它。初始值被置为0。模块内切换场景不改变它的值
    public static bool needToRefreshPreset = false;
    public GameObject presetButtonPrefab;//在PresetManager的inspector面板中绑定
    //GameObject presetButtonPrefab;
    public Texture2D albedoBlank, thumbnailBlank;
    public Camera snapshooter;
    GameObject content;//content下含各preset按钮，按钮以同一prefab为雏形动态创建并更新
    GameObject header;
    GameObject body;
    GameObject date;
    List<GameObject> presetButtons;
    GameObject headerInput, bodyInput;
    int thumbnailWidth, thumbnailHeight;
    //int albedoWidth, albedoHeight;
    public Texture2D officialPreset0, officialPreset1, officialPreset2, officialPreset3;
    public Texture2D officialPresetThumbnail0, officialPresetThumbnail1, officialPresetThumbnail2, officialPresetThumbnail3;
    GameObject deleteButton, paintButton;

    //单例
    private PresetsManager() { }
    private static PresetsManager instance = null;
    public static PresetsManager Instance
    {
        get
        {
            return instance;
        }
    }
    private void Awake()
    {
        if (instance != null)
        {
            return;
        }
        else
        {
            instance = this;
            //presetButtonPrefab = Resources.Load("Prefabs/Button_Preset") as GameObject;
            //DontDestroyOnLoad(gameObject);//略有bug，先用别的方式实现跨场景通讯了
        }
    }

    // Use this for initialization
    void Start ()
    {
        rootPath = Application.persistentDataPath + "/Presets";
        DirectoryInfo dirinfo = new DirectoryInfo(rootPath);
        if (!dirinfo.Exists) { Directory.CreateDirectory(rootPath); }
        presetsCount = dirinfo.GetDirectories().Length;//依据本地存储的预设文件夹数量确定预设总数
        //PlayerPrefs.SetInt("inUsePresetIndex", 0)：用户当前使用/向外界展示的预设，只有第一次进入时0个预设，或选中预设后点击“保存”会改变它的值
        if (!PlayerPrefs.HasKey("inUsePresetIndex")) { PlayerPrefs.SetInt("inUsePresetIndex", 0); }//默认使用的外观为第一个，哪怕列表是空的没有第一个，稍后会创建
        if (currentPresetIndex == -1)
        {
            currentPresetIndex = PlayerPrefs.GetInt("inUsePresetIndex");
        }

        content = GameObject.Find("Panel_content");
        header = GameObject.Find("Text_Header");
        body = GameObject.Find("Text_Body");
        date = GameObject.Find("Text_date");
        headerInput = GameObject.Find("HeaderInputField");
        bodyInput = GameObject.Find("BodyInputField");
        header.GetComponent<Button>().onClick.AddListener(delegate {
            headerInput.SetActive(true);
            //showHeaderInput();
        });
        headerInput.GetComponent<InputField>().onEndEdit.AddListener(delegate { EndHeaderInputEdit(); });
        body.GetComponent<Button>().onClick.AddListener(delegate {
            bodyInput.SetActive(true);
            //showBodyInput(); 
        });
        bodyInput.GetComponent<InputField>().onEndEdit.AddListener(delegate { EndBodyInputEdit(); });
        headerInput.SetActive(false);
        bodyInput.SetActive(false);

        snapshooter.gameObject.SetActive(false);

        thumbnailWidth = thumbnailHeight = 200;
        //albedoWidth = albedoHeight = 512;

        presetButtons = new List<GameObject>();

        for (int i = 0; i < presetsCount; i++)
        {
            string tempDirPath = rootPath + "/" + i.ToString();
            if (!Directory.Exists(tempDirPath)) { Directory.CreateDirectory(tempDirPath); }
            presetButtons.Add(Instantiate(presetButtonPrefab));//实例化prefab
            (presetButtons[i]).transform.SetParent(content.transform, false);//放入UIcanvas

            //TODO:进行文件数据检查，比如存在与否，格式对不对。现在先认为用户不会手动改，或者改的都符合格式
            //读本地存储，初始化preset button内容

            //依据本地存储的位图更新预览图
            //创建文件读取流
            FileStream fileStream = new FileStream(tempDirPath+"/thumbnail.png", FileMode.Open, FileAccess.Read);
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
            Texture2D texture = new Texture2D(thumbnailWidth, thumbnailHeight);
            texture.LoadImage(bytes);
            //创建Sprite
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            //更新Image
            (presetButtons[i]).transform.GetChild(0).GetComponent<Image>().sprite = sprite;

            //更新Text，仅按钮header
            string jsonstr = File.ReadAllText(tempDirPath + "/data.json");
            JsonData data = JsonMapper.ToObject(jsonstr);
            (presetButtons[i]).transform.GetChild(1).GetComponent<Text>().text = data["header"].ToString();

            /*
            //当使用official官方皮肤时，禁用删除和编辑涂装的按钮
            if (currentPresetIndex >= 0 && currentPresetIndex < 4)//0-3
            {
                deleteButton.GetComponent<Button>().interactable = false;
                paintButton.GetComponent<Button>().interactable = false;
            }
            else
            {
                deleteButton.GetComponent<Button>().interactable = true;
                paintButton.GetComponent<Button>().interactable = true;
            }
             */
        }

        if (presetsCount == 0)
        {
            StartUp();
        }

        //初始化其他物体
        ChangeCurrentPresetNoClick();
        
        if (needToRefreshPreset)
        {
            RefreshThumbnail();
            RefreshDate();
            needToRefreshPreset = false;
        }
    }

    // Update is called once per frame
    //void Update() { }
    
    void ShowHeaderInput()
    {
        headerInput.SetActive(true);
    }

    void EndHeaderInputEdit()
    {
        headerInput.SetActive(false);
        string text = headerInput.transform.GetChild(headerInput.transform.childCount - 1).GetComponent<Text>().text;

        //改即时显示
        header.GetComponent<Text>().text = text;
        date.GetComponent<Text>().text = "修改日期：" + GetDateStr();
        (presetButtons[currentPresetIndex]).transform.GetChild(1).GetComponent<Text>().text = text;

        //改本地存储
        string jsonstr = File.ReadAllText(rootPath + "/" + currentPresetIndex + "/data.json");
        JsonData data = JsonMapper.ToObject(jsonstr);
        SaveJson(currentPresetIndex, text, data["body"].ToString());
    }

    void ShowBodyInput()
    {
        bodyInput.SetActive(true);
    }

    void EndBodyInputEdit()
    {
        bodyInput.SetActive(false);
        string text = bodyInput.transform.GetChild(bodyInput.transform.childCount - 1).GetComponent<Text>().text;

        //改即时显示
        body.GetComponent<Text>().text = text;
        date.GetComponent<Text>().text = "修改日期：" + GetDateStr();

        //改本地存储
        string jsonstr = File.ReadAllText(rootPath + "/" + currentPresetIndex + "/data.json");
        JsonData data = JsonMapper.ToObject(jsonstr);
        SaveJson(currentPresetIndex, data["header"].ToString(), text);
    }

    //无需点击地增加一个预设，并设为in use预设。【只在预设数为0时被调用】
    void StartUp()
    {
        AddPreset();
        //AddOfficialPresetAtIndex(0);
        //AddOfficialPresetAtIndex(1);
        //AddOfficialPresetAtIndex(2);
        //AddOfficialPresetAtIndex(3);
        PlayerPrefs.SetInt("inUsePresetIndex", 0);
    }

    public void ChangeCurrentPreset()
    {
        //取消上一个被选中的按钮放大效果
        (presetButtons[currentPresetIndex]).transform.localScale = new Vector3(1, 1, 0);

        //更改currentPreset值
        currentPresetIndex = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.transform.GetSiblingIndex() - 1;//-1因为不是从0开始的

        ChangePresetRelatedUI();
    }

    public void ChangeCurrentPresetNoClick()
    {
        //取消上一个被选中的按钮放大效果
        (presetButtons[currentPresetIndex]).transform.localScale = new Vector3(1, 1, 0);

        ChangePresetRelatedUI();
    }

    void ChangePresetRelatedUI()
    {
        string tempDirPath = rootPath + "/" + currentPresetIndex;
        //更新描述
        string jsonstr = File.ReadAllText(tempDirPath + "/data.json");
        JsonData data = JsonMapper.ToObject(jsonstr);
        header.GetComponent<Text>().text = data["header"].ToString();
        body.GetComponent<Text>().text = data["body"].ToString();
        date.GetComponent<Text>().text = "修改日期：" + data["date"].ToString();
        //headerInput.transform.GetChild(headerInput.transform.childCount - 1).GetComponent<Text>().text = data["header"].ToString();
        //bodyInput.transform.GetChild(bodyInput.transform.childCount - 1).GetComponent<Text>().text = data["body"].ToString();

        //更新模型
        //创建文件读取流
        FileStream fileStream = new FileStream(tempDirPath + "/albedo.png", FileMode.Open, FileAccess.Read);
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
        Texture2D texture = new Texture2D(512, 512);
        texture.LoadImage(bytes);

        //更新材质
        GameObject.FindGameObjectWithTag("Model").GetComponentInChildren<MeshRenderer>().material.SetTexture("_MainTex", texture);

        //更新current按钮大小，表示被选中
        (presetButtons[currentPresetIndex]).transform.localScale += new Vector3(0.1f, 0.1f, 0);

        /*
        //当使用official官方皮肤时，禁用删除和编辑涂装的按钮
        if (currentPresetIndex >= 0 && currentPresetIndex < 4)//0-3
        {
            deleteButton.GetComponent<Button>().interactable = false;
            paintButton.GetComponent<Button>().interactable = false;
        }
        else
        {
            deleteButton.GetComponent<Button>().interactable = true;
            paintButton.GetComponent<Button>().interactable = true;
        }
         */
    }

    public void DeletePreset()
    {
        //由于index与文件夹命名有关，删除有两种实现，保留编号但要改逻辑解除index与文件夹命名的联系，或者被删预设后面的预设文件夹集体重命名，我选后者。。

        //删除文件夹
        Directory.Delete(rootPath + "/" + currentPresetIndex, true);//此删法不会加入回收站

        //改后面的文件夹的名字，presetsCount还没有-1，current此时仍表示被删的index
        for (int i = currentPresetIndex+1; i < presetsCount; i++)
        {
            Directory.Move(rootPath + "/" + i.ToString(), rootPath + "/" + (i - 1).ToString());
        }

        //改ArrayList
        Destroy(presetButtons[currentPresetIndex]);//删除GameObject
        presetButtons.RemoveAt(currentPresetIndex);//删除C#对象

        //改统计量
        presetsCount -= 1;

        //若现用预设正被删除，置新现用为第一个预设
        if (PlayerPrefs.GetInt("inUsePresetIndex") == currentPresetIndex) { if (presetsCount < 1) { StartUp(); } else { PlayerPrefs.SetInt("inUsePresetIndex", 0); } }
        
        //改current为现用预设
        currentPresetIndex = PlayerPrefs.GetInt("inUsePresetIndex");
        ChangeCurrentPresetNoClick();
    }

    //添加白模预设于最末尾
    public void AddPreset()
    {
        //增加预设不影响in use的预设

        //增加文件夹
        string tempDirPath = rootPath + "/" + presetsCount;
        Directory.CreateDirectory(tempDirPath);

        //改ArrayList，减少变更，直接插入到最末尾
        presetButtons.Add(Instantiate(presetButtonPrefab));//实例化prefab
        (presetButtons[presetsCount]).transform.SetParent(content.transform, false);//放入UIcanvas

        //生成数据和文件

        //json内容
        string header = "未命名\n（点击进行命名）", body = "点击添加描述\n";
        SaveJson(presetsCount, header, body);

        //预览图内容
        //创建Sprite
        Sprite sprite = Sprite.Create(thumbnailBlank, new Rect(0, 0, thumbnailBlank.width, thumbnailBlank.height), new Vector2(0.5f, 0.5f));
        //创建png
        SaveTexAsPng(tempDirPath + "/thumbnail.png", thumbnailBlank);

        //纹理内容
        SaveTexAsPng(tempDirPath + "/albedo.png", albedoBlank);

        //更新button的内容
        (presetButtons[presetsCount]).transform.GetChild(0).GetComponent<Image>().sprite = sprite;
        (presetButtons[presetsCount]).transform.GetChild(1).GetComponent<Text>().text = header;

        //取消上一个被选中的按钮放大效果
        (presetButtons[currentPresetIndex]).transform.localScale = new Vector3(1, 1, 0);
        //改current为新加预设
        currentPresetIndex = presetsCount;
        ChangePresetRelatedUI();

        //改统计量
        presetsCount += 1;
    }

    void AddOfficialPresetAtIndex(int officialIndex)
    {
        //增加预设不影响in use的预设

        //增加文件夹
        string tempDirPath = rootPath + "/" + officialIndex;
        if (!(new DirectoryInfo(tempDirPath)).Exists)
        {
            Directory.CreateDirectory(tempDirPath);
        }

        //改ArrayList，减少变更，直接插入到最末尾
        //理论上现在应该是空队列在加初始元素，加就完事了
        presetButtons.Add(Instantiate(presetButtonPrefab));//实例化prefab
        (presetButtons[officialIndex]).transform.SetParent(content.transform, false);//放入UIcanvas

        //生成数据和文件
        string header = "", body = "";
        Texture2D thumbnail = default(Texture2D);
        Texture2D albedo = default(Texture2D);
        switch (officialIndex)
        {
            case 0:
                header = "企鹅"; body = "ChatStyle";
                thumbnail = officialPresetThumbnail0;
                albedo = officialPreset0;
                break;
            case 1:
                header = "Papyrus"; body = "《Undertale》";
                thumbnail = officialPresetThumbnail1;
                albedo = officialPreset1;
                break;
            case 2:
                header = "乔帮主"; body = "you know who";
                thumbnail = officialPresetThumbnail2;
                albedo = officialPreset2;
                break;
            case 3:
                header = "辐射"; body = "《FallOut4》";
                thumbnail = officialPresetThumbnail3;
                albedo = officialPreset3;
                break;
        }

        //json内容
        SaveJson(officialIndex, header, body);

        //预览图内容
        //Sprite内容
        Sprite sprite = Sprite.Create(thumbnail, new Rect(0, 0, thumbnail.width, thumbnail.height), new Vector2(0.5f, 0.5f));
        //创建png
        SaveTexAsPng(tempDirPath + "/thumbnail.png", thumbnail);

        //纹理内容
        SaveTexAsPng(tempDirPath + "/albedo.png", albedo);

        //更新button的内容
        (presetButtons[officialIndex]).transform.GetChild(0).GetComponent<Image>().sprite = sprite;
        (presetButtons[officialIndex]).transform.GetChild(1).GetComponent<Text>().text = header;

        //取消上一个被选中的按钮放大效果
        (presetButtons[currentPresetIndex]).transform.localScale = new Vector3(1, 1, 0);
        //改current为新加预设
        currentPresetIndex = officialIndex;
        ChangePresetRelatedUI();

        //改统计量
        presetsCount += 1;
    }

    void SaveTexAsPng(string path, Texture2D texture2D)
    {
        //Debug.Log("Save Path:" + path);
        var bytes = texture2D.EncodeToPNG();
        //var bytes = texture2D.EncodeToJPG();
        System.IO.File.WriteAllBytes(path, bytes);
    }

    void RefreshThumbnail()
    {
        //预览图内容
        Texture2D texture = GetModelSnapshoot();
        //创建Sprite
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        //创建png
        SaveTexAsPng(rootPath + "/" + currentPresetIndex + "/thumbnail.png", texture);

        //更新button的内容
        (presetButtons[currentPresetIndex]).transform.GetChild(0).GetComponent<Image>().sprite = sprite;
    }

    void RefreshDate()
    {
        string tempDirPath = rootPath + "/" + currentPresetIndex;
        //更新描述
        string jsonstr = File.ReadAllText(tempDirPath + "/data.json");
        JsonData data = JsonMapper.ToObject(jsonstr);
        SaveJson(currentPresetIndex, data["header"].ToString(), data["body"].ToString());

        date.GetComponent<Text>().text = "修改日期：" + GetDateStr();
    }

    Texture2D GetModelSnapshoot()
    {
        snapshooter.gameObject.SetActive(true);
        snapshooter.Render();//强制渲染
        RenderTexture renderTexture = snapshooter.targetTexture;//拿到目标渲染纹理
        RenderTexture.active = renderTexture;
        Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height);//新建纹理存储渲染纹理
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);//把渲染纹理的像素给Texture2D,才能在项目里面使用
        tex.Apply();
        snapshooter.gameObject.SetActive(false);
        return tex;
    }

    Texture2D CreateTexFromRenderTex(RenderTexture renderTexture)
    {
        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
        var previous = RenderTexture.active;
        RenderTexture.active = renderTexture;

        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);

        RenderTexture.active = previous;

        texture2D.Apply();

        return texture2D;
    }

    string GetDateStr()
    {
        System.DateTime now = System.DateTime.Now;
        return now.Year.ToString() + "年" + now.Month.ToString() + "月" + now.Day.ToString() + "日";
    }

    void SaveJson(int dirIndex,string header,string body)
    {
        JsonData dataW = new JsonData();
        dataW["header"] = header;
        dataW["body"] = body;
        dataW["date"] = GetDateStr();
        string jsonstr = dataW.ToJson();
        //写json文件
        //找到当前路径
        FileInfo file = new FileInfo(rootPath + "/" + dirIndex + "/data.json");
        //判断有没有文件，有则打开文件，没有创建后打开文件
        StreamWriter sw = file.CreateText();
        //将转换好的字符串存进文件
        sw.WriteLine(jsonstr);
        //注意释放资源
        sw.Close();
        sw.Dispose();
    }
}