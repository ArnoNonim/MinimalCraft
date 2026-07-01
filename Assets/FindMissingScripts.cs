// 아무 오브젝트에 임시로 붙이고 실행
using UnityEngine;

public class FindMissingScripts : MonoBehaviour
{
    void Start()
    {
        var all = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in all)
        {
            var components = obj.GetComponents<Component>();
            foreach (var c in components)
            {
                if (c == null)
                    Debug.Log($"Missing Script: {obj.name}", obj);
            }
        }
    }
}