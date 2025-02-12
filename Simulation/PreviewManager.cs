using UnityEngine;
using UnityEngine.UI;

public class PreviewManager : MonoBehaviour
{

    public RawImage previewWindow;
    public Camera mainCamera;

   
    public int textureWidth = 1920;
    public int textureHeight = 1080;

    RenderTexture renderTexture;

    private void Awake()
    {
        // 如果没有手动指定主摄像机，则尝试查找
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("无法找到Main Camera！请确保场景中存在主摄像机。");
                return;
            }
        }

        // 创建渲染纹理
        CreateRenderTexture();

        // 设置摄像机的目标渲染纹理
        mainCamera.targetTexture = renderTexture;

        // 设置UI预览窗口的纹理
        if (previewWindow != null)
        {
            previewWindow.texture = renderTexture;
        }
        else
        {
            Debug.LogError("请在Inspector中指定预览窗口的RawImage组件！");
        }
    }

    private void CreateRenderTexture()
    {
        // 创建新的渲染纹理
        renderTexture = new RenderTexture(textureWidth, textureHeight, 24);
        renderTexture.name = "ScenePreviewRT";
        renderTexture.antiAliasing = 2;
        renderTexture.filterMode = FilterMode.Bilinear;
        renderTexture.Create();
    }

    private void OnDestroy()
    {
        // 清理资源
        if (mainCamera != null)
        {
            mainCamera.targetTexture = null;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}