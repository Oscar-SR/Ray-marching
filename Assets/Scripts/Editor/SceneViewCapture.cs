using UnityEngine;
using UnityEditor;
using System.IO;

public class SceneViewCapture
{
    [MenuItem("Tools/Capture Scene View")]
    public static void CaptureSceneViewHQ()
    {
        var view = SceneView.lastActiveSceneView;
        if (view == null)
        {
            Debug.LogWarning("No active Scene View found.");
            return;
        }
        
        var camera = view.camera;
        
        // Resolution of the screenshot
        int width = 3840;
        int height = 2160;
        
        // Create RenderTexture
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8; // Anti-aliasing 8x para suavizado máximo
        rt.Create();

        // Asign RenderTexture
        camera.targetTexture = rt;
        camera.Render();

        // Read the pixels
        RenderTexture.active = rt;
        Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();

        // Clear
        camera.targetTexture = null;
        RenderTexture.active = null;
        rt.Release();
        Object.DestroyImmediate(rt);

        // Save as PGN
        byte[] bytes = image.EncodeToPNG();
        string folderPath = Application.dataPath + "/../Images";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileName = "SceneView_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string path = Path.Combine(folderPath, fileName);
        File.WriteAllBytes(path, bytes);

        Debug.Log("Scene View captured to: " + path);
        EditorUtility.RevealInFinder(path);
    }
}
