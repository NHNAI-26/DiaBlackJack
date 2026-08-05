using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Keeps a URP overlay camera aligned with the live Cinemachine output camera.
    /// The synchronization runs immediately before the overlay camera renders so blends and
    /// projection changes cannot separate world-space TextUI from its background.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class TextUIOverlayCameraSync : MonoBehaviour
    {
        [SerializeField] private Camera sourceCamera;

        private Camera _overlayCamera;

        private void Reset()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            SynchronizeFromSource();
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        private void OnBeginCameraRendering(
            ScriptableRenderContext context,
            Camera renderingCamera)
        {
            if (renderingCamera == _overlayCamera)
            {
                SynchronizeFromSource();
            }
        }

        internal void SetSourceForTesting(Camera camera)
        {
            sourceCamera = camera;
            EnsureReferences();
        }

        internal void SynchronizeFromSource()
        {
            EnsureReferences();
            if (sourceCamera == null || _overlayCamera == null)
            {
                return;
            }

            transform.SetPositionAndRotation(
                sourceCamera.transform.position,
                sourceCamera.transform.rotation);

            _overlayCamera.rect = sourceCamera.rect;
            _overlayCamera.aspect = sourceCamera.aspect;
            _overlayCamera.orthographic = sourceCamera.orthographic;
            _overlayCamera.fieldOfView = sourceCamera.fieldOfView;
            _overlayCamera.orthographicSize = sourceCamera.orthographicSize;
            _overlayCamera.nearClipPlane = sourceCamera.nearClipPlane;
            _overlayCamera.farClipPlane = sourceCamera.farClipPlane;
            _overlayCamera.usePhysicalProperties =
                sourceCamera.usePhysicalProperties;
            _overlayCamera.focalLength = sourceCamera.focalLength;
            _overlayCamera.sensorSize = sourceCamera.sensorSize;
            _overlayCamera.lensShift = sourceCamera.lensShift;
            _overlayCamera.gateFit = sourceCamera.gateFit;
            _overlayCamera.worldToCameraMatrix =
                sourceCamera.worldToCameraMatrix;
            _overlayCamera.projectionMatrix = sourceCamera.projectionMatrix;
            _overlayCamera.nonJitteredProjectionMatrix =
                sourceCamera.nonJitteredProjectionMatrix;
        }

        private void EnsureReferences()
        {
            if (_overlayCamera == null)
            {
                _overlayCamera = GetComponent<Camera>();
            }

            if (sourceCamera == null)
            {
                sourceCamera = GetComponentInParent<Camera>();
                if (sourceCamera == _overlayCamera)
                {
                    sourceCamera = null;
                }
            }
        }
    }

    internal static class TextUIOverlayLayerUtility
    {
        internal const string LayerName = "TextUI";

        internal static void ApplyRecursively(GameObject root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            int layer = LayerMask.NameToLayer(LayerName);
            if (layer < 0)
            {
                throw new InvalidOperationException(
                    $"Required Unity layer '{LayerName}' is not defined.");
            }

            Transform[] hierarchy = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform item in hierarchy)
            {
                item.gameObject.layer = layer;
            }
        }
    }
}
