/*
 * CURRENTLY HAS NO FUNCTIONALITY!!
 */ 

using UnityEngine;
using GameUtilities;
using System.Collections;

[System.Serializable]
public class Condition {
    // Condition name
    public string ConditionName { get; set; }
    // Condition ID
    public int ConditionID { get; set; }
    // Condition description
    public string ConditionDesc { get; set; }
    // Harmful flag?
    public bool IsHarmful { get; set; }
    // Which actor stat does the condition effect?
    public ConditionStat ConditionStat { get; set; }
    public int ConditionValue { get; set; }
    // Duration, if any.
    public bool HasDuration { get; set; }
    public double ConditionDuration { get; set; }

    // Constructor used for duplication
    public Condition(Condition condition) {
        this.ConditionName = condition.ConditionName;
        this.ConditionID = condition.ConditionID;
        this.ConditionDesc = condition.ConditionDesc;
        this.IsHarmful = condition.IsHarmful;
        this.ConditionStat = condition.ConditionStat;
        this.ConditionValue = condition.ConditionValue;
        this.ConditionDuration = condition.ConditionDuration;
    }

    // Empty constructor
    public Condition() {

    }
}
