using _00_Work._01_Scripts.Player;
using UnityEngine;

namespace _00_Work._01_Scripts.Block
{
    public class BlockBreakAnimator : MonoBehaviour
    {
        public BlockBreaker blockBreaker;
        public Sprite[]     breakSprites;
        public Material     overlayMaterial; // Transparent + Unlit

        private GameObject  _overlayObj;
        private Renderer[]  _faceRenderers;

        void Update()
        {
            if (!blockBreaker.IsMiningStarted)
            {
                SetActive(false);
                return;
            }

            Vector3 blockPos = blockBreaker.TargetBlockPos;

            Vector3 center = new Vector3(
                Mathf.Floor(blockPos.x) + 0.5f,
                Mathf.Floor(blockPos.y) + 0.5f,
                Mathf.Floor(blockPos.z) + 0.5f);

            if (_overlayObj == null)
                CreateOverlay();

            _overlayObj.transform.position = center;

            SetActive(true);

            int index = Mathf.FloorToInt(
                blockBreaker.BreakProgress * breakSprites.Length);

            index = Mathf.Clamp(
                index,
                0,
                breakSprites.Length - 1);

            ApplyBreakSprite(index);
        }

        void CreateOverlay()
        {
            _overlayObj = new GameObject("BreakOverlay");

            // 6면 각각 Quad로 생성
            _faceRenderers = new Renderer[6];

            Vector3[] offsets = {
                Vector3.up * 0.503f,
                Vector3.down * 0.503f,
                Vector3.left * 0.503f,
                Vector3.right * 0.503f,
                Vector3.forward * 0.503f,
                Vector3.back * 0.503f,
            };

            Quaternion[] rotations = {
                Quaternion.Euler(90f,  0f, 0f),
                Quaternion.Euler(-90f, 0f, 0f),
                Quaternion.Euler(0f,  90f, 0f),
                Quaternion.Euler(0f, -90f, 0f),
                Quaternion.Euler(0f,   0f, 0f),
                Quaternion.Euler(0f, 180f, 0f),
            };

            for (int i = 0; i < 6; i++)
            {
                var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
                face.transform.parent        = _overlayObj.transform;
                face.transform.localPosition = offsets[i];
                face.transform.localRotation = rotations[i];
                face.transform.localScale    = Vector3.one * 1.01f;

                Destroy(face.GetComponent<Collider>());

                var mat = new Material(overlayMaterial);
                face.GetComponent<Renderer>().material = mat;
                _faceRenderers[i] = face.GetComponent<Renderer>();
            }
        }

        void SetActive(bool active)
        {
            if (_overlayObj != null)
                _overlayObj.SetActive(active);
        }

        // Sprite → Texture2D 변환
        Texture2D SpriteToTexture(Sprite sprite)
        {
            if (sprite.rect.width == sprite.texture.width)
                return sprite.texture;

            var tex = new Texture2D(
                (int)sprite.rect.width,
                (int)sprite.rect.height);

            var pixels = sprite.texture.GetPixels(
                (int)sprite.rect.x,
                (int)sprite.rect.y,
                (int)sprite.rect.width,
                (int)sprite.rect.height);

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // overlayMaterial에 아틀라스 텍스처 연결
// 스프라이트 UV 좌표로 타일링/오프셋 설정

        void ApplyBreakSprite(int index)
        {
            if (index >= breakSprites.Length) return;

            Sprite sprite = breakSprites[index];
            Texture tex   = sprite.texture;

            // UV 계산
            float texW = tex.width;
            float texH = tex.height;

            Rect rect = sprite.textureRect;
            Vector2 tiling = new Vector2(rect.width  / texW, rect.height / texH);
            Vector2 offset = new Vector2(rect.x      / texW, rect.y      / texH);

            foreach (var r in _faceRenderers)
            {
                r.material.mainTexture        = tex;
                r.material.mainTextureScale   = tiling;
                r.material.mainTextureOffset  = offset;
            }
        }
        
        void OnDestroy()
        {
            if (_overlayObj != null)
                Destroy(_overlayObj);
        }
    }
}