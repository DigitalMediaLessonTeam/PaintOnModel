using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;//调用外部库，需要引用命名空间
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//调用win32的弹窗类
public class Messagebox
{
    [DllImport("User32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
    public static extern int MessageBox(System.IntPtr handle, System.String message, System.String title, int type);
}

/// <summary>
/// preset指外观预设，一个预设包括描述+模型选项+预览图+纹理图
/// </summary>
public class ButtonScripts : MonoBehaviour {

    /// <summary>
    /// for buttons in scene "PresetsGallery"
    /// </summary>
    public void CancelChoiceOnClick()
    {
        StartCoroutine(ExitPresetsGallery());
        PresetsManager.currentPresetIndex = -1;
        print("cancel choose on avatar outlook and exit the avatar outlook edit module");
    }
    
    public void ConfirmChoiceOnClick()
    {
        PlayerPrefs.SetInt("inUsePresetIndex", PresetsManager.currentPresetIndex);
        PresetsManager.currentPresetIndex = -1;
        StartCoroutine(ExitPresetsGallery());
        print("save choice on avatar outlook (can be not changed) and exit the avatar outlook edit module");
    }

    public void AddPresetOnClick()
    {
        PresetsManager.Instance.AddPreset();
        //StartCoroutine(GoToPaint());
        print("click: add a new blank preset");
    }

    public void PaintPresetOnClick()
    {
        StartCoroutine(GoToPaint());
        print("click: edit the paint of choosen preset");
    }

    public void DeletePresetOnClick()
    {
        /*
         //不能被build的实现
        if (UnityEditor.EditorUtility.DisplayDialog("删除预设", "您确认要删除所选中的预设外观吗？", "取消", "确认")){}
         */
        print("click: want to delete the choosen preset");
        if (Messagebox.MessageBox(System.IntPtr.Zero, "您确认要删除所选中的预设外观吗？", "删除预设", 1) == 1)
        {
            PresetsManager.Instance.DeletePreset();
            print("deleted");
        }
        else
        {
            print("cancel delete");
        }
    }

    public void ChangeCurrentPresetOnClick()
    {
        PresetsManager.Instance.ChangeCurrentPreset();
        //print("click to alter current choosen preset");
    }
    

    /// <summary>
    /// for buttons in 进入此模块前的前置场景，未来可能被删除
    /// </summary>
    public void GoToAvatarOutlookModuleOnClick()
    {
        StartCoroutine(EnterPresetsGallery());
        print("click: move to the avatar outlook edit module");
    }


    /// <summary>
    /// for buttons in scene "Paint"
    /// </summary>
    public void CancelPaintOnClick()
    {
        PaintManager.Instance.SaveOriginTex();
        StartCoroutine(EnterPresetsGallery());
        print("cancel changing paint on target preset and go back to the presets gallery");
    }

    public void ConfirmPaintOnClick()
    {
        PaintManager.Instance.SaveCurrentTex();
        PresetsManager.needToRefreshPreset = true;
        StartCoroutine(EnterPresetsGallery());
        print("save new version of paint on target preset and go back to the presets gallery");
    }

    public void ShowOperationHintOnClick()
    {
        GameObject btn = GameObject.Find("Button_OperationHint");
        GameObject hint = btn.transform.parent.Find("Text_hint").gameObject;
        if (hint.activeSelf)
        {
            hint.SetActive(false);
            btn.GetComponentInChildren<Text>().text = "显示操作提示";
        }
        else
        {
            hint.SetActive(true);
            btn.GetComponentInChildren<Text>().text = "隐藏操作提示";
        }
        print(this.gameObject.tag+ "click: show/hide operation hints");
    }

    public void ResetPositionOnClick()
    {
        GameObject camera = GameObject.FindGameObjectWithTag("MainCamera");
        camera.GetComponent<P3D_MouseLook>().TargetPitch = camera.GetComponent<P3D_MouseLook>().TargetYaw = 0.0f;
        camera.GetComponent<P3D_MouseMove>().targetPosition = new Vector3(0.0f, 0.0f, 0.0f);
        print("click: reset main camera position");
    }

    public void QuickSaveOnClick()
    {
        PaintManager.Instance.SaveCurrentTex();
        print("click: save current texture");
    }

    /// <summary>
    /// coroutines to switch scenes
    /// </summary>
    IEnumerator ExitPresetsGallery() { AsyncOperation op = SceneManager.LoadSceneAsync("SampleScene"); yield return new WaitForEndOfFrame(); op.allowSceneActivation = true; }

    IEnumerator GoToPaint() { AsyncOperation op = SceneManager.LoadSceneAsync("Paint"); yield return new WaitForEndOfFrame(); op.allowSceneActivation = true; }

    IEnumerator EnterPresetsGallery() { AsyncOperation op = SceneManager.LoadSceneAsync("PresetsGallery"); yield return new WaitForEndOfFrame(); op.allowSceneActivation = true; }
}
