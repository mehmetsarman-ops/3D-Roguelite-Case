using UnityEditor;
using UnityEngine;
using RogueliteGame.Enemy;

[CustomEditor(typeof(Enemy))]
public class EnemyEditor : Editor
{
    private SerializedProperty levelProp;
    private SerializedProperty maxHealthProp;

    private SerializedProperty wanderSpeedProp;
    private SerializedProperty wanderRadiusProp;
    private SerializedProperty wanderPauseTimeProp;

    private SerializedProperty chaseSpeedProp;
    private SerializedProperty attackRangeProp;
    private SerializedProperty attackDamageProp;
    private SerializedProperty attackCooldownProp;

    private SerializedProperty healthBarYOffsetProp;

    private void OnEnable()
    {
        levelProp = serializedObject.FindProperty("level");
        maxHealthProp = serializedObject.FindProperty("maxHealth");

        wanderSpeedProp = serializedObject.FindProperty("wanderSpeed");
        wanderRadiusProp = serializedObject.FindProperty("wanderRadius");
        wanderPauseTimeProp = serializedObject.FindProperty("wanderPauseTime");

        chaseSpeedProp = serializedObject.FindProperty("chaseSpeed");
        attackRangeProp = serializedObject.FindProperty("attackRange");
        attackDamageProp = serializedObject.FindProperty("attackDamage");
        attackCooldownProp = serializedObject.FindProperty("attackCooldown");

        healthBarYOffsetProp = serializedObject.FindProperty("healthBarYOffset");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Enemy Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(levelProp);
        EditorGUILayout.PropertyField(maxHealthProp);

        EnemyLevel selectedLevel = (EnemyLevel)levelProp.intValue;
        EditorGUILayout.Space(6);

        // Seçilen seviyeye göre sadece ilgili alanları gösterdim, gereksiz karmaşıklık olmasın diye
        switch (selectedLevel)
        {
            case EnemyLevel.Level1_Static:
                EditorGUILayout.HelpBox("Level 1: Sabit düşman — sadece hasar alır.", MessageType.Info);
                break;

            case EnemyLevel.Level2_Wanderer:
                EditorGUILayout.LabelField("Level 2 — Wandering", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(wanderSpeedProp);
                EditorGUILayout.PropertyField(wanderRadiusProp);
                EditorGUILayout.PropertyField(wanderPauseTimeProp);
                break;

            case EnemyLevel.Level3_Chaser:
                EditorGUILayout.LabelField("Level 3 — Chase & Attack", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(chaseSpeedProp);
                EditorGUILayout.PropertyField(attackRangeProp);
                EditorGUILayout.PropertyField(attackDamageProp);
                EditorGUILayout.PropertyField(attackCooldownProp);
                break;
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Health Bar", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(healthBarYOffsetProp);

        serializedObject.ApplyModifiedProperties();
    }
}
