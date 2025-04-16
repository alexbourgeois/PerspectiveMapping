using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class PerspectiveMappingCamera : MonoBehaviour
{
    // coordinates of original handle position
    // FIXME?: invariants can depend on aspect ratio
    public Vector2[] invariants {
        get { return handles.sources; }
        set {
            handles.sources = value;
        }
    }

    // coordinates of warped handle position
    public Vector2[] targets {
        get { return handles.GetTargetPositions(); }
        set {
            handles.SetTargetPositions(value);
        }
    }

    [Header("Informations")]
    public bool interactable = false;

    private MappingInvariants _currentInvariants = MappingInvariants.Corners;

    [Header("Controls settings")]
    [SerializeField] public MappingInvariants mappingInvariants = MappingInvariants.Corners;
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

    public MappingHandles handles = new MappingHandles();

    private Vector3 _multiDisplayOffset;

    [HideInInspector] public Matrix4x4 matrix;

    private Camera _cam;


    void OnEnable() {
        _cam = GetComponent<Camera>();
       
        if(_cam.targetDisplay < Display.displays.Length) {
            Display.displays[_cam.targetDisplay].Activate();
        }

        // Debug.Log("OnEnable");
        ResetInvariants();
        LoadInvariants();
        _currentInvariants = mappingInvariants;
    }

    void Update() {
        if(Input.GetKeyDown(mappingModeHotkey)) {
            #if !UNITY_EDITOR
                if(GetMouseCurrentDisplay() != _cam.targetDisplay)
                    return;
            #endif

            if(interactable) {
                _isFollowingMouse = false;
                SaveInvariants();
            }
            interactable = !interactable;
        }

        if(Input.GetKeyDown(resetMappingHotkey)) {
            #if !UNITY_EDITOR
                if(GetMouseCurrentDisplay() != _cam.targetDisplay)
                    return;
            #endif
            ResetInvariants();
        }

        if(Input.GetKeyDown(showTestPatternHotkey)) {
            #if !UNITY_EDITOR
                if(GetMouseCurrentDisplay() != _cam.targetDisplay)
                    return;
            #endif
            showTestPatternTexture = !showTestPatternTexture;
        }

        if (mappingInvariants != _currentInvariants) {
            _currentInvariants = mappingInvariants;
            // Debug.Log("mappingInvariants");
            ResetInvariants();
        }

        if(!interactable)
            return;
        
        handles.magneticDistance = this.magneticCornerDistance;

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
                handles.SelectNone();
                return;
            }
        #endif

        if(Input.GetMouseButtonDown(0)) {
            _isFollowingMouse = true;
            var mousePos = _cam.ScreenToViewportPoint(Input.mousePosition - _multiDisplayOffset);
            mousePos = remap(mousePos, 0f, 1f, -1f, 1f);
             Debug.Log($"raw mouse: {mousePos}");
            handles.SelectClosestHandle(mousePos);
            // Debug.Log($"warped mouse: {mousePos}");
            // Debug.Log($"source[0]: {handles.sources[0]}");
            // Debug.Log($"target[0]: {handles.targets[0].GetPosition()}");
        }

        if( handles.current.type != HandleType.None) {
            if( _isFollowingMouse) {
                var mousePos = _cam.ScreenToViewportPoint(Input.mousePosition - _multiDisplayOffset);
                mousePos = remap(mousePos, 0f, 1f, -1f, 1f);
                Debug.Log($"raw mouse: {mousePos}");
                handles.SelectClosestHandle(mousePos);
                handles.current.SetPosition(mousePos);
            }
            else {
                // Translate.
                Vector2 delta = new Vector2( Input.GetAxisRaw( "Horizontal" ), Input.GetAxisRaw( "Vertical" ) * _cam.aspect ) * 0.1f;
                if( Input.GetKey( KeyCode.LeftShift ) || Input.GetKey( KeyCode.RightShift ) ) delta *= 10;
                else if(  Input.GetKey( KeyCode.LeftControl ) || Input.GetKey( KeyCode.RightControl ) ) delta *= 0.2f;
                handles.current.SetPosition(handles.current.GetPosition() + delta * Time.deltaTime);
            }
        }

        if(Input.GetMouseButtonUp(0)) {
             _isFollowingMouse = false;
        }
    }

    void OnApplicationQuit() {
        SaveInvariants();
    }

    // TODO?: keep transformation when switching invariants
    // requires to apply homography on new invariant coordinates
    public void ResetInvariants() {
        // Debug.Log("Reset Invariants");
        Vector2[] sources = new Vector2[4];
        switch (mappingInvariants)
        {
            case MappingInvariants.Circle:
                float a = 1; // horizontal radius
                float b = 1; // vertical radius;

                // FIXME: handles are wrong when aspect ratio changes
                // -> reset invariants when aspect ratio changes ?
                float aspect = GetASpectRatio(); // width/height
                if (aspect > 1)
                    a = 1 / aspect;
                else if (aspect < 1)
                    b = aspect;

                //OpenGL coordinates
                sources[1] = new Vector2(-a, 0); // left of ellipse
                sources[0] = new Vector2( 0, b); // top of ellipse
                sources[3] = new Vector2( a, 0); // right of ellipse
                sources[2] = new Vector2( 0,-b); // bottom of ellipse
                break;

            default: // MappingInvariants.Corners
                //OpenGL coordinates
                sources[1] = new Vector2(-1,-1);
                sources[0] = new Vector2(-1, 1);
                sources[3] = new Vector2( 1, 1);
                sources[2] = new Vector2( 1,-1);
                break;
        }

        invariants = sources;
    }

    public Vector2[] GetTargetListForShaderVector2() { //Coordinates between -1 and 1
        var result = new Vector2[ targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            result[i] = targets[i];
        } 
        return result;
    }

    public Vector2[] GetSourceListForShaderVector2() { //Coordinates between -1 and 1
        var result = new Vector2[invariants.Length];
        for (int i = 0; i < invariants.Length; i++)
        {
            result[i] = invariants[i];
        } 

        return result;
    }

    public float GetMouseCurrentDisplay()
    {
        return Display.RelativeMouseAt(Input.mousePosition).z;
    }

    public float GetASpectRatio() {
        return _cam.aspect;
    }

    public void SaveInvariants() {
        var config = new PerspectiveMappingDisplayConfig();
        config.invariants = invariants;
        config.targets = targets;
        config.mappingInvariants = mappingInvariants;
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

    public void LoadInvariants() {
        var path = Path.Combine(Application.streamingAssetsPath, "PerspectiveMapping", "display" + _cam.targetDisplay + "_config.json");
        if(File.Exists(path)) {
            var data = File.ReadAllText(path);
            var config = JsonUtility.FromJson<PerspectiveMappingDisplayConfig>(data);
            invariants = config.invariants;
            targets = config.targets;
            mappingInvariants = config.mappingInvariants;
            Debug.Log("[PerspectiveMapping] Loaded config file : " + path);
        }
    }

    public static float remap(float value, float oldMin, float oldMax, float newMin, float newMax)
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

    public static Vector2 remap(Vector2 value, float oldMin, float oldMax, float newMin, float newMax) {
        var result = Vector2.zero;
        result.x = remap(value.x, oldMin, oldMax, newMin, newMax);
        result.y = remap(value.y, oldMin, oldMax, newMin, newMax);
        return result;
    }

    public static Vector3 remap(Vector3 value, float oldMin, float oldMax, float newMin, float newMax) {
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
    public MappingInvariants mappingInvariants;
    public Vector2[] invariants;
    public Vector2[] targets;
}

public enum MappingInvariants {
    Corners,
    Circle,
}

