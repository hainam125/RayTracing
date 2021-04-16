using UnityEngine;

public class RayTracingMaster : MonoBehaviour {
    [SerializeField] private ComputeShader rayTracingShader;
    [SerializeField] private Texture2D skyboxTexture;
    [SerializeField] private Light directionalLight;

    private RenderTexture target;
    private new Camera camera;

    private uint currentSample = 0;
    private Material addMaterial;

    private void Awake() {
        camera = GetComponent<Camera>();
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
}
