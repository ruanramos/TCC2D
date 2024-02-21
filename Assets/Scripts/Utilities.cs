using Unity.Netcode;
using UnityEngine;

public class Utilities
{
    public static GameObject FindChildGameObjectByName(Transform parent, string name)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).name == name)
            {
                return parent.GetChild(i).gameObject;
            }

            var child = FindChildGameObjectByName(parent.GetChild(i), name);
            if (child != null)
            {
                return child;
            }
        }

        return null;
    }
        
    public static void PrintWithServerTime(string text)
    {
        Debug.Log($"{text} --- Server time: {NetworkManager.Singleton.NetworkTimeSystem.ServerTime}");
    }
}