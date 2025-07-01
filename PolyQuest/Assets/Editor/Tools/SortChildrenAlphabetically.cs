using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
//---------------------------------

public class SortChildrenAlphabetically : EditorWindow
{
    [MenuItem("Tools/Sort Selected Object's Children Alphabetically")]
    static void ShowWindow()
    {
        GetWindow<SortChildrenAlphabetically>("Sort Children Alphabetically");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Sort Children Alphabetically"))
        {
            SortSelectedChildrenAlphabetically();
        }
    }

    static void SortSelectedChildrenAlphabetically()
    {
        foreach (Transform parent in Selection.transforms)
        {
            // Get children
            int childCount = parent.childCount;
            List<Transform> children = new();
            for (int i = 0; i < childCount; i++)
            {
                children.Add(parent.GetChild(i));
            }

            // Sort children using natural sort order
            children.Sort((x, y) => NaturalCompare(x.name, y.name));

            // Reassign sibling indices
            for (int i = 0; i < childCount; i++)
            {
                children[i].SetSiblingIndex(i);
            }
        }
    }

    static int NaturalCompare(string a, string b)
    {
        if (a == null || b == null)
        {
            return 0;
        }

        int aLen = a.Length, bLen = b.Length;
        int aIndex = 0, bIndex = 0;

        while (aIndex < aLen && bIndex < bLen)
        {
            char aChar = a[aIndex], bChar = b[bIndex];

            if (char.IsDigit(aChar) && char.IsDigit(bChar))
            {
                int aStart = aIndex, bStart = bIndex;

                while (aIndex < aLen && char.IsDigit(a[aIndex])) aIndex++;
                while (bIndex < bLen && char.IsDigit(b[bIndex])) bIndex++;

                string aNum = a[aStart..aIndex];
                string bNum = b[bStart..bIndex];

                int aInt = int.Parse(aNum);
                int bInt = int.Parse(bNum);

                int result = aInt.CompareTo(bInt);
                if (result != 0)
                {
                    return result;
                }
            }
            else
            {
                int result = aChar.CompareTo(bChar);
                if (result != 0)
                {
                    return result;
                }

                aIndex++;
                bIndex++;
            }
        }

        return aLen - bLen;
    }
}