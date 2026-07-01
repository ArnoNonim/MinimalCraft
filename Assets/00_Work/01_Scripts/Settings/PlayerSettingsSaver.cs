using System.IO;
using UnityEngine;

namespace _00_Work._01_Scripts.Settings
{
    public static class PlayerSettingsSaver
    {
        public static string SaveDirectory { get; set; }

        static string GetPath()
        {
            if (string.IsNullOrEmpty(SaveDirectory))
                throw new System.Exception(
                    "PlayerSettingsSaver.SaveDirectory 미설정 — " +
                    "초기화 코드에서 설정해줘.");

            if (!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);

            return Path.Combine(SaveDirectory, "settings.json");
        }

        public static void Save(PlayerSettingsData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(GetPath(), json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"설정 저장 실패: {e.Message}");
            }
        }

        public static PlayerSettingsData Load()
        {
            try
            {
                string path = GetPath();
                if (!File.Exists(path)) return new PlayerSettingsData();

                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<PlayerSettingsData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"설정 로드 실패: {e.Message}");
                return new PlayerSettingsData();
            }
        }
    }
}