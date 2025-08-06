#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class MandelbulbMaster : MonoBehaviour
{
    [SerializeField] private ComputeShader mandelbulbShader;

    [Header ("Fractal parameters")]
    [SerializeField, Range (1, 20)] private float fractalPower = 10;
    [SerializeField] private float darkness = 70;

    [Header ("Colour mixing")]
    [SerializeField, Range (0, 1)] private float blackAndWhite;
    [SerializeField] private Color colorA;
    [SerializeField] private Color colorB;

    [Header ("Animation Settings")]
    [SerializeField] private float powerIncreaseSpeed = 0.2f;
    
    private Light directionalLight;
    private RenderTexture _target;
    private Camera _camera;

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
        Application.targetFrameRate = 60;
        _camera = GetActiveCamera();
        directionalLight = FindObjectOfType<Light>();
    }

    void Update ()
    {
        if (Application.isPlaying)
        {
            fractalPower += powerIncreaseSpeed * Time.deltaTime;
        }
    }

    private void SetShaderParameters()
    {
        mandelbulbShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        mandelbulbShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);

        Vector3 l = directionalLight.transform.forward;
        mandelbulbShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, directionalLight.intensity));

        mandelbulbShader.SetFloat ("power", Mathf.Max (fractalPower, 1.01f));
        mandelbulbShader.SetFloat ("darkness", darkness);

        mandelbulbShader.SetFloat("blackAndWhite", blackAndWhite);
        mandelbulbShader.SetVector("colourAMix", colorA);
        mandelbulbShader.SetVector("colourBMix", colorB);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }

    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();

        // Set the target and dispatch the compute shader
        mandelbulbShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(_camera.pixelWidth / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(_camera.pixelHeight / 8.0f);
        mandelbulbShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

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