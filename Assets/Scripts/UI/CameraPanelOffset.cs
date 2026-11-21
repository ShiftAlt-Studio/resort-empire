using UnityEngine;

namespace ResortEmpire.UI
{
    public class CameraPanelOffset : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private RectTransform panel;
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float zoomOutFactor = 1.3f;
        [SerializeField] private float perspectiveShift = 5f;

        private float currentShift;
        private float appliedShift;
        private float currentZoom;
        private float appliedZoom;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (targetCamera == null || panel == null) return;

            bool panelShown = panel.gameObject.activeInHierarchy;
            float t = 1f - Mathf.Exp(-moveSpeed * Time.unscaledDeltaTime);

            if (targetCamera.orthographic)
            {
                float baseSize = targetCamera.orthographicSize - appliedZoom;
                float targetZoom = panelShown ? baseSize * (zoomOutFactor - 1f) : 0f;
                currentZoom = Mathf.Lerp(currentZoom, targetZoom, t);

                float zoomDelta = currentZoom - appliedZoom;
                if (Mathf.Abs(zoomDelta) > 0.0001f)
                {
                    targetCamera.orthographicSize += zoomDelta;
                    appliedZoom = currentZoom;
                }
            }

            float targetShift = panelShown ? CalculateShift() : 0f;
            currentShift = Mathf.Lerp(currentShift, targetShift, t);

            float shiftDelta = currentShift - appliedShift;
            if (Mathf.Abs(shiftDelta) > 0.0001f)
            {
                targetCamera.transform.position += targetCamera.transform.right * shiftDelta;
                appliedShift = currentShift;
            }
        }

        private float CalculateShift()
        {
            Canvas canvas = panel.GetComponentInParent<Canvas>();
            if (canvas == null) return 0f;

            RectTransform canvasRect = canvas.rootCanvas.transform as RectTransform;
            float panelFraction = panel.rect.width / canvasRect.rect.width;

            if (!targetCamera.orthographic) return perspectiveShift;

            float worldWidth = 2f * targetCamera.orthographicSize * targetCamera.aspect;
            return panelFraction * worldWidth * 0.5f;
        }
    }
}