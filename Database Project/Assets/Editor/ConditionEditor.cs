using UnityEngine;
using UnityEditor;
using LitJson;
using GameUtilities;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class ConditionEditor : EditorWindow {
    private ConditionDatabase conditionDatabase;
    //private List<Condition> conditions = new List<Condition>();
    private string conditionAssetPath = "Assets/conddb.asset";

    private EditorState editorState;
    private Condition selectedCondition;
    enum EditorState { Home, Create, Edit }
    Vector2 listScrollPos;
    Vector2 editScrollPos;

    #region Condition Properties
    string conditionName;
    int conditionID;
    string conditionDesc;
    bool isHarmful;
    ConditionStat conditionStat;
    int conditionValue;
    bool hasDuration;
    double conditionDuration;
    #endregion


    // Add menu named "Condition Editor" to the Window menu
    [MenuItem("Window/Condition Editor")]
    static void Init() {
        // Get existing open window or if none, make a new one:
        ConditionEditor editor = (ConditionEditor)EditorWindow.GetWindow(typeof(ConditionEditor));
        editor.minSize = new Vector2(1000, 600);
        editor.Show();
    }

    void Awake() {
        LoadConditionDatabase();
    }

    void LoadConditionDatabase() {
        conditionDatabase = AssetDatabase.LoadAssetAtPath<ConditionDatabase>(conditionAssetPath);

        if (conditionDatabase == null)
            CreateConditionDatabase();
        else
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = conditionDatabase;
        }
    }

    void CreateConditionDatabase() {
        Debug.Log("Creating condition database...");

        conditionDatabase = ScriptableObject.CreateInstance<ConditionDatabase>();
        Debug.Log(conditionDatabase);

        AssetDatabase.CreateAsset(conditionDatabase, "Assets/conddb.asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = conditionDatabase;
    }

    void OnEnable() {

    }

    void OnGUI() {
        /*
         * Editor toolbar
         */
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create New Condition", GUILayout.Width(300)))
        {
            editorState = EditorState.Create;
            return;
        }
        if (GUILayout.Button("Reload Database", GUILayout.Width(300)))
        {
            conditionDatabase.ReloadDatabase();
            return;
        }
        if (GUILayout.Button("Save to JSON", GUILayout.Width(300)))
        {
            // Delete this item from the database.
            conditionDatabase.SaveDatabase();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        EditorGUILayout.EndHorizontal();

        if (conditionDatabase == null || conditionDatabase.Conditions == null)
        {
            EditorGUILayout.LabelField("The database may need reloading.");
            return;
        }

        EditorGUILayout.BeginHorizontal();
        // List all of the items on the left hand side.
        listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos, false, false, GUILayout.Width(450), GUILayout.MinHeight(550));
        foreach (Condition i in conditionDatabase.Conditions)
        {
            // Horizontal group per condition.
            EditorGUILayout.BeginHorizontal(GUILayout.Width(400.0f));

            if (GUILayout.Button("X", GUILayout.Width(50.0f)))
            {
                // Delete this item from the database.
                conditionDatabase.RemoveCondition(i);
                EditorUtility.SetDirty(conditionDatabase);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = conditionDatabase;
                return;
            }

            if (GUILayout.Button("C", GUILayout.Width(50.0f)))
            {
                // Duplicate this item.
                conditionDatabase.DuplicateCondition(i);
                EditorUtility.SetDirty(conditionDatabase);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = conditionDatabase;
                return;
            }

            if (GUILayout.Button(i.ConditionName.ToString(), GUILayout.Width(300)))
            {
                if (editorState == EditorState.Edit)
                    SaveExistingCondition();
                else if (editorState == EditorState.Create)
                    SaveNewCondition();

                //Get the new item and its associated data.
                selectedCondition = i;
                GetConditionData();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = conditionDatabase;

                editorState = EditorState.Edit;

                return;
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (editorState == EditorState.Create || editorState == EditorState.Edit)
            ShowCreateWindow();

        EditorGUILayout.EndHorizontal();




    }

    void ShowCreateWindow() {
        editScrollPos = EditorGUILayout.BeginScrollView(editScrollPos, false, false, GUILayout.MinWidth(540), GUILayout.MinHeight(550));

        conditionName = EditorGUILayout.TextField("Name: ", conditionName, GUILayout.Width(300));
        conditionID = EditorGUILayout.IntField("ID: ", conditionID, GUILayout.Width(300));
        conditionDesc = EditorGUILayout.TextField("Description: ", conditionDesc, GUILayout.Width(450));
        isHarmful = EditorGUILayout.Toggle("Harmful", isHarmful);
        conditionStat = (ConditionStat)EditorGUILayout.EnumPopup("Affected Stat: ", conditionStat, GUILayout.Width(450));
        conditionValue = EditorGUILayout.IntField("Value: ", conditionValue, GUILayout.Width(300));
        hasDuration = EditorGUILayout.Toggle("Timed", hasDuration);
        conditionDuration = EditorGUILayout.DoubleField("Duration: ", conditionDuration, GUILayout.Width(300));

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save", GUILayout.Width(150.0f)))
        {
            // Save this item to the database, either as a new item
            // or as an existing item.
            if (editorState == EditorState.Create)
                SaveNewCondition();
            else
                SaveExistingCondition();

            EditorUtility.SetDirty(conditionDatabase);
            editorState = EditorState.Home;
        }
        if (GUILayout.Button("Cancel", GUILayout.Width(150.0f)))
        {
            EditorUtility.SetDirty(conditionDatabase);
            editorState = EditorState.Home;
        }
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.EndScrollView();
    }

    void SaveNewCondition() {
        Condition newCondition = new Condition();

        newCondition.ConditionName = conditionName;
        newCondition.ConditionID = conditionID;
        newCondition.ConditionDesc = conditionDesc;
        newCondition.ConditionDuration = conditionDuration;
        newCondition.ConditionStat = conditionStat;
        newCondition.ConditionValue = conditionValue;
        newCondition.IsHarmful = isHarmful;
        newCondition.HasDuration = hasDuration;

        // Check that the given ID isn't already in the database.
        if (RequirementsMet(conditionID))
            conditionDatabase.AddCondition(newCondition);
        else
            Debug.LogError("An item with that ID (" + newCondition.ConditionID + ") already exists.");

    }

    void SaveExistingCondition() {
        // Check that the given ID isn't already in the database.
        if (!RequirementsMet(conditionID, selectedCondition))
        {
            Debug.LogError("An item with that ID (" + conditionID + ") already exists.");
            return;
        }

        selectedCondition.ConditionName = conditionName;
        selectedCondition.ConditionID = conditionID;
        selectedCondition.ConditionDesc = conditionDesc;
        selectedCondition.ConditionDuration = conditionDuration;
        selectedCondition.ConditionStat = conditionStat;
        selectedCondition.IsHarmful = isHarmful;
        selectedCondition.HasDuration = hasDuration;
    }

    void GetConditionData() {
        conditionName = EditorGUILayout.TextField("Name: ", selectedCondition.ConditionName);
        conditionID = EditorGUILayout.IntField("ID: ", selectedCondition.ConditionID);
        conditionDesc = EditorGUILayout.TextField("Description: ", selectedCondition.ConditionDesc);
        isHarmful = EditorGUILayout.Toggle("Harmful", selectedCondition.IsHarmful);
        conditionStat = (ConditionStat)EditorGUILayout.EnumPopup("Affected Stat: ", selectedCondition.ConditionStat);
        conditionValue = EditorGUILayout.IntField("Value: ", selectedCondition.ConditionValue);
        hasDuration = EditorGUILayout.Toggle("Timed", selectedCondition.HasDuration);
        conditionDuration = EditorGUILayout.DoubleField("Duration: ", selectedCondition.ConditionDuration);
    }

    // Check that the unique ID is not taken.
    bool RequirementsMet(int id) {
        if (id == -1)
            return true;

        foreach (Condition i in conditionDatabase.Conditions)
        {
            if (i.ConditionID == id)
                return false;
        }

        return true;
    }

    // Check whether the unique ID is taken BY ANOTHER ITEM...
    bool RequirementsMet(int id, Condition self) {
        if (id == -1 || self == null)
            return true;

        foreach (Condition i in conditionDatabase.Conditions)
        {
            if (i != self && i.ConditionID == id)
                return false;
        }

        return true;
    }
}
