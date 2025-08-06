#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class RayMarchingMaster : MonoBehaviour
{
    [SerializeField] private ComputeShader rayMarchingShader;

    [Header("Background Settings")]
    [SerializeField] private bool useDefaultBackground;
    [SerializeField] private Color backgroundColor;
    
    [Header("Glow Settings")]
    [SerializeField] private bool glowActive;
    [SerializeField] private Color lowGlowColor;
    [SerializeField] private Color highGlowColor;
    [SerializeField, Range(0.1f, 5f)] private float glowFallOff = 1.3f;
    
    private Light directionalLight;
    private RenderTexture _target;
    private Camera _camera;
    private ComputeBuffer shapeBuffer;

    Camera GetActiveCamera()
    {
    #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return Camera.current; // Camera of the scene
        }
    #endif

        return Camera.main;
    }

    private void Start()
    {
        _camera = GetActiveCamera();
        directionalLight = FindObjectOfType<Light>();
    }

    private void OnDisable()
    {
        if (shapeBuffer != null)
        {
            shapeBuffer.Release();
        }
    }

    private void SetShaderParameters()
    {
        rayMarchingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        rayMarchingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);

        Vector3 l = directionalLight.transform.forward;
        rayMarchingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, directionalLight.intensity));

        rayMarchingShader.SetBool("_UseDefaultBackground",useDefaultBackground);
        rayMarchingShader.SetVector("_BackgroundColor", backgroundColor);
        rayMarchingShader.SetBool("_GlowActive", glowActive);
        rayMarchingShader.SetVector("_LowGlowColor", lowGlowColor);
        rayMarchingShader.SetVector("_HighGlowColor", highGlowColor);
        rayMarchingShader.SetFloat("_GlowFallOff", glowFallOff);
    }

    void SetUpScene()
    {
        //_camera = Camera.current;
        List<Shape> allShapes = new List<Shape>(FindObjectsOfType<Shape>());
        allShapes.Sort((a, b) => a.Operation.CompareTo(b.Operation));

        List<Shape> orderedShapes = new List<Shape>();

        for (int i = 0; i < allShapes.Count; i++) {
            // Add top-level shapes (those without a parent)
            if (allShapes[i].transform.parent == null) {

                Transform parentShape = allShapes[i].transform;
                orderedShapes.Add (allShapes[i]);
                allShapes[i].NumChildren = parentShape.childCount;

                // Add all children of the shape (nested children not supported currently)
                for (int j = 0; j < parentShape.childCount; j++) {
                    if (parentShape.GetChild(j).GetComponent<Shape>() != null) {
                        orderedShapes.Add(parentShape.GetChild(j).GetComponent<Shape> ());
                        orderedShapes[orderedShapes.Count - 1].NumChildren = 0;
                    }
                }
            }

        }

        Shape.Data[] shapeData = new Shape.Data[orderedShapes.Count];
        for (int i = 0; i < orderedShapes.Count; i++) {
            shapeData[i] = orderedShapes[i].ToStruct();
        }

        if (shapeBuffer != null)
        {
            shapeBuffer.Release();
        }

        if(shapeData.Length > 0)
        {
            shapeBuffer = new ComputeBuffer(shapeData.Length, Marshal.SizeOf<Shape.Data>());
            shapeBuffer.SetData(shapeData);
            rayMarchingShader.SetBuffer(0, "shapes", shapeBuffer);
            rayMarchingShader.SetInt("numShapes", shapeData.Length);
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetUpScene();
        SetShaderParameters();
        Render(source, destination);
    }

    private void Render(RenderTexture source, RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();

        // Set the target and dispatch the compute shader
        rayMarchingShader.SetTexture(0, "Source", source);
        rayMarchingShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(_camera.pixelWidth / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(_camera.pixelHeight / 8.0f);
        rayMarchingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        Graphics.Blit(_target, destination);
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != _camera.pixelWidth || _target.height != _camera.pixelHeight)
        {
            // Release render texture if we already have one
            if (_target != null)
                _target.Release();

            // Get a render target for Ray Tracing
            _target = new RenderTexture(_camera.pixelWidth, _camera.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
        }
    }
}