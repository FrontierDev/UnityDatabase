using UnityEngine;
using GameUtilities;
using LitJson;
using System.IO;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ConditionDatabase : ScriptableObject {
    // The list which contains the actual items.
    [SerializeField]
    public List<Condition> Conditions { get; set; }

    // Holds item data that is pulled in from the JSON string
    JsonData conditionData;

    void Start() {
        ReloadDatabase();
    }

    public void ReloadDatabase() {
        Debug.Log("(Re)loading condition database...");

        if (Conditions == null)
            Conditions = new List<Condition>();

        conditionData = JsonMapper.ToObject(File.ReadAllText(Application.dataPath + "/StreamingAssets/Conditions.json"));

        if (conditionData == null)
            CreateJSONFile();

        CreateConditionDatabase();
    }

    void CreateJSONFile() {
        File.CreateText(Application.dataPath + "/StreamingAssets/Conditions.json");
        ReloadDatabase();
    }

    // Saves the database to a JSON file.
    public void SaveDatabase() {
        conditionData = JsonMapper.ToJson(this);
        File.WriteAllText(Application.dataPath + "/StreamingAssets/Conditions.json", conditionData.ToString());
    }

    // This extracts information from the JSON database (through conditionData)
    void CreateConditionDatabase() {
        for (int i = 0; i < conditionData["Conditions"].Count; i++)
        {
            if(!Contains((int)conditionData["Conditions"][i]["ConditionID"]))
            {
                Condition newCondition = new Condition();

                // Map each line in the ith JSON entry to a variable:
                newCondition.ConditionName = (string)conditionData["Conditions"][i]["ConditionName"];
                newCondition.ConditionID = (int)conditionData["Conditions"][i]["ConditionID"];
                newCondition.ConditionDesc = (string)conditionData["Conditions"][i]["ConditionDesc"];
                newCondition.ConditionDuration = (double)conditionData["Conditions"][i]["ConditionDuration"];
                newCondition.ConditionStat = (ConditionStat)((int)conditionData["Conditions"][i]["ConditionStat"]);
                newCondition.ConditionValue = (int)conditionData["Conditions"][i]["ConditionValue"];
                newCondition.IsHarmful = (bool)conditionData["Conditions"][i]["IsHarmful"];
                newCondition.HasDuration = (bool)conditionData["Conditions"][i]["HasDuration"];

                // Add this condition to the database.
                AddCondition(newCondition);

                Debug.Log("(CondDB) " + newCondition.ConditionName + " loaded.");
            }
        }
    }

    public void AddCondition(Condition i) {
        Conditions.Add(i);
    }

    public void DuplicateCondition(Condition i) {
        Condition duplicate = new Condition(i);

        duplicate.ConditionName = duplicate.ConditionName + "copy";
        while (Contains(duplicate.ConditionID))
        {
            duplicate.ConditionID++;
        }

        AddCondition(duplicate);
    }

    public void RemoveCondition(Condition i) {
        Conditions.Remove(i);
    }

    // Get Item reference by ID
    public Condition Condition(int id) {
        foreach (Condition i in Conditions)
        {
            if (i.ConditionID == id)
            {
                return i;
            }
        }

        Debug.LogError("Condition with ID " + id + " does not exist in the database.");
        return null;
    }

    public bool Contains(int id) {
        foreach (Condition i in Conditions)
        {
            if (i.ConditionID == id)
            {
                return true;
            }
        }


        return false;
    }
}
