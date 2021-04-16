using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour {
    [SerializeField] private ComputeShader rayTracingShader;
    [SerializeField] private Texture2D skyboxTexture;
    [SerializeField] private Light directionalLight;

    [SerializeField] private Vector2 sphereRadius = new Vector2(3.0f, 8.0f);
    [SerializeField] private uint spheresMax = 100;
    [SerializeField] private float spherePlacementRadius = 100.0f;

    private ComputeBuffer sphereBuffer;

    private RenderTexture target;
    private new Camera camera;

    private uint currentSample;
    private Material addMaterial;

    private void Awake() {
        camera = GetComponent<Camera>();
    }

    private void OnEnable() {
        currentSample = 0;
        SetupScene();
    }

    private void OnDisable() {
        if (sphereBuffer != null) sphereBuffer.Release();
    }

    private void SetupScene() {
        var spheres = new List<Sphere>();
        for(int i = 0; i < spheresMax; i++) {
            var sphere = new Sphere();

            sphere.radius = sphereRadius.x + Random.value * (sphereRadius.y - sphereRadius.x);
            var randomPos = Random.insideUnitCircle * spherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);

            // Reject spheres that are intersecting others
            foreach (var other in spheres) {
                float minDist = sphere.radius + other.radius;
                if(Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist) {
                    goto SkipSphere;
                }
            }

            // Albedo and specular color
            Color color = Random.ColorHSV();
            bool metal = Random.value < 0.5f;
            sphere.albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;
            // Add the sphere to the list
            spheres.Add(sphere);

        SkipSphere:
            continue;
        }

        // Assign to compute buffer
        sphereBuffer = new ComputeBuffer(spheres.Count, 40);
        sphereBuffer.SetData(spheres);
    }

    private void Update() {
        if (transform.hasChanged) {
            currentSample = 0;
            transform.hasChanged = false;
        }
    }

    private void SetShaderParameters() {
        rayTracingShader.SetTexture(0, "_SkyboxTexture", skyboxTexture);
        rayTracingShader.SetMatrix("_CameraToWorld", camera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("_CameraInverseProjection", camera.projectionMatrix.inverse);
        rayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        Vector3 l = directionalLight.transform.forward;
        rayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, directionalLight.intensity));
        rayTracingShader.SetBuffer(0, "_Spheres", sphereBuffer);
        rayTracingShader.SetInt("_NumSpheres", sphereBuffer.count);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        SetShaderParameters();
        Render(destination);
    }

    private void Render(RenderTexture destination) {
        InitRenderTexture();

        rayTracingShader.SetTexture(0, "Result", target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8f);
        rayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        if (addMaterial == null) addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        addMaterial.SetFloat("_Sample", currentSample);
        Graphics.Blit(target, destination, addMaterial);
        currentSample++;
    }

    private void InitRenderTexture() {
        if(target == null || target.width != Screen.width || target.height != Screen.height) {
            if (target != null) target.Release();

            target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create();
        }
    }

    struct Sphere {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
    };
}
