using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Text;

public class AnimatorControllerAnalyzer : EditorWindow
{
    [SerializeField] private AnimatorController animatorController;
    private Vector2 scrollPosition;
    private string analysisResult = "";
    private GUIStyle headerStyle;
    private GUIStyle codeStyle;
    private bool autoAnalyze = true;

    [MenuItem("Tools/AV10 Animator Controller Analyzer")]
    public static void ShowWindow()
    {
        GetWindow<AnimatorControllerAnalyzer>("AV10 Animator Controller Analyzer");
    }

    void OnEnable()
    {
        minSize = new Vector2(400, 300);
    }

    void OnGUI()
    {
        InitializeStyles();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("AV10 Animator Controller Analyzer", headerStyle);
        EditorGUILayout.Space(10);

        // Drag and drop area for Animator Controller
        DrawAnimatorControllerField();

        EditorGUILayout.Space(5);

        // Auto-analyze toggle
        autoAnalyze = EditorGUILayout.Toggle("Auto Analyze", autoAnalyze);

        EditorGUILayout.Space(5);

        // Manual analyze button
        EditorGUI.BeginDisabledGroup(animatorController == null);
        if (GUILayout.Button("Analyze Animator Controller", GUILayout.Height(30)))
        {
            AnalyzeAnimatorController();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(10);

        // Clear button
        if (!string.IsNullOrEmpty(analysisResult))
        {
            if (GUILayout.Button("Clear Results"))
            {
                analysisResult = "";
            }
            EditorGUILayout.Space(5);
        }

        // Display analysis results
        if (!string.IsNullOrEmpty(analysisResult))
        {
            EditorGUILayout.LabelField("Analysis Results:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.TextArea(analysisResult, codeStyle, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
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

        if (codeStyle == null)
        {
            codeStyle = new GUIStyle(EditorStyles.textArea)
            {
                fontSize = 10,
                fontStyle = FontStyle.Normal,
                wordWrap = false
            };
        }
    }

    private void DrawAnimatorControllerField()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Drag Animator Controller Here:", EditorStyles.boldLabel);

        AnimatorController newController = (AnimatorController)EditorGUILayout.ObjectField(
            "Animator Controller",
            animatorController,
            typeof(AnimatorController),
            false
        );

        if (newController != animatorController)
        {
            animatorController = newController;
            if (autoAnalyze && animatorController != null)
            {
                AnalyzeAnimatorController();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void AnalyzeAnimatorController()
    {
        if (animatorController == null)
        {
            analysisResult = "No Animator Controller selected.";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════════════════════════");
        sb.AppendLine($"ANIMATOR CONTROLLER ANALYSIS: {animatorController.name}");
        sb.AppendLine("═══════════════════════════════════════════════════════════");
        sb.AppendLine();

        // Analyze Parameters
        AnalyzeParameters(sb);
        sb.AppendLine();

        // Analyze Layers
        AnalyzeLayers(sb);

        analysisResult = sb.ToString();
        Repaint();
    }

    private void AnalyzeParameters(StringBuilder sb)
    {
        sb.AppendLine("PARAMETERS:");
        sb.AppendLine("───────────────────────────────────────────────────────────");

        if (animatorController.parameters.Length == 0)
        {
            sb.AppendLine("  No parameters found.");
        }
        else
        {
            foreach (var param in animatorController.parameters)
            {
                string typeStr = GetParameterTypeString(param.type);
                string defaultValue = GetParameterDefaultValue(param);

                sb.AppendLine($"  • {param.name} ({typeStr}) = {defaultValue}");
            }
        }

        sb.AppendLine();
    }

    private void AnalyzeLayers(StringBuilder sb)
    {
        for (int layerIndex = 0; layerIndex < animatorController.layers.Length; layerIndex++)
        {
            var layer = animatorController.layers[layerIndex];

            sb.AppendLine($"LAYER {layerIndex}: {layer.name}");
            sb.AppendLine("───────────────────────────────────────────────────────────");
            sb.AppendLine($"  Weight: {layer.defaultWeight:F2} | Blending: {layer.blendingMode}");
            sb.AppendLine();

            AnalyzeStatesInLayer(sb, layer.stateMachine, layerIndex);

            sb.AppendLine("───────────────────────────────────────────────────────────");
            sb.AppendLine();
        }
    }

    private void AnalyzeStatesInLayer(StringBuilder sb, AnimatorStateMachine stateMachine, int layerIndex)
    {
        // Analyze states
        sb.AppendLine("  STATES:");
        sb.AppendLine("  ─────────────────────────────────────────────────────────");

        if (stateMachine.states.Length == 0)
        {
            sb.AppendLine("    No states found.");
        }
        else
        {
            foreach (var stateInfo in stateMachine.states)
            {
                var state = stateInfo.state;
                string stateType = state == stateMachine.defaultState ? " (DEFAULT)" : "";

                sb.AppendLine($"    • {state.name}{stateType}");

                // Motion info
                if (state.motion != null)
                {
                    sb.AppendLine($"      Motion: {state.motion.name}");
                }

                // Speed and other properties
                sb.AppendLine($"      Speed: {state.speed:F2} | Loop Time: {state.motion?.isLooping}");
            }
        }

        sb.AppendLine();

        // Analyze transitions
        AnalyzeTransitions(sb, stateMachine);

        // Analyze Any State transitions
        AnalyzeAnyStateTransitions(sb, stateMachine);
    }

    private void AnalyzeTransitions(StringBuilder sb, AnimatorStateMachine stateMachine)
    {
        sb.AppendLine("  STATE TRANSITIONS:");
        sb.AppendLine("  ─────────────────────────────────────────────────────────");

        bool hasTransitions = false;

        foreach (var stateInfo in stateMachine.states)
        {
            var state = stateInfo.state;

            if (state.transitions.Length > 0)
            {
                foreach (var transition in state.transitions)
                {
                    hasTransitions = true;
                    string destinationName = GetTransitionDestinationName(transition, stateMachine);

                    sb.AppendLine($"    {state.name} → {destinationName}");

                    // Transition properties
                    sb.AppendLine($"      Has Exit Time: {transition.hasExitTime}");
                    if (transition.hasExitTime)
                    {
                        sb.AppendLine($"      Exit Time: {transition.exitTime:F2}");
                    }
                    sb.AppendLine($"      Duration: {transition.duration:F2}");

                    // Conditions
                    if (transition.conditions.Length > 0)
                    {
                        sb.AppendLine("      Conditions:");
                        foreach (var condition in transition.conditions)
                        {
                            string conditionStr = FormatCondition(condition);
                            sb.AppendLine($"        • {conditionStr}");
                        }
                    }
                    else
                    {
                        sb.AppendLine("      Conditions: None");
                    }

                    sb.AppendLine();
                }
            }
        }

        if (!hasTransitions)
        {
            sb.AppendLine("    No state transitions found.");
        }

        sb.AppendLine();
    }

    private void AnalyzeAnyStateTransitions(StringBuilder sb, AnimatorStateMachine stateMachine)
    {
        if (stateMachine.anyStateTransitions.Length > 0)
        {
            sb.AppendLine("  ANY STATE TRANSITIONS:");
            sb.AppendLine("  ─────────────────────────────────────────────────────────");

            foreach (var transition in stateMachine.anyStateTransitions)
            {
                string destinationName = GetTransitionDestinationName(transition, stateMachine);

                sb.AppendLine($"    Any State → {destinationName}");

                // Transition properties
                sb.AppendLine($"      Has Exit Time: {transition.hasExitTime}");
                if (transition.hasExitTime)
                {
                    sb.AppendLine($"      Exit Time: {transition.exitTime:F2}");
                }
                sb.AppendLine($"      Duration: {transition.duration:F2}");

                // Conditions
                if (transition.conditions.Length > 0)
                {
                    sb.AppendLine("      Conditions:");
                    foreach (var condition in transition.conditions)
                    {
                        string conditionStr = FormatCondition(condition);
                        sb.AppendLine($"        • {conditionStr}");
                    }
                }
                else
                {
                    sb.AppendLine("      Conditions: None");
                }

                sb.AppendLine();
            }

            sb.AppendLine();
        }
    }

    private string GetParameterTypeString(AnimatorControllerParameterType type)
    {
        switch (type)
        {
            case AnimatorControllerParameterType.Bool: return "Bool";
            case AnimatorControllerParameterType.Trigger: return "Trigger";
            case AnimatorControllerParameterType.Int: return "Int";
            case AnimatorControllerParameterType.Float: return "Float";
            default: return "Unknown";
        }
    }

    private string GetParameterDefaultValue(AnimatorControllerParameter param)
    {
        switch (param.type)
        {
            case AnimatorControllerParameterType.Bool:
                return param.defaultBool.ToString();
            case AnimatorControllerParameterType.Trigger:
                return "false";
            case AnimatorControllerParameterType.Int:
                return param.defaultInt.ToString();
            case AnimatorControllerParameterType.Float:
                return param.defaultFloat.ToString("F2");
            default:
                return "N/A";
        }
    }

    private string GetTransitionDestinationName(AnimatorStateTransition transition, AnimatorStateMachine stateMachine)
    {
        if (transition.destinationState != null)
        {
            return transition.destinationState.name;
        }
        else if (transition.destinationStateMachine != null)
        {
            return transition.destinationStateMachine.name + " (StateMachine)";
        }
        else if (transition.isExit)
        {
            return "Exit";
        }
        else
        {
            return "Unknown";
        }
    }

    private string FormatCondition(AnimatorCondition condition)
    {
        string modeStr = "";
        switch (condition.mode)
        {
            case AnimatorConditionMode.If:
                modeStr = "== true";
                break;
            case AnimatorConditionMode.IfNot:
                modeStr = "== false";
                break;
            case AnimatorConditionMode.Greater:
                modeStr = $"> {condition.threshold:F2}";
                break;
            case AnimatorConditionMode.Less:
                modeStr = $"< {condition.threshold:F2}";
                break;
            case AnimatorConditionMode.Equals:
                modeStr = $"== {condition.threshold:F0}";
                break;
            case AnimatorConditionMode.NotEqual:
                modeStr = $"!= {condition.threshold:F0}";
                break;
        }

        return $"{condition.parameter} {modeStr}";
    }
}
