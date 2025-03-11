using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;


[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class PerspectiveMappingCamera : MonoBehaviour
{
    [HideInInspector] public Vector2[] corners;
    [Header("Informations")]
    public bool interactable = false;

    [Header("Controls settings")]
    [SerializeField] public KeyCode mappingModeHotkey = KeyCode.P;
    [SerializeField] public KeyCode resetMappingHotkey = KeyCode.R;
    [SerializeField] public KeyCode showTestPatternHotkey = KeyCode.O;

    [Range(0.01f, 0.5f)]
    public float magneticCornerDistance = 0.2f;

    [Header("Background settings")]
    public Color clearColor;

    [Header("Test pattern settings")]
    public bool showTestPatternTexture;
    public Texture testPatternTexture;

    public float squareSize = 10.0f;

    [Range(0.00f, 0.1f)]
    public float lineWidth = 0.02f;
    
    private bool _isFollowingMouse = false;

    private Handles _handles = new Handles();

    private Vector3 _multiDisplayOffset;

    [HideInInspector] public Matrix4x4 matrix;

    private Camera _cam;


    void OnEnable() {
        _cam = GetComponent<Camera>();
       
        if(_cam.targetDisplay < Display.displays.Length) {
            Display.displays[_cam.targetDisplay].Activate();
        }

        ResetCorners();
        LoadCorners();
    }

    void Update() {
        _handles.magneticDistance = magneticCornerDistance;

        if(Input.GetKeyDown(mappingModeHotkey)) {
            #if !UNITY_EDITOR
                if(GetMouseCurrentDisplay() != _cam.targetDisplay)
                    return;
            #endif

            if(interactable) {
                _isFollowingMouse = false;
                SaveCorners();
            }
            interactable = !interactable;
        }

        if(Input.GetKeyDown(resetMappingHotkey)) {
            #if !UNITY_EDITOR
                if(GetMouseCurrentDisplay() != _cam.targetDisplay)
                    return;
            #endif
            ResetCorners();
        }

        if(Input.GetKeyDown(showTestPatternHotkey)) {
            #if !UNITY_EDITOR
                if(GetMouseCurrentDisplay() != _cam.targetDisplay)
                    return;
            #endif
            showTestPatternTexture = !showTestPatternTexture;
        }

        if(!interactable)
            return;
        
        Vector3 mainMousePos = Input.mousePosition;
        Vector3 relMousePos = Display.RelativeMouseAt( mainMousePos ); 
        
        int hoveredDisplay = (int) relMousePos.z;
        if( hoveredDisplay == _cam.targetDisplay ) {
            _multiDisplayOffset = mainMousePos - relMousePos;
            _multiDisplayOffset.z = 0;
        }
        #if UNITY_EDITOR
            _multiDisplayOffset = Vector3.zero;
        #endif

        #if !UNITY_EDITOR
            if( GetMouseCurrentDisplay() != _cam.targetDisplay ) {
                _handles.SelectNone();
                return;
            }
        #endif

        if(Input.GetMouseButtonDown(0)) {
            _isFollowingMouse = true;
            var mousePos = _cam.ScreenToViewportPoint(Input.mousePosition - _multiDisplayOffset);
            Debug.Log(mousePos);
            mousePos = remap(mousePos, 0f, 1f, -1f, 1f);
            // mousePos.y = -mousePos.y;
            _handles.SelectClosestHandle(mousePos);
        }

        if( _handles.current.type != HandleType.None) {
            if( _isFollowingMouse) {
                var mousePos = _cam.ScreenToViewportPoint(Input.mousePosition - _multiDisplayOffset);
                mousePos = remap(mousePos, 0f, 1f, -1f, 1f);
                // mousePos.y = -mousePos.y;
                _handles.current.SetPosition(mousePos);
            }
            else {
               

                // Translate.
                Vector2 delta = new Vector2( Input.GetAxisRaw( "Horizontal" ), Input.GetAxisRaw( "Vertical" ) * _cam.aspect ) * 0.1f;
                if( Input.GetKey( KeyCode.LeftShift ) || Input.GetKey( KeyCode.RightShift ) ) delta *= 10;
                else if(  Input.GetKey( KeyCode.LeftControl ) || Input.GetKey( KeyCode.RightControl ) ) delta *= 0.2f;
                _handles.current.SetPosition(_handles.current.GetPosition() + delta * Time.deltaTime);
                    
            }
        }

        if(Input.GetMouseButtonUp(0)) {
             _isFollowingMouse = false;
             _handles.SelectNone();
        }
    }

    void OnApplicationQuit() {
        SaveCorners();
    }

    public void ResetCorners() {
        _handles.corners[1].SetPosition( - Vector2.up    - Vector2.right );
        _handles.corners[0].SetPosition( - Vector2.right + Vector2.up    );
        _handles.corners[3].SetPosition(   Vector2.up    + Vector2.right );
        _handles.corners[2].SetPosition(   Vector2.right - Vector2.up    );
    }

    public Vector2[] GetCornerListForShaderVector2() {
        Vector2[] positions = new Vector2[4];
        for (int i = 0; i < _handles.corners.Length; i++) {
            positions[i] = _handles.corners[i].GetPosition();
        }
        return positions;
    }

    public float GetMouseCurrentDisplay()
    {
        return Display.RelativeMouseAt(Input.mousePosition).z;
    }

    public float GetASpectRatio() {
        return _cam.aspect;
    }

    // TODO
    public void SaveCorners() {
        var config = new PerspectiveMappingDisplayConfig();
        config.corners = corners;
        config.displayIndex = _cam.targetDisplay;
        var path = Path.Combine(Application.streamingAssetsPath, "PerspectiveMapping", "display" + _cam.targetDisplay + "_config.json");
        if(!File.Exists(path)) {
            Debug.Log("[PerspectiveMapping] Creating config file : " + path);
            Directory.CreateDirectory(Path.Combine(Application.streamingAssetsPath, "PerspectiveMapping"));
            var stream = File.Create(path);
            stream.Close();
        }
        File.WriteAllText(path, JsonUtility.ToJson(config));
        Debug.Log("[PerspectiveMapping] Saved config file : " + path);
    }

    // TODO
    public void LoadCorners() {
        var path = Path.Combine(Application.streamingAssetsPath, "PerspectiveMapping", "display" + _cam.targetDisplay + "_config.json");
        if(File.Exists(path)) {
            var data = File.ReadAllText(path);
            var config = JsonUtility.FromJson<PerspectiveMappingDisplayConfig>(data);
            corners = config.corners;
            //Debug.Log("[PerspectiveMapping] Loaded config file : " + path);
        }
    }

    float remap(float value, float oldMin, float oldMax, float newMin, float newMax)
    {
        // S'assurer que nous ne divisons pas par zéro
        float oldRange = oldMax - oldMin;
        if (oldRange == 0)
            return newMin;
        
        float newRange = newMax - newMin;
        
        // Normaliser la valeur entre 0 et 1, puis la mapper sur la nouvelle plage
        float normalizedValue = (value - oldMin) / oldRange;
        return newMin + (normalizedValue * newRange);
    }

    Vector3 remap(Vector3 value, float oldMin, float oldMax, float newMin, float newMax) {
        var result = Vector3.zero;
        result.x = remap(value.x, oldMin, oldMax, newMin, newMax);
        result.y = remap(value.y, oldMin, oldMax, newMin, newMax);
        result.z = remap(value.z, oldMin, oldMax, newMin, newMax);
        return result;
    }
}

[Serializable]
public class PerspectiveMappingDisplayConfig {
    public int displayIndex;
    public Vector2[] corners;
}
