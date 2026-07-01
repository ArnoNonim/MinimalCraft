using System.Collections.Generic;
using _00_Work._01_Scripts.Player.PlayerCamera;
using _00_Work._01_Scripts.Player.SO;
using UnityEngine;

namespace _00_Work._01_Scripts.Player
{
    /// <summary>
    /// 플레이어 사망 시 래그돌 전환
    ///
    /// 본 구조: Body → Head, LArm, RArm, Legs → LLeg, RLeg
    /// 각 본에 Rigidbody + BoxCollider + CharacterJoint 자동 설정
    /// </summary>
    public class PlayerRagdoll : MonoBehaviour
    {
        [Header("본 참조")]
        [SerializeField] private Transform body;
        [SerializeField] private Transform head;
        [SerializeField] private Transform lArm;
        [SerializeField] private Transform rArm;
        [SerializeField] private Transform lLeg;
        [SerializeField] private Transform rLeg;

        [Header("래그돌 설정")]
        [SerializeField] private float bodyMass   = 10f;
        [SerializeField] private float limbMass   = 3f;
        [SerializeField] private float jointSpring;
        [SerializeField] private float jointDamper;

        [Header("카메라")]
        [SerializeField] private CameraLook    cameraLook;
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private Transform     cameraHolder;
        
        [Header("사망 연출")]
        [Tooltip("사망 시 날아가는 힘")]
        [SerializeField] private float deathImpulse  = 3f;
        [Tooltip("사망 시 회전 토크")]
        [SerializeField] private float deathTorque   = 2f;

