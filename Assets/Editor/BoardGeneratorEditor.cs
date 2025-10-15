#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoardGenerator))]
public class BoardGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BoardGenerator generator = (BoardGenerator)target;

        GUILayout.Space(8);
        EditorGUILayout.HelpBox("Use the buttons below to generate or clear the board in the Editor. Generation does not run automatically at Play start.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Board (Editor)"))
        {
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Generate Board");
            generator.GenerateBoard();
            // mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }

        if (GUILayout.Button("Clear Board"))
        {
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Clear Board");
            generator.ClearBoard();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }
        EditorGUILayout.EndHorizontal();
    }
}
#endif
