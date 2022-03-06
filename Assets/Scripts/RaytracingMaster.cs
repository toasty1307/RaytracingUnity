using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace RaytracingUnity.Scripts;

public class RaytracingMaster : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    public Texture SkyboxTexture;
    [Range(1, 20)]
    public float MouseSensitivity;
    public Light DirectionalLight;
    private RenderTexture _target;
    private Camera _camera;
    private uint _currentSample;
    private Material _addMaterial;
    private ComputeBuffer _sphereBuffer;
    private static readonly int _sample = Shader.PropertyToID("_Sample");
    public const int Width = 1920 * 2 * 2 * 2 * 2;

    private struct Sphere
    {
        public Vector3 Position;
        public float Radius;
        public Vector3 Albedo;
        public Vector3 Specular;
    };
    
    private void OnEnable()
    {
        _currentSample = 0;
        SetUpScene();
    }
    private void OnDisable()
    {
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
    }
    private void SetUpScene()
    {
        var spheres = new List<Sphere>();
        
        for (var i = -10; i < 10; i++)
        {
            for (var j = -10; j < 10; j++)
            {
                var center = new Vector3((float) (i + 0.9*Random.value), 0.2f, (float) (j + 0.9*Random.value));

                if ((center - new Vector3(4, 0.2f, 0)).magnitude > 0.9)
                {
                    Vector3 sphereMaterial;

                    sphereMaterial = new Vector3(0.5f * (1 + Random.value),
                        0.5f * (1 + Random.value),
                        0.5f * (1 + Random.value));
                    var s = new Sphere
                    {
                        Albedo = sphereMaterial,
                        Radius = 0.2f,
                        Position = center,
                        Specular = Vector3.one * 0.8f
                    };
                    spheres.Add(s);
                }
            }
        }
        
        spheres.Add(new Sphere
        {
            Radius = 1,
            Position = new Vector3(0, 1, 0),
            Albedo = new Vector3(0.8f, 0.8f, 0.8f),
            Specular = Vector3.one
        });
        
        spheres.Add(new Sphere
        {
            Radius = 1,
            Position = new Vector3(-4, 1, 0),
            Albedo = new Vector3(0.8f, 0.8f, 0.8f),
            Specular = Vector3.one
        });
        
        spheres.Add(new Sphere
        {
            Radius = 1,
            Position = new Vector3(4, 1, 0),
            Albedo = new Vector3(0.8f, 0.8f, 0.8f),
            Specular = Vector3.one
        });

        _sphereBuffer = new ComputeBuffer(spheres.Count, 40);
        _sphereBuffer.SetData(spheres);
    }
    
    private void Awake()
    {
        _camera = GetComponent<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(_target ?? src, dest);
    }

    private void SetShaderParameters()
    {
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        Vector3 l = DirectionalLight.transform.forward;
        RayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));
        RayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);
    }

    private void Render()
    {
        var time = DateTime.Now;
        SetShaderParameters();
        
        // Make sure we have a current render target
        InitRenderTexture();
        // Set the target and dispatch the compute shader
        RayTracingShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(Width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(9 / 16f / 8f * Width);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        _addMaterial.SetFloat(_sample, _currentSample);
        var tex = ToTexture2D(_target);
        var png = tex.EncodeToPNG();
        File.WriteAllBytes("output.png", png);
        Process.Start(new ProcessStartInfo("output.png") { UseShellExecute = true });
        _currentSample++;
        
        Debug.Log("Render time: " + (DateTime.Now - time).TotalSeconds);
    }
    
    Texture2D ToTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.ARGB32, false);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_target != null)
                _target.Release();
            // Get a render target for Ray Tracing
            _target = new RenderTexture(Width, Width / 16 * 9, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                enableRandomWrite = true
            };
            _target.Create();
            _currentSample = 0;
        }

    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            Render();
        /*
        if (transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }

        // ok rider
        var transform1 = transform;

        if (Input.GetKey(KeyCode.W))
            transform1.position += transform1.forward;
        if (Input.GetKey(KeyCode.S))
            transform1.position -= transform1.forward;
        if (Input.GetKey(KeyCode.A))
            transform1.position -= transform1.right;
        if (Input.GetKey(KeyCode.D))
            transform1.position += transform1.right;
        
        if (Input.GetKey(KeyCode.Space))
            transform1.position += transform1.up;
        if (Input.GetKey(KeyCode.LeftControl))
            transform1.position -= transform1.up;

        var mouseX = Input.GetAxis("Mouse X") * MouseSensitivity;
        var mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity;
        
        var rotation = transform1.rotation;
        
        var x = rotation.eulerAngles.x > 180
            ? rotation.eulerAngles.x - 360
            : rotation.eulerAngles.x;

        var clamp = Mathf.Clamp(x - mouseY, -80, 80);
        rotation = Quaternion.Euler(clamp, rotation.eulerAngles.y + mouseX, 0);
        
        transform1.rotation = rotation;
    */
    } 
}