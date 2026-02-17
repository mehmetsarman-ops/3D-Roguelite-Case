using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AnimatorControllerManager))]
public class AnimatorControllerManagerEditor : Editor
{
    private AnimatorControllerManager manager;
    private bool showAnimatorSettings = true;
    private bool showMovementSettings = true;
    private bool showComboSettings = true;
    private bool showDebugInfo = true;
    private bool showRuntimeInfo = true;

    private GUIStyle headerStyle;
    private GUIStyle boxStyle;

    void OnEnable()
    {
        manager = (AnimatorControllerManager)target;
    }

    public override void OnInspectorGUI()
    {
        InitializeStyles();

        serializedObject.Update();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Animator Controller Manager", headerStyle);
        EditorGUILayout.Space(5);

        DrawAnimatorSettings();
        DrawMovementSettings();
        DrawComboSettings();
        DrawDebugInfo();

        if (Application.isPlaying)
        {
            DrawRuntimeInfo();
        }

        serializedObject.ApplyModifiedProperties();

        // Repaint in play mode for real-time updates
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    private void InitializeStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
        }

        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
        }
    }

    private void DrawAnimatorSettings()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        showAnimatorSettings = EditorGUILayout.Foldout(showAnimatorSettings, "Animator Settings", true);

        if (showAnimatorSettings)
        {
            EditorGUI.indentLevel++;

            SerializedProperty animatorProp = serializedObject.FindProperty("animator");
            EditorGUILayout.PropertyField(animatorProp, new GUIContent("Animator", "The Animator component to control"));

            if (animatorProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Animator component is required! The script will try to find it automatically on Start.", MessageType.Warning);
            }
            else
            {
                Animator anim = animatorProp.objectReferenceValue as Animator;
                if (anim != null && anim.runtimeAnimatorController != null)
                {
                    EditorGUILayout.LabelField("Controller:", anim.runtimeAnimatorController.name);

                    // Validate parameters
                    ValidateAnimatorParameters(anim);
                }
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawMovementSettings()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        showMovementSettings = EditorGUILayout.Foldout(showMovementSettings, "Movement Settings", true);

        if (showMovementSettings)
        {
            EditorGUI.indentLevel++;

            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isMoving"), new GUIContent("Is Moving", "Current movement state"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isInCombo"), new GUIContent("Is In Combo", "Current combo state"));
            GUI.enabled = true;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Controls:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• WASD Keys: Run Animation");
            EditorGUILayout.LabelField("• Space Key: Combo Attacks");

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawComboSettings()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        showComboSettings = EditorGUILayout.Foldout(showComboSettings, "Combo Settings", true);

        if (showComboSettings)
        {
            EditorGUI.indentLevel++;

            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentComboIndex"), new GUIContent("Current Combo Index", "Current combo in the chain"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spaceKeyHeld"), new GUIContent("Space Key Held", "Is Space key currently being held"));
            GUI.enabled = true;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Combo System:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• Hold Space: Start/Continue combo");
            EditorGUILayout.LabelField("• Release Space: End combo");
            EditorGUILayout.LabelField("• Combo Chain: 01→02→03→04→05→06→07→01");

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawDebugInfo()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        showDebugInfo = EditorGUILayout.Foldout(showDebugInfo, "Debug Info", true);

        if (showDebugInfo)
        {
            EditorGUI.indentLevel++;

            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentState"), new GUIContent("Current State", "Current animation state"));
            GUI.enabled = true;

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawRuntimeInfo()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        showRuntimeInfo = EditorGUILayout.Foldout(showRuntimeInfo, "Runtime Info", true);

        if (showRuntimeInfo)
        {
            EditorGUI.indentLevel++;

            // Real-time information
            EditorGUILayout.LabelField("Runtime Status:", EditorStyles.boldLabel);

            Color originalColor = GUI.color;

            // Current state with color coding
            switch (manager.CurrentState)
            {
                case "wait":
                    GUI.color = Color.yellow;
                    break;
                case "run":
                    GUI.color = Color.green;
                    break;
                default:
                    if (manager.CurrentState.Contains("combo"))
                        GUI.color = Color.red;
                    break;
            }

            EditorGUILayout.LabelField($"Current State: {manager.CurrentState}");
            GUI.color = originalColor;

            EditorGUILayout.LabelField($"Is Moving: {manager.IsMoving}");
            EditorGUILayout.LabelField($"Is In Combo: {manager.IsInCombo}");
            EditorGUILayout.LabelField($"Space Key Held: {manager.SpaceKeyHeld}");

            if (manager.IsInCombo)
            {
                EditorGUILayout.LabelField($"Combo Index: {manager.CurrentComboIndex}");
            }

            EditorGUILayout.Space(5);

            // Control buttons
            EditorGUILayout.LabelField("Runtime Controls:", EditorStyles.boldLabel);

            if (GUILayout.Button("Force Reset Combo"))
            {
                manager.ForceResetCombo();
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void ValidateAnimatorParameters(Animator animator)
    {
        if (animator.runtimeAnimatorController == null) return;

        string[] requiredParams = { "combo_trigger", "wait_trigger", "run_trigger", "combo_bool", "wait_bool", "run_bool" };
        bool allParamsFound = true;

        foreach (string paramName in requiredParams)
        {
            bool found = false;
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == paramName)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                allParamsFound = false;
                break;
            }
        }

        if (allParamsFound)
        {
            EditorGUILayout.HelpBox("✓ All required parameters found in Animator Controller", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("⚠ Some required parameters are missing in Animator Controller:\n" +
                                  "combo_trigger, wait_trigger, run_trigger, combo_bool, wait_bool, run_bool", MessageType.Warning);
        }
    }
}
