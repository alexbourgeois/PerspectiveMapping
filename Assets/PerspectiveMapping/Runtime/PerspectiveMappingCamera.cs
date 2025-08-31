using UnityEngine;
using System.IO;
using System;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

    [Header("Perspective Mapping settings")]
    [SerializeField] public MappingInvariants mappingInvariants = MappingInvariants.Corners;

    [Header("Controls settings")]
#if ENABLE_INPUT_SYSTEM
    public InputActionAsset inputActions;
#else
    [SerializeField] public KeyCode mappingModeHotkey = KeyCode.P;
    [SerializeField] public KeyCode resetMappingHotkey = KeyCode.R;
    [SerializeField] public KeyCode showTestPatternHotkey = KeyCode.O;
    [SerializeField] public KeyCode exitWithoutSavingHotkey = KeyCode.Escape;
#endif

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

    [HideInInspector] public PerspectiveMappingDisplayConfig config;

    void OnEnable()
    {
        _cam = GetComponent<Camera>();

        if (_cam.targetDisplay < Display.displays.Length)
        {
            Display.displays[_cam.targetDisplay].Activate();
        }

        ResetInvariants();
        LoadInvariants();
        _currentInvariants = mappingInvariants;

#if ENABLE_INPUT_SYSTEM
        InitNewInputSystem();
#endif
    }
    
#if ENABLE_INPUT_SYSTEM
    public void InitNewInputSystem()
    {
        var playerinput = GetComponent<PlayerInput>();
        if (playerinput == null)
            playerinput = this.gameObject.AddComponent<PlayerInput>();

        if (inputActions == null)
        {
            Debug.LogError("[PerspectiveMapping] No Input Action Asset set in the inspector. You can put the one from the package in Packages/PerspectiveMapping/PerspectiveMappingInputActions.inputactions");
            return;
        }
        playerinput.actions = inputActions;
    }
