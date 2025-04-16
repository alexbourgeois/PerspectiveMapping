using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(PerspectiveMappingCamera))]
public class PerspectiveMappingUI : MonoBehaviour
{
    public PerspectiveMappingCamera perspectiveMappingCamera;

    private RectTransform _canvasRt;
    private List<Image> _handlesImgs;
    void OnEnable()
    {
        perspectiveMappingCamera = GetComponent<PerspectiveMappingCamera>();
        //Remove all children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        SetupCanvas();
    }

    void Update()
    {
        UpdateHandles();
    }

    public void UpdateHandles()
    {
        // Update the handles based on the current perspective mapping camera settings
        for (int i = 0; i < _handlesImgs.Count; i++)
        {
            MappingHandles.Handle handle = perspectiveMappingCamera.handles.all[i];
            RectTransform rectTransform = _canvasRt.GetChild(i).GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                var posX = handle.GetPosition().x * _canvasRt.rect.width/2.0f;
                var posY = handle.GetPosition().y * _canvasRt.rect.height/2.0f;
                rectTransform.anchoredPosition = new Vector2(posX, posY); // Set position of the handle
            }
            
            var img = rectTransform.GetComponent<Image>();
            if(handle == perspectiveMappingCamera.handles.center) {
                img.color = Color.yellow; // Set color of the handle
            } else {
                img.color = Color.red; // Set color of the handle
            }
            if(handle == perspectiveMappingCamera.handles.current) {
                img.color = Color.green; // Set color of the handle
            } else {
                img.color = Color.red; // Set color of the handle
            }
        }
    }

    void SetupCanvas() {
        // Set up the canvas for the perspective mapping UI
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("PerspectiveMapping_Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.targetDisplay = this.GetComponent<Camera>().targetDisplay;
        }
        canvas.transform.SetParent(this.transform, false);
        canvas.sortingOrder = 1000; // Set a high sorting order to ensure it appears above other UI elements

        _canvasRt = canvas.GetComponent<RectTransform>();
        _canvasRt.anchoredPosition3D = Vector3.zero;

        _handlesImgs = new List<Image>();

        //Add an Image for each Handle
        for (int i = 0; i < perspectiveMappingCamera.handles.all.Length; i++)
        {
            MappingHandles.Handle handle = perspectiveMappingCamera.handles.all[i];

            GameObject handleObject = new GameObject("Handle_" + i);
            handleObject.transform.SetParent(canvas.transform);
            RectTransform rectTransform = handleObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(20, 20); // Set size of the handle
            var posX = handle.GetPosition().x * _canvasRt.rect.width/2.0f;
            var posY = handle.GetPosition().y * _canvasRt.rect.height/2.0f;
            rectTransform.anchoredPosition = new Vector2(posX, posY); // Set position of the handle

            Image image = handleObject.AddComponent<Image>();
            if(handle == perspectiveMappingCamera.handles.center) {
                image.color = Color.yellow; // Set color of the handle
            } else {
                image.color = Color.red; // Set color of the handle
            }

            _handlesImgs.Add(image);
        }
    }
}
