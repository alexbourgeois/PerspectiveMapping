using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PerspectiveMappingCamera))]
public class PerspectiveMappingUI : MonoBehaviour
{
    [Header("UI Customisation")]
    [Range(10f, 100f)]
    public float handleSize = 20f;
    public Color idleHandleColor = Color.darkCyan;
    public Color selectedHandleColor = Color.darkOrange;

    private PerspectiveMappingCamera _perspectiveMappingCamera;

    //UI elements
    private RectTransform _canvasRt;
    private List<Image> _handlesImgs = new List<Image>();
    private List<RectTransform> _handlesRts = new List<RectTransform>();
    private Canvas _canvas;
    private Camera _camera;

    void Start()
    {
        _perspectiveMappingCamera = GetComponent<PerspectiveMappingCamera>();
        SetupCanvas();
    }

    void Update()
    {
        UpdateHandles();
    }

    public void UpdateHandles()
    {
        if (_canvasRt == null)
            return;
        
        if (!_perspectiveMappingCamera.interactable)
        {
            _canvasRt.gameObject.SetActive(false);
            return;
        }
        else
        {
            _canvasRt.gameObject.SetActive(true);
        }

        if (_canvas.targetDisplay != _camera.targetDisplay)
            _canvas.targetDisplay = _camera.targetDisplay;

        // Update the handles based on the current perspective mapping camera settings
        for (int i = 0; i < _handlesRts.Count; i++)
        {
            MappingHandles.Handle handle = _perspectiveMappingCamera.handles.all[i];
            if (_handlesRts[i] != null)
            {
                var posX = handle.GetPosition().x * _canvasRt.rect.width / 2.0f;
                var posY = handle.GetPosition().y * _canvasRt.rect.height / 2.0f;
                _handlesRts[i].anchoredPosition = new Vector2(posX, posY); // Set position of the handle
                _handlesRts[i].sizeDelta = new Vector2(handleSize, handleSize);
            }

            var img = _handlesImgs[i];
            if (handle == _perspectiveMappingCamera.handles.current)
            {
                img.color = selectedHandleColor; // Set color of the handle
            }
            else
            {
                img.color = idleHandleColor; // Set color of the handle
            }
        }
    }

    public void SetupCanvas()
    {
        //Load configuration
        if (_perspectiveMappingCamera.config.uiConfig != null)
        {
            handleSize = _perspectiveMappingCamera.config.uiConfig.handleSize;
            idleHandleColor = _perspectiveMappingCamera.config.uiConfig.idleHandleColor;
            selectedHandleColor = _perspectiveMappingCamera.config.uiConfig.selectedHandleColor;
        }

        GameObject canvasGO = new GameObject("PerspectiveMapping_Canvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _camera = this.GetComponent<Camera>();
        _canvas.targetDisplay = _camera.targetDisplay;
        _canvas.transform.SetParent(this.transform, false);
        _canvas.sortingOrder = 1000; // Set a high sorting order to ensure it appears above other UI elements

        Debug.Log("[PerspectiveMappingUI] Setting up PerspectiveMapping UI on display " + _canvas.targetDisplay);

        _canvasRt = _canvas.GetComponent<RectTransform>();
        _canvasRt.anchoredPosition3D = Vector3.zero;

        //Add an Image for each Handle
        for (int i = 0; i < _perspectiveMappingCamera.handles.all.Length; i++)
        {
            MappingHandles.Handle handle = _perspectiveMappingCamera.handles.all[i];

            GameObject handleObject = new GameObject("Handle_" + i);
            handleObject.transform.SetParent(_canvas.transform, false);

            RectTransform rectTransform = handleObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(handleSize, handleSize); // Set size of the handle
            var posX = handle.GetPosition().x * _canvasRt.rect.width / 2.0f;
            var posY = handle.GetPosition().y * _canvasRt.rect.height / 2.0f;
            rectTransform.anchoredPosition = new Vector2(posX, posY); // Set position of the handle

            _handlesRts.Add(rectTransform);

            Image image = handleObject.AddComponent<Image>();
            image.color = idleHandleColor; // Set color of the handle

            _handlesImgs.Add(image);
        }
    }

    public void CleanUpUI()
    {
        Debug.Log("[PerspectiveMappingUI] Cleaning up PerspectiveMapping UI");
        if (_canvasRt != null)
        {
            Destroy(_canvasRt.gameObject);
            _canvasRt = null;

            _handlesImgs.Clear();
            _handlesRts.Clear();
        }
    }
}
