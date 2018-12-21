using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class LoadTexture : MonoBehaviour {

    // Use this for initialization
    void Awake()
    {
        //依据本地存储的纹理更新模型
        //创建文件读取流
        FileStream fileStream = new FileStream(PresetsManager.rootPath + "/" + PresetsManager.currentPresetIndex + "/albedo.png", FileMode.Open, FileAccess.Read);
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
        GetComponent<MeshRenderer>().material.SetTexture("_MainTex", texture);
    }
}
