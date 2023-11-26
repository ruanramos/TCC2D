using UnityEngine;

namespace DefaultNamespace
{
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
    }
}