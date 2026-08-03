using System;
using System.Collections;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MammonDieView : MonoBehaviour
    {
        private const string RollSurfaceName = "MammonDieRollSurface";
        private const string RollWallName = "MammonDieRollWall";
        private const int RollWallCount = 4;

        [SerializeField] private Transform dieVisual;
        [SerializeField] private Collider inputCollider;
        [SerializeField] private Rigidbody dieBody;
        [SerializeField] private float launchHeight = 0.35f;
        [SerializeField] private float launchImpulse = 1.8f;
        [SerializeField] private float lateralImpulse = 0.45f;
        [SerializeField] private float torqueImpulse = 8f;
        [SerializeField] private float rollDuration = 1.2f;
        [SerializeField] private float settleDuration = 0.2f;
        [SerializeField] private float settleLinearSpeed = 0.08f;
        [SerializeField] private float settleAngularSpeed = 0.2f;
        [SerializeField] private Vector2 rollSurfaceSize = new Vector2(1.4f, 1.2f);
        [SerializeField] private float rollSurfaceThickness = 0.08f;
        [SerializeField] private float rollWallHeight = 0.65f;
        [SerializeField] private float rollWallThickness = 0.08f;

        private Vector3 _restLocalPosition;
        private Quaternion _restLocalRotation;
        private Vector3 _rollAnchorPosition;
        private Quaternion _rollAnchorRotation;
        private GameObject _rollSurface;
        private BoxCollider _rollSurfaceCollider;
        private BoxCollider[] _rollWallColliders;
        private Coroutine _rollRoutine;
        private bool _initialized;
        private bool _requestedInteractable;
        private bool _hasRollAnchor;

        public bool IsInteractable { get; private set; }

        public int CurrentValue { get; private set; }

        private void Awake()
        {
            Initialize();
            gameObject.SetActive(false);
        }

        private void OnValidate()
        {
            launchHeight = Mathf.Max(0.05f, launchHeight);
            launchImpulse = Mathf.Max(0.1f, launchImpulse);
            lateralImpulse = Mathf.Max(0f, lateralImpulse);
            torqueImpulse = Mathf.Max(0.1f, torqueImpulse);
            rollDuration = Mathf.Max(0.1f, rollDuration);
            settleDuration = Mathf.Max(0.05f, settleDuration);
            settleLinearSpeed = Mathf.Max(0.01f, settleLinearSpeed);
            settleAngularSpeed = Mathf.Max(0.01f, settleAngularSpeed);
            rollSurfaceSize.x = Mathf.Max(0.5f, rollSurfaceSize.x);
            rollSurfaceSize.y = Mathf.Max(0.5f, rollSurfaceSize.y);
            rollSurfaceThickness = Mathf.Max(0.02f, rollSurfaceThickness);
            rollWallHeight = Mathf.Max(0.2f, rollWallHeight);
            rollWallThickness = Mathf.Max(0.02f, rollWallThickness);
            AutoBind();
        }

        private void OnDisable()
        {
            bool wasRolling = _rollRoutine != null;
            if (wasRolling)
            {
                StopCoroutine(_rollRoutine);
                _rollRoutine = null;
            }

            if (wasRolling)
            {
                RestoreRollAnchor();
            }
            else
            {
                StopPhysics();
            }

            IsInteractable = false;
        }

        private void OnDestroy()
        {
            if (_rollSurface == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_rollSurface);
            }
            else
            {
                DestroyImmediate(_rollSurface);
            }
        }

        public void Render(int? value, bool isInteractable)
        {
            Initialize();
            bool visible = value.HasValue;
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }

            _requestedInteractable = visible && isInteractable;
            IsInteractable = _rollRoutine == null && _requestedInteractable;
            if (inputCollider != null)
            {
                inputCollider.enabled = visible;
            }

            if (!visible || _rollRoutine != null)
            {
                return;
            }

            CurrentValue = value.Value;
            ApplyResultRotation(CurrentValue);
        }

        public void PlayRoll(int result)
        {
            if (result < 1 || result > 6)
            {
                return;
            }

            Initialize();
            gameObject.SetActive(true);
            if (_rollRoutine != null)
            {
                StopCoroutine(_rollRoutine);
                RestoreRollAnchor();
            }

            _rollRoutine = StartCoroutine(Roll(result, null));
        }

        public void PlayPhysicalRoll(Action<int> onLanded)
        {
            if (onLanded == null)
            {
                return;
            }

            Initialize();
            gameObject.SetActive(true);
            if (_rollRoutine != null)
            {
                StopCoroutine(_rollRoutine);
                RestoreRollAnchor();
            }

            _rollRoutine = StartCoroutine(Roll(null, onLanded));
        }

        private IEnumerator Roll(int? displayedResult, Action<int> onLanded)
        {
            IsInteractable = false;
            if (!_hasRollAnchor)
            {
                _rollAnchorPosition = transform.position;
                _rollAnchorRotation = transform.rotation;
                _hasRollAnchor = true;
            }

            Vector3 launchPosition = transform.position;
            dieVisual.localPosition = _restLocalPosition;
            dieVisual.localRotation = _restLocalRotation;
            PrepareRollSurface();

            dieBody.isKinematic = true;
            dieBody.position = launchPosition + Vector3.up * launchHeight;
            dieBody.rotation = _rollAnchorRotation;
            dieBody.isKinematic = false;
            dieBody.linearVelocity = Vector3.zero;
            dieBody.angularVelocity = Vector3.zero;
            dieBody.WakeUp();

            float sideDirection = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            Vector3 impulse = Vector3.up * launchImpulse +
                transform.right * (lateralImpulse * sideDirection) +
                transform.forward * UnityEngine.Random.Range(
                    -lateralImpulse,
                    lateralImpulse);
            Vector3 torque = new Vector3(
                UnityEngine.Random.Range(-torqueImpulse, torqueImpulse),
                UnityEngine.Random.Range(-torqueImpulse, torqueImpulse),
                UnityEngine.Random.Range(-torqueImpulse, torqueImpulse));
            dieBody.AddForce(impulse, ForceMode.VelocityChange);
            dieBody.AddTorque(torque, ForceMode.VelocityChange);

            float elapsed = 0f;
            float settledFor = 0f;
            while (elapsed < rollDuration)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
                bool isSlow = dieBody.linearVelocity.sqrMagnitude <=
                        settleLinearSpeed * settleLinearSpeed &&
                    dieBody.angularVelocity.sqrMagnitude <=
                        settleAngularSpeed * settleAngularSpeed;
                settledFor = isSlow
                    ? settledFor + Time.fixedDeltaTime
                    : 0f;
                if (settledFor >= settleDuration)
                {
                    break;
                }
            }

            int result = displayedResult ?? GetPhysicalTopFace();
            CurrentValue = result;
            FinishRollInPlace(result);
            _rollRoutine = null;
            IsInteractable = _requestedInteractable;
            onLanded?.Invoke(result);
        }

        private int GetPhysicalTopFace()
        {
            Vector3[] faceNormals =
            {
                Vector3.forward,
                Vector3.right,
                Vector3.up,
                Vector3.down,
                Vector3.left,
                Vector3.back
            };
            int topFace = 1;
            float bestUpDot = float.NegativeInfinity;
            for (int index = 0; index < faceNormals.Length; index++)
            {
                float upDot = Vector3.Dot(
                    dieVisual.TransformDirection(faceNormals[index]),
                    Vector3.up);
                if (upDot > bestUpDot)
                {
                    bestUpDot = upDot;
                    topFace = index + 1;
                }
            }

            return topFace;
        }

        private void FinishRollInPlace(int result)
        {
            Vector3 settledPosition = dieBody.position;
            StopPhysics();
            dieBody.position = settledPosition;
            dieBody.rotation = _rollAnchorRotation;
            ApplyResultRotation(result);
            Physics.SyncTransforms();
        }

        private void PrepareRollSurface()
        {
            if (_rollSurface == null)
            {
                _rollSurface = new GameObject(RollSurfaceName);
                _rollSurface.hideFlags = HideFlags.HideAndDontSave;
                int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
                if (ignoreRaycastLayer >= 0)
                {
                    _rollSurface.layer = ignoreRaycastLayer;
                }

                _rollSurfaceCollider = _rollSurface.AddComponent<BoxCollider>();
                _rollWallColliders = new BoxCollider[RollWallCount];
                for (int i = 0; i < _rollWallColliders.Length; i++)
                {
                    GameObject wall = new GameObject(RollWallName);
                    wall.hideFlags = HideFlags.HideAndDontSave;
                    wall.layer = _rollSurface.layer;
                    wall.transform.SetParent(_rollSurface.transform, false);
                    _rollWallColliders[i] = wall.AddComponent<BoxCollider>();
                }
            }

            float dieHalfHeight = inputCollider != null
                ? Mathf.Max(0.05f, inputCollider.bounds.extents.y)
                : 0.2f;
            _rollSurface.transform.SetPositionAndRotation(
                _rollAnchorPosition - Vector3.up *
                    (dieHalfHeight + rollSurfaceThickness * 0.5f),
                Quaternion.identity);
            _rollSurface.transform.localScale = Vector3.one;
            _rollSurfaceCollider.size = new Vector3(
                rollSurfaceSize.x,
                rollSurfaceThickness,
                rollSurfaceSize.y);
            _rollSurfaceCollider.enabled = true;
            ConfigureRollWalls();
            Physics.SyncTransforms();
        }

        private void ConfigureRollWalls()
        {
            float centerY = (rollSurfaceThickness + rollWallHeight) * 0.5f;
            float x = (rollSurfaceSize.x + rollWallThickness) * 0.5f;
            float z = (rollSurfaceSize.y + rollWallThickness) * 0.5f;

            ConfigureRollWall(
                _rollWallColliders[0],
                new Vector3(-x, centerY, 0f),
                new Vector3(
                    rollWallThickness,
                    rollWallHeight,
                    rollSurfaceSize.y + rollWallThickness * 2f));
            ConfigureRollWall(
                _rollWallColliders[1],
                new Vector3(x, centerY, 0f),
                new Vector3(
                    rollWallThickness,
                    rollWallHeight,
                    rollSurfaceSize.y + rollWallThickness * 2f));
            ConfigureRollWall(
                _rollWallColliders[2],
                new Vector3(0f, centerY, -z),
                new Vector3(
                    rollSurfaceSize.x,
                    rollWallHeight,
                    rollWallThickness));
            ConfigureRollWall(
                _rollWallColliders[3],
                new Vector3(0f, centerY, z),
                new Vector3(
                    rollSurfaceSize.x,
                    rollWallHeight,
                    rollWallThickness));
        }

        private static void ConfigureRollWall(
            BoxCollider wall,
            Vector3 localPosition,
            Vector3 size)
        {
            wall.transform.localPosition = localPosition;
            wall.transform.localRotation = Quaternion.identity;
            wall.transform.localScale = Vector3.one;
            wall.size = size;
            wall.enabled = true;
        }

        private void RestoreRollAnchor()
        {
            StopPhysics();
            if (dieBody == null)
            {
                return;
            }

            dieBody.position = _rollAnchorPosition;
            dieBody.rotation = _rollAnchorRotation;
        }

        private void StopPhysics()
        {
            if (dieBody == null)
            {
                return;
            }

            if (!dieBody.isKinematic)
            {
                dieBody.linearVelocity = Vector3.zero;
                dieBody.angularVelocity = Vector3.zero;
            }

            dieBody.isKinematic = true;
            dieBody.Sleep();
            if (_rollSurfaceCollider != null)
            {
                _rollSurfaceCollider.enabled = false;
            }

            if (_rollWallColliders == null)
            {
                return;
            }

            for (int i = 0; i < _rollWallColliders.Length; i++)
            {
                _rollWallColliders[i].enabled = false;
            }
        }

        private void ApplyResultRotation(int value)
        {
            dieVisual.localPosition = _restLocalPosition;
            dieVisual.localRotation = _restLocalRotation *
                Quaternion.Euler(GetResultEuler(value));
        }

        private static Vector3 GetResultEuler(int value)
        {
            switch (value)
            {
                case 1: return new Vector3(-90f, 0f, 0f);
                case 2: return new Vector3(0f, 0f, 90f);
                case 3: return Vector3.zero;
                case 4: return new Vector3(180f, 0f, 0f);
                case 5: return new Vector3(0f, 0f, -90f);
                case 6: return new Vector3(90f, 0f, 0f);
                default: return Vector3.zero;
            }
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            AutoBind();
            if (dieVisual == null)
            {
                dieVisual = transform;
            }

            _restLocalPosition = dieVisual.localPosition;
            _restLocalRotation = dieVisual.localRotation;
            _initialized = true;
        }

        private void AutoBind()
        {
            if (dieVisual == null && transform.childCount > 0)
            {
                dieVisual = transform.GetChild(0);
            }

            inputCollider ??= GetComponentInChildren<Collider>(true);
            dieBody ??= GetComponent<Rigidbody>();
        }
    }
}
