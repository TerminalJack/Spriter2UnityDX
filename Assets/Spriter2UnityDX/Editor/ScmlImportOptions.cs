using UnityEditor;
using UnityEngine;
using System.Collections;

namespace Spriter2UnityDX.Importing
{
    public class ScmlImportOptionsWindow : EditorWindow
    {
        public System.Action OnImport;

        void OnEnable()
        {
            titleContent = new GUIContent("Spriter Import Options");
            minSize = new Vector2(350, 270);
        }

        void OnGUI()
        {
            EditorGUILayout.Space();

            ScmlImportOptions.options.pixelsPerUnit =
                EditorGUILayout.FloatField("Pixels Per Unit", ScmlImportOptions.options.pixelsPerUnit);

            ScmlImportOptions.options.useUnitySpriteSwapping =
                EditorGUILayout.Toggle("Native Sprite Swapping", ScmlImportOptions.options.useUnitySpriteSwapping);

            ScmlImportOptions.options.importOption =
                (ScmlImportOptions.AnimationImportOption)EditorGUILayout.EnumPopup("Animation Import Style", ScmlImportOptions.options.importOption);

            EditorGUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Pixels Per Unit: The images will have their PPU import setting set to this value.  PPU is " +
                "the number of pixels of width/height in the sprite image that correspond to one distance unit " +
                "in world space.  You can typically leave this at its default value of 100.\n\n" +
                "Native Sprite Swapping: With native sprite swapping enabed, sprites will be keyed directly as " +
                "opposed to indirectly via the Texture Controller component.  See the documentation for the Texture " +
                "Controller component for more information.\n\n" +
                "Animation Import Style: Where to store animation clips.",
                MessageType.Info, wide: true);

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(100), GUILayout.Height(24)))
            {
                Close();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Import", GUILayout.Width(100), GUILayout.Height(24)))
            {
                Close();
                OnImport();
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }
    }

    public class ScmlImportOptions
    {
        public static ScmlImportOptions options = null;

	    public enum AnimationImportOption : byte { NestedInPrefab, SeparateFolder }

        public float pixelsPerUnit = 100f;
        public bool useUnitySpriteSwapping = false;
		public AnimationImportOption importOption;
    }
}