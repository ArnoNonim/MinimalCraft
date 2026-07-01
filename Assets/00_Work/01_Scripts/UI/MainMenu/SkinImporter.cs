using System.IO;
using SFB;
using UnityEngine;

namespace _00_Work._01_Scripts.UI.MainMenu
{
    public class SkinImporter : MonoBehaviour
    {
        [SerializeField] private Material targetMaterial;
        [SerializeField] private Texture2D defaultSkin;

        private static string SkinSavePath =>
            Path.Combine(Application.persistentDataPath, "PlayerSkin.png");

        private void Start()
        {
            LoadSavedSkinOrDefault();
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        public void OpenFileAndApplySkin()
        {
            var extensions = new[]
            {
                new ExtensionFilter("Image Files", "png", "jpg", "jpeg")
            };

            string[] paths = StandaloneFileBrowser.OpenFilePanel("스킨 이미지 선택", "", extensions, false);

            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                byte[] fileData = File.ReadAllBytes(paths[0]);
                Texture2D texture = LoadTextureFromBytes(fileData);
                if (texture == null) return;

                ApplyToMaterial(texture);
                SaveSkin(texture);
            }
        }

        // ──────────────────────────────────────────────
        // 내부
        // ──────────────────────────────────────────────

        private void LoadSavedSkinOrDefault()
        {
            if (File.Exists(SkinSavePath))
            {
                byte[] fileData = File.ReadAllBytes(SkinSavePath);
                Texture2D texture = LoadTextureFromBytes(fileData);
                if (texture != null)
                {
                    ApplyToMaterial(texture);
                    return;
                }
            }

            // 저장 파일 없거나 로드 실패 → 기본 스킨
            if (defaultSkin != null)
                targetMaterial.SetTexture("_BaseMap", defaultSkin);
        }

        private Texture2D LoadTextureFromBytes(byte[] data)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32,
                mipChain: true,
                linear: false);

            if (!texture.LoadImage(data))
            {
                Debug.LogError("[SkinImporter] 이미지 로드 실패");
                Destroy(texture);
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode   = TextureWrapMode.Repeat;
            texture.anisoLevel = 1;
            texture = ResizeToMaxSize(texture, 64);
            texture.Apply(updateMipmaps: true, makeNoLongerReadable: false);

            return texture;
        }

        private void ApplyToMaterial(Texture2D texture)
        {
            targetMaterial.SetTexture("_BaseMap", texture);
        }

        private void SaveSkin(Texture2D texture)
        {
            byte[] png = texture.EncodeToPNG();
            if (png == null)
            {
                Debug.LogError("[SkinImporter] PNG 인코딩 실패");
                return;
            }

            File.WriteAllBytes(SkinSavePath, png);
            Debug.Log($"[SkinImporter] 스킨 저장 완료: {SkinSavePath}");
        }

        private Texture2D ResizeToMaxSize(Texture2D src, int maxSize)
        {
            if (src.width <= maxSize && src.height <= maxSize)
                return src;

            float ratio = Mathf.Min((float)maxSize / src.width, (float)maxSize / src.height);
            int newW = Mathf.Max(1, Mathf.RoundToInt(src.width  * ratio));
            int newH = Mathf.Max(1, Mathf.RoundToInt(src.height * ratio));

            RenderTexture rt = RenderTexture.GetTemporary(newW, newH, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.filterMode = FilterMode.Point;

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(src, rt);

            Texture2D resized = new Texture2D(newW, newH, TextureFormat.RGBA32,
                mipChain: true, linear: false);
            resized.ReadPixels(new Rect(0, 0, newW, newH), 0, 0);
            resized.filterMode = FilterMode.Point;
            resized.wrapMode   = TextureWrapMode.Repeat;
            resized.anisoLevel = 1;

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            Destroy(src);

            return resized;
        }
    }
}