        [Header("참조")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerStats    playerStats;
        [SerializeField] private Animator animator;

        // 플레이어 컨트롤 컴포넌트들 — 사망 시 비활성화
        [Header("비활성화할 컴포넌트")]
        [SerializeField] private MonoBehaviour[] disableOnDeath;

        private Transform _originalCameraHolderParent;
        private Vector3   _originalCameraHolderLocalPos;
        private Quaternion _originalCameraHolderLocalRot;
        
        private Rigidbody   _playerRb;
        private Collider    _playerCollider;
        private bool        _isDead;

        // ── 래그돌 본 데이터 ──────────────────────────────────────
        private struct BoneData
        {
            public Transform  Bone;
            public Vector3    Size;
            public float      Mass;
            public Transform  ConnectedBone; // Joint 연결 대상
        }

        // ──────────────────────────────────────────────

        private struct BoneTransform
        {
            public Vector3    LocalPosition;
            public Quaternion LocalRotation;
        }

        private Dictionary<Transform, BoneTransform> _originalTransforms = new();
        
        void Awake()
        {
            _playerRb       = GetComponent<Rigidbody>();
            _playerCollider = GetComponent<Collider>();

            if (cameraHolder != null)
            {
                _originalCameraHolderParent   = cameraHolder.parent;
                _originalCameraHolderLocalPos = cameraHolder.localPosition;
                _originalCameraHolderLocalRot = cameraHolder.localRotation;
            }
        }

        /// <summary>PlayerStats.OnDeath에서 호출</summary>
        public void OnDeath()
        {
            if (_isDead) return;
            _isDead = true;
            playerInput.IsInputBlocked = true;

            // 플레이어 컨트롤 비활성화
            foreach (var comp in disableOnDeath)
                if (comp != null) comp.enabled = false;

            // 메인 Rigidbody/Collider 비활성화
            if (_playerRb != null)
            {
                _playerRb.linearVelocity = Vector3.zero;
                _playerRb.isKinematic = true;
            }
            if (_playerCollider != null)
                _playerCollider.enabled = false;

            if (animator != null) animator.enabled = false;
            
            SaveBoneTransforms();
            // 래그돌 생성
            SetupRagdoll();

            // 사망 충격 — body에 힘 가하기
            var bodyRb = body?.GetComponent<Rigidbody>();
            if (bodyRb != null)
            {
                Vector3 impulseDir = (Vector3.up + transform.forward * -0.5f).normalized;
                bodyRb.AddForce(impulseDir * deathImpulse, ForceMode.Impulse);
                bodyRb.AddTorque(Random.insideUnitSphere * deathTorque, ForceMode.Impulse);
            }
            
            HandleDeathCamera();
        }
        
        void SaveBoneTransforms()
        {
            _originalTransforms.Clear();
            Transform[] bones = { body, head, lArm, rArm, lLeg, rLeg };
            foreach (var bone in bones)
            {
                if (bone == null) continue;
                _originalTransforms[bone] = new BoneTransform
                {
                    LocalPosition = bone.localPosition,
                    LocalRotation = bone.localRotation
                };

                // 자식들도 저장
                foreach (Transform child in bone.GetComponentsInChildren<Transform>())
                {
                    if (!_originalTransforms.ContainsKey(child))
                        _originalTransforms[child] = new BoneTransform
                        {
                            LocalPosition = child.localPosition,
                            LocalRotation = child.localRotation
                        };
                }
            }
        }

        void HandleDeathCamera()
        {
            if (cameraLook != null)
                cameraLook.SetDeadState(true); // ← 추가

            if (cameraLook != null && !cameraLook.isThirdPerson && cameraHolder != null && head != null)
            {
                cameraHolder.SetParent(head, false);

                var headRenderer = head.GetComponentInChildren<Renderer>();
                if (headRenderer != null)
                {
                    Vector3 worldCenter = headRenderer.bounds.center;
                    Vector3 localCenter = head.InverseTransformPoint(worldCenter);
                    cameraHolder.localPosition = localCenter;
                }
                else
                {
                    cameraHolder.localPosition = Vector3.zero;
                }

                cameraHolder.localRotation = Quaternion.identity;
            }
        }
        
        // ──────────────────────────────────────────────
        // 래그돌 세팅
        // ──────────────────────────────────────────────

        void SetupRagdoll()
        {
            // 본별 크기/질량/연결 설정
            var bones = new BoneData[]
            {
                new BoneData { Bone = body,  Size = new Vector3(0.5f, 0.75f, 0.25f), Mass = bodyMass, ConnectedBone = null  },
                new BoneData { Bone = head,  Size = new Vector3(0.5f, 0.5f,  0.5f),  Mass = limbMass, ConnectedBone = body  },
                new BoneData { Bone = lArm,  Size = new Vector3(0.25f, 0.6f, 0.25f), Mass = limbMass, ConnectedBone = body  },
                new BoneData { Bone = rArm,  Size = new Vector3(0.25f, 0.6f, 0.25f), Mass = limbMass, ConnectedBone = body  },
                new BoneData { Bone = lLeg,  Size = new Vector3(0.25f, 0.7f, 0.25f), Mass = limbMass, ConnectedBone = body  },
                new BoneData { Bone = rLeg,  Size = new Vector3(0.25f, 0.7f, 0.25f), Mass = limbMass, ConnectedBone = body  },
            };

            foreach (var data in bones)
            {
                if (data.Bone == null) continue;
                SetupBone(data);
            }
        }

        void SetupBone(BoneData data)
        {
            var bone = data.Bone;

            // BoxCollider
            var col = bone.GetComponent<BoxCollider>();
            if (col == null) col = bone.gameObject.AddComponent<BoxCollider>();
            col.size   = data.Size;
            col.center = Vector3.zero;

            // Rigidbody
            var rb = bone.GetComponent<Rigidbody>();
            if (rb == null) rb = bone.gameObject.AddComponent<Rigidbody>();
            
            rb.mass                   = data.Mass;
            rb.linearDamping          = 0.05f;
            rb.angularDamping         = 0.05f;
            rb.interpolation          = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.isKinematic            = false;

            if (data.ConnectedBone == null) return;

            var joint = bone.GetComponent<CharacterJoint>();
            if (joint == null) joint = bone.gameObject.AddComponent<CharacterJoint>();
            joint.connectedBody = data.ConnectedBone.GetComponent<Rigidbody>();
            joint.autoConfigureConnectedAnchor = false;

            // 이 본의 월드 피벗을 connectedBone 로컬 기준으로 변환
            // → 본이 실제로 붙어있는 위치에서 조인트 연결
            joint.anchor          = Vector3.zero;
            joint.connectedAnchor = data.ConnectedBone.InverseTransformPoint(bone.position);

            joint.lowTwistLimit  = new SoftJointLimit { limit = -70 };
            joint.highTwistLimit = new SoftJointLimit { limit =  70 };
            joint.swing1Limit    = new SoftJointLimit { limit =  70 };
            joint.swing2Limit    = new SoftJointLimit { limit =  50 };

            var drive = new SoftJointLimitSpring { spring = jointSpring, damper = jointDamper };
            joint.swingLimitSpring = drive;
            joint.twistLimitSpring = drive;
        }

        // ──────────────────────────────────────────────
        // 리셋 (리스폰용)
        // ──────────────────────────────────────────────

        public void ResetRagdoll()
        {
            if (!_isDead) return;
            _isDead = false;

            if (animator != null) animator.enabled = true;
            
            // 본 컴포넌트 제거
            Transform[] bones = { body, head, lArm, rArm, lLeg, rLeg };
            foreach (var bone in bones)
            {
                if (bone == null) continue;
                var joint = bone.GetComponent<CharacterJoint>();
                var rb    = bone.GetComponent<Rigidbody>();
                var col   = bone.GetComponent<BoxCollider>();
                if (joint != null) Destroy(joint);
                if (rb    != null) Destroy(rb);
                if (col   != null) Destroy(col);
            }
            
            RestoreBoneTransforms();
            
            if (cameraHolder != null)
            {
                cameraHolder.SetParent(_originalCameraHolderParent, false);
                cameraHolder.localPosition = _originalCameraHolderLocalPos;
                cameraHolder.localRotation = _originalCameraHolderLocalRot;
            }

            if (cameraLook != null)
                cameraLook.SetDeadState(false);

            // 플레이어 컨트롤 복구
            if (_playerRb != null) _playerRb.isKinematic = false;
            if (_playerCollider != null) _playerCollider.enabled = true;
            foreach (var comp in disableOnDeath)
                if (comp != null) comp.enabled = true;
            
            if (playerInput != null)
                playerInput.IsInputBlocked = false;
        }
        
        void RestoreBoneTransforms()
        {
            foreach (var kv in _originalTransforms)
            {
                if (kv.Key == null) continue;
                kv.Key.localPosition = kv.Value.LocalPosition;
                kv.Key.localRotation = kv.Value.LocalRotation;
            }
            _originalTransforms.Clear();
        }
    }
}