#endif

    public void OnInteractable()
    {
        #if !UNITY_EDITOR
            if(GetMouseCurrentDisplay() != _cam.targetDisplay)
                return;
        #endif

        if (interactable)
        {
            _isFollowingMouse = false;
            SaveInvariants();
        }
        interactable = !interactable;
    }

    public void OnTestPattern()
    {
        if (!interactable) return;

    #if !UNITY_EDITOR
            if(GetMouseCurrentDisplay() != _cam.targetDisplay)
                return;
    #endif
        showTestPatternTexture = !showTestPatternTexture;
    }

    public void OnResetMapping()
    {
        if (!interactable) return;

        #if !UNITY_EDITOR
            if(GetMouseCurrentDisplay() != _cam.targetDisplay)
                return;
        #endif
        ResetInvariants();
    }

    public void OnExitWithoutSaving()
    {
        if (!interactable) return;
        
        handles.SelectNone();
        _isFollowingMouse = false;
        interactable = false;
        LoadInvariants();
    }

    public void OnNextHandle()
    {
        if (!interactable) return;

        int currentIndex = Array.IndexOf(handles.all, handles.current);
        
        // Tab to go forwards
        currentIndex++;
        if (currentIndex >= handles.all.Length)
            currentIndex = 0;
        
        handles.SelectHandle(currentIndex + 1);
    }

    public void OnPreviousHandle()
    {
        if (!interactable) return;

        int currentIndex = Array.IndexOf(handles.all, handles.current);

        // Shift + Tab to go backwards
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = handles.all.Length - 1;

        handles.SelectHandle(currentIndex + 1);
    }


    public void OnSelectHandle1()
    {
        if (!interactable) return;

        if (handles.current == handles.targets[0])
            handles.SelectNone();
        else
            handles.SelectHandle(1);
    }

    public void OnSelectHandle2()
    {
        if (!interactable) return;

        if (handles.current == handles.targets[1])
            handles.SelectNone();
        else
            handles.SelectHandle(2);
    }

    public void OnSelectHandle3()
    {
        if (!interactable) return;

        if (handles.current == handles.targets[2])
            handles.SelectNone();
        else
            handles.SelectHandle(3);
    }

    public void OnSelectHandle4()
    {
        if (!interactable) return;

        if (handles.current == handles.targets[3])
            handles.SelectNone();
        else
            handles.SelectHandle(4);
    }

    public void OnSelectHandleCenter()
    {
        if (!interactable) return;

        if (handles.current == handles.center)
            handles.SelectNone();
        else
            handles.SelectHandle(5);
    }

    void Update() {

        if (mappingInvariants != _currentInvariants) {
            _currentInvariants = mappingInvariants;
            ResetInvariants();
        }


#if !ENABLE_INPUT_SYSTEM
        //Check keyboard
        UpdateLegacyKeyboard();
#endif

        //Check mouse
        UpdateMouse();
    }

    public void UpdateMouse()
    {
        if (!interactable)
            return;

        //Check mouse
        handles.magneticDistance = this.magneticCornerDistance;
#if ENABLE_INPUT_SYSTEM
        Vector3 mainMousePos = Mouse.current.position.ReadValue();
#else
        Vector3 mainMousePos = Input.mousePosition;
#endif
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

#if ENABLE_INPUT_SYSTEM
        bool mouseBtn0Down = Mouse.current.leftButton.wasPressedThisFrame;
        bool mouseBtn0Up = Mouse.current.leftButton.wasReleasedThisFrame;
#else
        bool mouseBtn0Down = Input.GetMouseButtonDown(0);
        bool mouseBtn0Up = Input.GetMouseButtonUp(0);
#endif

        // listen for mouse to change state
        bool mouseButtonJustPressed = !_isFollowingMouse && mouseBtn0Down;
        bool mouseButtonJustReleased = _isFollowingMouse && mouseBtn0Up;

        if (mouseButtonJustPressed) {
            _isFollowingMouse = true;
            var mousePos = _cam.ScreenToViewportPoint(mainMousePos - _multiDisplayOffset);
            mousePos = remap(mousePos, 0f, 1f, -1f, 1f);
            handles.SelectClosestHandle(mousePos);
        }

        if (mouseButtonJustReleased) {
            _isFollowingMouse = false;
        }

        if (handles.current.type != HandleType.None) {
            if (_isFollowingMouse) {
                var mousePos = _cam.ScreenToViewportPoint(mainMousePos - _multiDisplayOffset);
                mousePos = remap(mousePos, 0f, 1f, -1f, 1f);
                // constrain handle to screen
                Vector3 clampedMousePos = MathTools.Clamp(mousePos, -1.0f, 1.0f);
                handles.SetPosition(handles.current, clampedMousePos);
            }
            else {
                // Translate.
#if ENABLE_INPUT_SYSTEM
                Vector2 delta = Vector2.zero;
                if (Keyboard.current.leftArrowKey.isPressed) delta.x -= 1;
                if (Keyboard.current.rightArrowKey.isPressed) delta.x += 1;
                if (Keyboard.current.downArrowKey.isPressed) delta.y -= 1;
                if (Keyboard.current.upArrowKey.isPressed) delta.y += 1;
                delta.y *= _cam.aspect;
                delta *= 0.1f;
                
                if (Keyboard.current.shiftKey.isPressed) delta *= 10;
                else if (Keyboard.current.ctrlKey.isPressed) delta *= 0.2f;
#else
                Vector2 delta = new Vector2( Input.GetAxisRaw( "Horizontal" ), Input.GetAxisRaw( "Vertical" ) * _cam.aspect)* 0.1f;
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) delta *= 10;
                else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) delta *= 0.2f;
#endif

                handles.SetPosition(handles.current, handles.current.GetPosition() + delta * Time.deltaTime);
            }
        }
    }
    
