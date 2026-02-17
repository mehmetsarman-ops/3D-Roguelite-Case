using UnityEditor;
using UnityEngine;
using RogueliteGame.Enemy;

[CustomEditor(typeof(EnemySpawner))]
public class EnemySpawnerEditor : Editor
{
    private SerializedProperty minEnemyCountProp;
    private SerializedProperty respawnDelayProp;
    private SerializedProperty initialSpawnDelayProp;
    private SerializedProperty mapSizeProp;
    private SerializedProperty spawnYProp;
    private SerializedProperty level1CountProp;
    private SerializedProperty level2CountProp;
    private SerializedProperty level3CountProp;
    private SerializedProperty level1PrefabProp;
    private SerializedProperty level2PrefabProp;
    private SerializedProperty level3PrefabProp;
    private SerializedProperty playerTransformProp;

    private void OnEnable()
    {
        minEnemyCountProp = serializedObject.FindProperty("minEnemyCount");
        respawnDelayProp = serializedObject.FindProperty("respawnDelay");
        initialSpawnDelayProp = serializedObject.FindProperty("initialSpawnDelay");
        mapSizeProp = serializedObject.FindProperty("mapSize");
        spawnYProp = serializedObject.FindProperty("spawnY");
        level1CountProp = serializedObject.FindProperty("level1Count");
        level2CountProp = serializedObject.FindProperty("level2Count");
        level3CountProp = serializedObject.FindProperty("level3Count");
        level1PrefabProp = serializedObject.FindProperty("level1Prefab");
        level2PrefabProp = serializedObject.FindProperty("level2Prefab");
        level3PrefabProp = serializedObject.FindProperty("level3Prefab");
        playerTransformProp = serializedObject.FindProperty("playerTransform");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Spawn Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(minEnemyCountProp);
        EditorGUILayout.PropertyField(respawnDelayProp);
        EditorGUILayout.PropertyField(initialSpawnDelayProp);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Map Bounds", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(mapSizeProp);
        EditorGUILayout.PropertyField(spawnYProp);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Level Distribution", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(level1CountProp);
        EditorGUILayout.PropertyField(level2CountProp);
        EditorGUILayout.PropertyField(level3CountProp);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Enemy Prefabs", EditorStyles.boldLabel);
        DrawEnemyPrefabField(level1PrefabProp, "Level 1 Prefab");
        DrawEnemyPrefabField(level2PrefabProp, "Level 2 Prefab");
        DrawEnemyPrefabField(level3PrefabProp, "Level 3 Prefab");

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(playerTransformProp);

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawEnemyPrefabField(SerializedProperty property, string label)
    {
        var current = property.objectReferenceValue as Enemy;
        var picked = (Enemy)EditorGUILayout.ObjectField(
            new GUIContent(label),
            current,
            typeof(Enemy),
            false);
        property.objectReferenceValue = picked;
    }
}
