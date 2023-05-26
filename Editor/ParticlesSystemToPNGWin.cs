using UnityEngine;
using UnityEditor;
using System.IO;

public class ParticlesSystemToPNGWin : EditorWindow
{    
#if UNITY_EDITOR
    private ParticleSystem _particlesSystem;
    private Camera _particlesCamera;
    private int _width = 512;
    private int _height = 512;
    private string _fileName = "ParticlesFrame";
    private string _folderName = "ParticlesRenderd";
    private int _numFrames = 30;
    private float _frameDuration = 0.1f;
    private bool _playOnAwake = false;
    private bool _cameraAutoFit = true;
    private bool _alert = true;
    private const string _helpBox = "1. Create a camera in your scene. You don't need to worry about its settings for now." +
        "\n2. Drag and drop your newly created camera into the Camera field of the plugin.\n3. Drag and drop your particle system into the Particle System field." +
        "\n4. Specify a file name for the PNGs you want to capture." +
        "\n5. Specify a folder name to save the files. You can also include subfolders by using the forward slash (/) character. If the folder doesn't exist, the plugin will create it automatically and save all the files in it." +
        "\n6. Determine the number of frames you want to capture." +
        "\n7. Set the duration between frames capturing." +
        "\n8. Enable (Camera Auto Size) to optimize PNG capture by adjusting the camera size to fit the particle system, reducing overdraw.";
    

    private float startTime;
    private bool showTabContent;

    [MenuItem("Tools/Particles System To PNGs")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(ParticlesSystemToPNGWin));
    }

    private void Awake()
    {
        if (_playOnAwake)
        {
            _particlesSystem.Play();
        }        
    }

    private void OnGUI()
    {
        GUILayout.Label("Capture Settings", EditorStyles.boldLabel);

        _particlesSystem = EditorGUILayout.ObjectField("Particles System", _particlesSystem, typeof(ParticleSystem), true) as ParticleSystem;
        _particlesCamera = EditorGUILayout.ObjectField("Particles Camera", _particlesCamera, typeof(Camera), true) as Camera;
        _width = EditorGUILayout.IntField("Width", _width);
        _height = EditorGUILayout.IntField("Height", _height);
        _fileName = EditorGUILayout.TextField("File Name", _fileName);
        _folderName = EditorGUILayout.TextField("Folder Name", _folderName);
        _numFrames = EditorGUILayout.IntField("Num Frames", _numFrames);
        _frameDuration = EditorGUILayout.FloatField("Frame Duration", _frameDuration);
        _playOnAwake = EditorGUILayout.Toggle("Play On Awake(Play Mode)", _playOnAwake);
        _cameraAutoFit = EditorGUILayout.Toggle("Camera auto size", _cameraAutoFit);
        _alert = EditorGUILayout.Toggle("Show Alert", _alert);

        GUIStyle greenButtonStyle = new GUIStyle(GUI.skin.button);
        greenButtonStyle.normal.textColor = Color.cyan;
        greenButtonStyle.hover.textColor = Color.green;
        greenButtonStyle.fontSize = 20;
        greenButtonStyle.fontStyle = FontStyle.Bold;

        if (GUILayout.Button(">>> CAPTURE <<<", greenButtonStyle, GUILayout.Height(50)))
        {
            Capture();
        }
       
        showTabContent = EditorGUILayout.Foldout(showTabContent, "How to use ?");
        if (showTabContent)
        {            
            EditorGUILayout.HelpBox(_helpBox, MessageType.Info);
            EditorGUILayout.HelpBox("Camera\nBy following these steps, the plugin will configure the camera to use an orthographic view and adjust its position and scale to fit the particle system. This ensures accurate and consistent captures of your particles effects.", MessageType.Warning);
        }

        GUIStyle linkButton = new GUIStyle(GUI.skin.button);        
        linkButton.hover.textColor = Color.green;
        linkButton.fontSize = 8;        
        if (GUILayout.Button("NikDorn.com", linkButton))
        {
            Application.OpenURL("https://nikdorn.com/");
        }
    }

    private void Capture()
    {        
        string folderPath = Application.dataPath + "/" + _folderName;
        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }

        startTime = Time.time;
        float timePerFrame = _particlesSystem.main.duration / (float)_numFrames;
        RenderTexture renderTexture = new RenderTexture(_width, _height, 24);
        _particlesSystem.gameObject.SetActive(true);
        Camera camera = _particlesCamera;
        camera.orthographic = true;
        if (_cameraAutoFit)
        {
            ParticleSystemRenderer[] renderers = _particlesSystem.GetComponentsInChildren<ParticleSystemRenderer>();
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float cameraSize = Mathf.Max(bounds.size.x, bounds.size.y) / 2f;
            camera.orthographicSize = cameraSize;
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, camera.transform.position.z);
        }
        camera.targetTexture = renderTexture;

        for (int i = 0; i < _numFrames; i++)
        {
            float progress = (float)i / _numFrames;
            EditorUtility.DisplayProgressBar("Capturing Frames", "Frame " + i + " of " + _numFrames, progress);

            float time = startTime + i * timePerFrame;
            _particlesSystem.Simulate(_frameDuration, true, false);
            _particlesSystem.Play();
            camera.Render();
            RenderTexture.active = renderTexture;
            Texture2D texture = new Texture2D(_width, _height, TextureFormat.ARGB32, false);
            texture.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            texture.Apply();
            byte[] bytes = texture.EncodeToPNG();           
            string filePath = Path.Combine(Application.dataPath, _folderName.Replace('/', '\\'), _fileName + "_" + i + ".png");
            System.IO.File.WriteAllBytes(filePath, bytes);
            Debug.Log("Saved particle system frame " + i + " to " + filePath);
            RenderTexture.active = null;
        }
        EditorUtility.ClearProgressBar();
        if (_alert)
        {
            EditorUtility.DisplayDialog("Hold on tight, things are about to get crazy!", "To generate folders and files in Unity, try minimizing and maximizing the Unity editor. This action triggers the generation process.", "Roger that, Captain!", "Sure, why not?");
        }

    }
#endif
}