#if !ENABLE_INPUT_SYSTEM
    public void UpdateLegacyKeyboard()
    {
        if (Input.GetKeyUp(mappingModeHotkey))
        {
            OnInteractable();
        }

        if (!interactable)
            return;

        if (Input.GetKeyUp(resetMappingHotkey))
        {
            OnResetMapping();
        }

        if (Input.GetKeyUp(showTestPatternHotkey))
        {
            OnTestPattern();
        }

        if (Input.GetKeyUp(exitWithoutSavingHotkey))
        {
            OnExitWithoutSaving();
        }

        //Using number keys to select handles
        if (Input.GetKeyUp(KeyCode.Alpha1) || Input.GetKeyUp(KeyCode.Keypad1))
        {
            OnSelectHandle1();
        }
        else if (Input.GetKeyUp(KeyCode.Alpha2) || Input.GetKeyUp(KeyCode.Keypad2))
        {
            OnSelectHandle2();
        }
        else if (Input.GetKeyUp(KeyCode.Alpha3) || Input.GetKeyUp(KeyCode.Keypad3))
        {
            OnSelectHandle3();
        }
        else if (Input.GetKeyUp(KeyCode.Alpha4) || Input.GetKeyUp(KeyCode.Keypad4))
        {
            OnSelectHandle4();
        }
        else if (Input.GetKeyUp(KeyCode.Alpha5) || Input.GetKeyUp(KeyCode.Keypad5))
        {
            OnSelectHandleCenter();
        }

        //Using tab to cycle through handles and shift tab to go backwards
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                OnPreviousHandle();
            }
            else
            {
                OnNextHandle();
            }
        }
    }
#endif 

    void OnApplicationQuit() {
        SaveInvariants();
    }

    // TODO?: keep transformation when switching invariants
    // requires to apply homography on new invariant coordinates
    public void ResetInvariants()
    {
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
                sources[0] = new Vector2(0, b); // top of ellipse
                sources[3] = new Vector2(a, 0); // right of ellipse
                sources[2] = new Vector2(0, -b); // bottom of ellipse
                break;

            default: // MappingInvariants.Corners
                //OpenGL coordinates
                sources[1] = new Vector2(-1, -1);
                sources[0] = new Vector2(-1, 1);
                sources[3] = new Vector2(1, 1);
                sources[2] = new Vector2(1, -1);
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
        config = new PerspectiveMappingDisplayConfig();
        config.invariants = invariants;
        config.targets = targets;
        config.mappingInvariants = mappingInvariants;
        config.displayIndex = _cam.targetDisplay;

        var ui = this.GetComponent<PerspectiveMappingUI>();
        config.uiConfig = new PerspectiveMappingUIConfig();
        if (ui != null) {
            config.uiConfig.handleSize = ui.handleSize;
            config.uiConfig.idleHandleColor = ui.idleHandleColor;
            config.uiConfig.selectedHandleColor = ui.selectedHandleColor;
        }

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

    public void LoadInvariants()
    {
        var path = Path.Combine(Application.streamingAssetsPath, "PerspectiveMapping", "display" + _cam.targetDisplay + "_config.json");
        if (File.Exists(path))
        {
            var data = File.ReadAllText(path);
            config = JsonUtility.FromJson<PerspectiveMappingDisplayConfig>(data);
            invariants = config.invariants;
            targets = config.targets;
            mappingInvariants = config.mappingInvariants;
            Debug.Log("[PerspectiveMapping] Loaded config file : " + path);
        }
        else
        {
            Debug.Log("[PerspectiveMapping] No config file found for display " + _cam.targetDisplay + ".");
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
public class PerspectiveMappingDisplayConfig
{
    public int displayIndex;
    public MappingInvariants mappingInvariants;
    public Vector2[] invariants;
    public Vector2[] targets;
    public PerspectiveMappingUIConfig uiConfig;
}

[Serializable]
public class PerspectiveMappingUIConfig
{
    public Color idleHandleColor = Color.darkCyan;
    public Color selectedHandleColor = Color.darkOrange;
    public float handleSize = 20f;
}
public enum MappingInvariants
{
    Corners,
    Circle,
}

