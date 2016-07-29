using UnityEngine;
using UnityEditor;
using LitJson;
using GameUtilities;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class ItemEditor : EditorWindow {
    private ItemDatabase itemDatabase;
    //private List<Item> items = new List<Item>();
    private string itemAssetPath = "Assets/itemdb.asset";

    #region Auxillary Databases
    private ConditionDatabase _conditionDatabase;
    private string conditionPath = "Assets/conddb.asset";
    private PerkDatabase _perkDatabase;
    private string perkAssetPath = "Assets/perkdb.asset";
    #endregion

    private Item selectedItem;
    private EditorState editorState;
    enum EditorState { Home, Create, Edit }
    private ItemType filterType;
    bool filterTypeToggle;
    Vector2 listScrollPos;
    Vector2 editScrollPos;
    Vector2 addedPerkScrollPos;
    Vector2 perkScrollPos;
    Vector2 addedConditionScrollPos;
    Vector2 conditionScrollPos;
    bool showContentsOrConditions;
    bool showRequirements;

    #region Item Properties
    string itemName = "";
    int itemID = -1;
    string itemShortDesc = "";
    string itemLongDesc = "";
    ItemQuality itemQuality = ItemQuality.Quality1;
    int itemWeight = 0;
    int itemCost = 0;
    Texture2D itemIcon = Texture2D.whiteTexture;
    string itemIconPath = "";
    GameObject itemModel = null;
    string itemModelPath = "";
    ItemType itemType = ItemType.Weapon;
    int newPerkID = 0;
    List<int> itemReqPerkIDs = new List<int>();
    #endregion
    #region Weapon Properties
    int baseDamage = 0;
    int attackSpeed = 1;
    int bluntDamage = 0;
    int pierceDamage = 0;
    int slashDamage = 0;
    #endregion
    #region Armour Properties
    int baseDefence;
    int bluntDefence;
    int pierceDefence;
    int slashDefence;
    int natureDefence;
    int thermalDefence;
    ArmourType armourType;
    ArmourMaterial armourMaterial;
    #endregion
    #region Consumable Properties
    ConsumableType consumableType;
    List<int> conditions = new List<int>();
    int newConditionID;
    int consumableCharges;
    #endregion
    #region Container Properties
    int contentItemID;
    int contentQuantity;
    List<int> contentItems = new List<int>();
    List<int> contentQuantities = new List<int>();
    #endregion
    #region Ingredient Properties
    bool isStackable;
    #endregion

    // Add menu named "Item Editor" to the Window menu
    [MenuItem("Window/Item Editor")]
    static void Init() {
        // Get existing open window or if none, make a new one:
        ItemEditor editor = (ItemEditor)EditorWindow.GetWindow(typeof(ItemEditor));
        editor.minSize = new Vector2(1000, 600);
        editor.Show();
    }

    void Awake() {
        LoadItemDatabase();
        LoadAuxillaryDatabases();
    }

    void LoadItemDatabase() {
        //Debug.Log(itemAssetPath);

        itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabase>(itemAssetPath);

        if (itemDatabase.Items == null)
            itemDatabase.ReloadDatabase();

        if (itemDatabase == null)
            CreateItemDatabase();
        else
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = itemDatabase;
        }
    }

    void LoadAuxillaryDatabases() {
        // CONDITION database
        _conditionDatabase = AssetDatabase.LoadAssetAtPath<ConditionDatabase>(conditionPath);
        if (_conditionDatabase.Conditions == null)
            _conditionDatabase.ReloadDatabase();

        // PERK database
        _perkDatabase = AssetDatabase.LoadAssetAtPath<PerkDatabase>(perkAssetPath);
        _perkDatabase.ReloadDatabase();
    }

    void CreateItemDatabase() {
        Debug.Log("Creating item database...");

        itemDatabase = ScriptableObject.CreateInstance<ItemDatabase>();

        AssetDatabase.CreateAsset(itemDatabase, itemAssetPath);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = itemDatabase;
    }

    void OnEnable() {
        
    }

    void OnGUI() {
        /*
         * Editor toolbar.
         */
        // Button row.
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create New Item", GUILayout.Width(300.0f)))
        {
            // Create a new item...
            editorState = EditorState.Create;
            return;
        }
        if (GUILayout.Button("Reload Database", GUILayout.Width(300.0f)))
        {
            itemDatabase.ReloadDatabase();
            _conditionDatabase.ReloadDatabase();
            _perkDatabase.ReloadDatabase();
            return;
        }
        if (GUILayout.Button("Save to JSON", GUILayout.Width(300)))
        {
            // Delete this item from the database.
            itemDatabase.SaveDatabase();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        EditorGUILayout.EndHorizontal();

        // Filter row.
        EditorGUILayout.BeginHorizontal();
        filterType = (ItemType)EditorGUILayout.EnumPopup("Show Type: ", filterType);
        filterTypeToggle = EditorGUILayout.Toggle(filterTypeToggle);
        EditorGUILayout.EndHorizontal();

        // Kill the script before a null pointer exception is thrown (ugh why).
        if (itemDatabase == null || itemDatabase.Items == null)
        {
            EditorGUILayout.LabelField("The database may need reloading.");
            return;
        }

        EditorGUILayout.BeginHorizontal();
        // List all of the items on the left hand side.
        listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos, false, false, GUILayout.Width(450), GUILayout.MinHeight(550));
        foreach (Item i in itemDatabase.Items)
        {
            // Filter if necessary.
            if(filterTypeToggle)
            {
                if(i.ItemType != filterType)
                {
                    continue;
                }
            }

            // Horizontal group per item.
            EditorGUILayout.BeginHorizontal(GUILayout.Width(400));

            if (GUILayout.Button("X", GUILayout.Width(50.0f)))
            {
                // Delete this item from the database.
                itemDatabase.RemoveItem(i);
                EditorUtility.SetDirty(itemDatabase);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = itemDatabase;
                return;
            }

            if (GUILayout.Button("C", GUILayout.Width(50.0f)))
            {
                // Duplicate this item.
                itemDatabase.DuplicateItem(i);
                EditorUtility.SetDirty(itemDatabase);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = itemDatabase;
                return;
            }

            if (GUILayout.Button(i.ItemName.ToString(), GUILayout.Width(300)))
            {
                if (editorState == EditorState.Edit)
                    SaveExistingItem(selectedItem.ItemType);
                else if (editorState == EditorState.Create)
                    SaveNewItem(itemType);

                //Get the new item and its associated data.
                selectedItem = i;
                GetItemData();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = itemDatabase;

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

        itemName = EditorGUILayout.TextField("Name: ", itemName, GUILayout.Width(300));
        itemID = EditorGUILayout.IntField("ID: ", itemID, GUILayout.Width(300));
        itemShortDesc = EditorGUILayout.TextField("Short Desc.: ", itemShortDesc, GUILayout.Width(450));
        EditorGUILayout.LabelField("Long description:");
        itemLongDesc = EditorGUILayout.TextArea(itemLongDesc, GUILayout.Width (450), GUILayout.MinHeight(100));
        itemWeight = EditorGUILayout.IntField("Weight: ", itemWeight, GUILayout.Width(300));
        itemCost = EditorGUILayout.IntField("Cost: ", itemCost, GUILayout.Width(300));
        itemIcon = EditorGUILayout.ObjectField("Icon: ", itemIcon, typeof(Texture2D), true, GUILayout.Width(450)) as Texture2D;
        itemModel = EditorGUILayout.ObjectField("Model: ", itemModel, typeof(GameObject), true, GUILayout.Width(450)) as GameObject;
        itemType = (ItemType)EditorGUILayout.EnumPopup("Type: ", itemType, GUILayout.Width(480));

        EditorGUILayout.Space();

        switch(itemType)
        {
            case (ItemType.Weapon):
                EditorGUILayout.LabelField("Weapon-specific Attributes", EditorStyles.boldLabel);
                baseDamage = EditorGUILayout.IntField("Base Damage: ", baseDamage, GUILayout.Width(300));
                attackSpeed = EditorGUILayout.IntField("Attack Speed: ", attackSpeed, GUILayout.Width(300));
                bluntDamage = EditorGUILayout.IntField("Blunt", bluntDamage, GUILayout.Width(300));
                pierceDamage = EditorGUILayout.IntField("Pierce", pierceDamage, GUILayout.Width(300));
                slashDamage = EditorGUILayout.IntField("Slash", slashDamage, GUILayout.Width(300));

                EditorGUILayout.Space();

                showContentsOrConditions = EditorGUILayout.Foldout(showContentsOrConditions, "ON-EQUIP Conditions");
                if(showContentsOrConditions)
                    DisplayConditions();

                break;

            case (ItemType.Armour):
                EditorGUILayout.LabelField("Armour-specific Attributes", EditorStyles.boldLabel);
                baseDefence = EditorGUILayout.IntField("Base Defence: ", baseDefence, GUILayout.Width(300));
                bluntDefence = EditorGUILayout.IntField("Blunt", bluntDefence, GUILayout.Width(300));
                pierceDefence = EditorGUILayout.IntField("Pierce", pierceDefence, GUILayout.Width(300));
                slashDefence = EditorGUILayout.IntField("Slash", slashDefence, GUILayout.Width(300));
                natureDefence = EditorGUILayout.IntField("Nature", natureDefence, GUILayout.Width(300));
                thermalDefence = EditorGUILayout.IntField("Thermal", thermalDefence, GUILayout.Width(300));
                armourType = (ArmourType)EditorGUILayout.EnumPopup("Slot: ", armourType, GUILayout.Width(450));
                armourMaterial = (ArmourMaterial)EditorGUILayout.EnumPopup("Material: ", armourMaterial, GUILayout.Width(450));

                EditorGUILayout.Space();

                showContentsOrConditions = EditorGUILayout.Foldout(showContentsOrConditions, "ON-EQUIP Conditions");
                if (showContentsOrConditions)
                    DisplayConditions();

                break;

            case (ItemType.Consumable):
                EditorGUILayout.LabelField("Consumable-specific Attributes", EditorStyles.boldLabel);
                consumableType = (ConsumableType)EditorGUILayout.EnumPopup("Type: ", consumableType, GUILayout.Width(450));
                consumableCharges = EditorGUILayout.IntField("Uses: ", consumableCharges, GUILayout.Width(300));

                EditorGUILayout.Space();

                showContentsOrConditions = EditorGUILayout.Foldout(showContentsOrConditions, "ON-USE Conditions");
                if (showContentsOrConditions)
                    DisplayConditions();

                break;

            case (ItemType.Container):
                EditorGUILayout.LabelField("Container-specific Attributes", EditorStyles.boldLabel);
                showContentsOrConditions = EditorGUILayout.Foldout(showContentsOrConditions, "CONTAINER CONTENTS");
                if (showContentsOrConditions)
                    DisplayContainerEditor();

                break;

            case (ItemType.Ingredient):
                EditorGUILayout.LabelField("Ingredient-specific Attributes", EditorStyles.boldLabel);
                isStackable = EditorGUILayout.Toggle("Stackable: ", isStackable);
                break;

            case (ItemType.Misc):
                EditorGUILayout.LabelField("Miscellaneous-specific Attributes", EditorStyles.boldLabel);
                isStackable = EditorGUILayout.Toggle("Stackable: ", isStackable);
                break;
        }

        EditorGUILayout.Space();

        showRequirements = EditorGUILayout.Foldout(showRequirements, "REQUIRED PERKS");
        if (showRequirements)
            DisplayRequirements();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save", GUILayout.Width(150.0f)))
        {
            // Save this item to the database, either as a new item
            // or as an existing item.
            if (editorState == EditorState.Create)
                SaveNewItem(itemType);
            else
                SaveExistingItem(itemType);

            EditorUtility.SetDirty(itemDatabase);
            editorState = EditorState.Home;
        }
        if (GUILayout.Button("Cancel", GUILayout.Width(150.0f)))
        {
            EditorUtility.SetDirty(itemDatabase);
            editorState = EditorState.Home;
        }
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.EndScrollView();
    }

    void SaveNewItem(ItemType type) {
        Item newItem = new Item();

        // Using the empty constructor so that more variables can be added easily.
        newItem.ItemName = itemName;
        newItem.ItemID = itemID;
        newItem.ItemShortDesc = itemShortDesc;
        newItem.ItemLongDesc = itemLongDesc;
        newItem.ItemWeight = itemWeight;
        newItem.ItemCost = itemCost;
        newItem.ItemQuality = itemQuality;
        newItem.ItemType = itemType;
        newItem.ItemPerkReqIDs = itemReqPerkIDs;

        // Set the model and icon
        newItem.SetIcon(itemIcon);
        newItem.SetModel(itemModel);

        // Find model path
        itemModelPath = AssetDatabase.GetAssetPath(itemModel);
        itemModelPath = GameUtility.CleanItemResourcePath(itemModelPath, "Assets/Resources/");
        itemModelPath = GameUtility.CleanItemResourcePath(itemModelPath, ".prefab");
        newItem.ItemModelPath = itemModelPath;

        // Find icon path
        itemIconPath = AssetDatabase.GetAssetPath(itemIcon);
        itemIconPath = GameUtility.CleanItemResourcePath(itemIconPath, "Assets/Resources/");
        itemIconPath = GameUtility.CleanItemResourcePath(itemIconPath, ".png");
        newItem.ItemIconPath = itemIconPath;

        switch (itemType)
        {
            case (ItemType.Weapon):
                WeaponStats wstats = new WeaponStats();
                wstats.AttackSpeed = attackSpeed;
                wstats.BaseDamage = baseDamage;
                wstats.BluntDamage = bluntDamage;
                wstats.PierceDamage = pierceDamage;
                wstats.SlashDamage = slashDamage;
                wstats.Conditions = conditions;

                newItem.WStats = wstats;
                break;

            case (ItemType.Armour):
                ArmourStats astats = new ArmourStats();
                astats.ArmourMaterial = armourMaterial;
                astats.ArmourType = armourType;
                astats.BaseDefence = baseDefence;
                astats.BluntDefence = bluntDefence;
                astats.PierceDefence = pierceDefence;
                astats.SlashDefence = slashDefence;
                astats.NatureDefence = natureDefence;
                astats.ThermalDefence = thermalDefence;
                astats.Conditions = conditions;

                newItem.AStats = astats;
                break;

            case (ItemType.Consumable):
                ConsumableStats constats = new ConsumableStats();
                constats.ConsumableType = consumableType;
                constats.Conditions = conditions;
                constats.Charges = consumableCharges;

                newItem.ConStats = constats;
                break;

            case (ItemType.Container):
                ContainerStats ctnstats = new ContainerStats();
                ctnstats.ContentItems = contentItems;
                ctnstats.ContentQuantities = contentQuantities;

                newItem.CtnStats = ctnstats;
                break;

            case (ItemType.Ingredient):
                IngredientStats ingstats = new IngredientStats();
                ingstats.IsStackable = isStackable;

                newItem.IngStats = ingstats;
                break;

            case (ItemType.Misc):
                MiscStats miscstats = new MiscStats();
                miscstats.IsStackable = isStackable;

                newItem.MiscStats = miscstats;
                break;
        }

        if (RequiremetsMet(newItem))
            itemDatabase.AddItem(newItem);
        else
            Debug.LogError("An item with that ID (" + newItem.ItemID + ") already exists.");

        EditorUtility.SetDirty(itemDatabase);
        AssetDatabase.SaveAssets();
    }

    void SaveExistingItem(ItemType type) {
        //Item newItem = new Item();

        if (!RequiremetsMet(itemID, selectedItem))
        {
            Debug.LogError("An item with that ID (" + itemID + ") already exists.");
            return;
        }
            

        // Using the empty constructor so that more variables can be added easily.
        selectedItem.ItemName = itemName;
        selectedItem.ItemID = itemID;
        selectedItem.ItemShortDesc = itemShortDesc;
        selectedItem.ItemLongDesc = itemLongDesc;
        selectedItem.ItemWeight = itemWeight;
        selectedItem.ItemCost = itemCost;
        selectedItem.ItemQuality = itemQuality;
        selectedItem.ItemType = itemType;
        selectedItem.ItemPerkReqIDs = itemReqPerkIDs;

        // Set the model and icon
        selectedItem.SetIcon(itemIcon);
        selectedItem.SetModel(itemModel);

        // Find model path
        itemModelPath = AssetDatabase.GetAssetPath(itemModel);
        itemModelPath = GameUtility.CleanItemResourcePath(itemModelPath, "Assets/Resources/");
        itemModelPath = GameUtility.CleanItemResourcePath(itemModelPath, ".prefab");
        selectedItem.ItemModelPath = itemModelPath;

        // Find icon path
        itemIconPath = AssetDatabase.GetAssetPath(itemIcon);
        itemIconPath = GameUtility.CleanItemResourcePath(itemIconPath, "Assets/Resources/");
        itemIconPath = GameUtility.CleanItemResourcePath(itemIconPath, ".png");
        selectedItem.ItemIconPath = itemIconPath;

        switch (type)
        {
            case (ItemType.Weapon):
                WeaponStats wstats = selectedItem.WStats;
                wstats.AttackSpeed = attackSpeed;
                wstats.BaseDamage = baseDamage;
                wstats.BluntDamage = bluntDamage;
                wstats.PierceDamage = pierceDamage;
                wstats.SlashDamage = slashDamage;
                wstats.Conditions = conditions;

                selectedItem.WStats = wstats;
                break;

            case (ItemType.Armour):
                ArmourStats astats = selectedItem.AStats;
                astats.ArmourMaterial = armourMaterial;
                astats.ArmourType = armourType;
                astats.BaseDefence = baseDefence;
                astats.BluntDefence = bluntDefence;
                astats.PierceDefence = pierceDefence;
                astats.SlashDefence = slashDefence;
                astats.NatureDefence = natureDefence;
                astats.ThermalDefence = thermalDefence;
                astats.Conditions = conditions;

                selectedItem.AStats = astats;
                break;

            case (ItemType.Consumable):
                ConsumableStats constats = selectedItem.ConStats;
                constats.ConsumableType = consumableType;
                constats.Conditions = conditions;
                constats.Charges = consumableCharges;

                selectedItem.ConStats = constats;
                break;

            case (ItemType.Container):
                ContainerStats ctnstats = selectedItem.CtnStats;
                ctnstats.ContentItems = contentItems;
                ctnstats.ContentQuantities = contentQuantities;

                selectedItem.CtnStats = ctnstats;
                break;

            case (ItemType.Ingredient):
                IngredientStats ingstats = selectedItem.IngStats;
                ingstats.IsStackable = isStackable;

                selectedItem.IngStats = ingstats;
                break;

            case (ItemType.Misc):
                MiscStats miscstats = selectedItem.MiscStats;
                miscstats.IsStackable = isStackable;

                selectedItem.MiscStats = miscstats;
                break;
        }

        EditorUtility.SetDirty(itemDatabase);
        AssetDatabase.SaveAssets();
    }

    void GetItemData() {
        //Debug.LogWarning(selectedItem.ItemType);

        itemName = EditorGUILayout.TextField("Name: ", selectedItem.ItemName, GUILayout.Width(300));
        itemID = EditorGUILayout.IntField("ID: ", selectedItem.ItemID, GUILayout.Width(300));
        itemShortDesc = EditorGUILayout.TextField("Short Desc.: ", selectedItem.ItemShortDesc, GUILayout.Width(500));
        EditorGUILayout.LabelField("Long description:");
        itemLongDesc = EditorGUILayout.TextArea(selectedItem.ItemLongDesc, GUILayout.MinHeight(100));
        itemWeight = EditorGUILayout.IntField("Weight: ", selectedItem.ItemWeight, GUILayout.Width(300));
        itemCost = EditorGUILayout.IntField("Cost: ", selectedItem.ItemCost, GUILayout.Width(300));
        itemIcon = EditorGUILayout.ObjectField("Icon: ", selectedItem.GetIcon(), typeof(Texture2D), true) as Texture2D;
        itemModel = EditorGUILayout.ObjectField("Model: ", selectedItem.GetModel(), typeof(GameObject), true) as GameObject;
        itemType = (ItemType)EditorGUILayout.EnumPopup("Type: ", selectedItem.ItemType);
        itemReqPerkIDs = selectedItem.ItemPerkReqIDs;
        CheckPerks();

        switch (selectedItem.ItemType)
        {
            case (ItemType.Weapon):
                baseDamage = EditorGUILayout.IntField("Base Damage: ", selectedItem.WStats.BaseDamage);
                attackSpeed = EditorGUILayout.IntField("Attack Speed: ", selectedItem.WStats.AttackSpeed);
                bluntDamage = EditorGUILayout.IntField("Blunt", selectedItem.WStats.BluntDamage);
                pierceDamage = EditorGUILayout.IntField("Pierce", selectedItem.WStats.PierceDamage);
                slashDamage = EditorGUILayout.IntField("Slash", selectedItem.WStats.SlashDamage);

                conditions = selectedItem.WStats.Conditions;
                CheckConditions();
                break;

            case (ItemType.Armour):
                baseDefence = EditorGUILayout.IntField("Base Defence: ", selectedItem.AStats.BaseDefence);
                bluntDefence = EditorGUILayout.IntField("Blunt", selectedItem.AStats.BluntDefence);
                pierceDefence = EditorGUILayout.IntField("Pierce", selectedItem.AStats.PierceDefence);
                slashDefence = EditorGUILayout.IntField("Slash", selectedItem.AStats.SlashDefence);
                natureDefence = EditorGUILayout.IntField("Nature", selectedItem.AStats.NatureDefence);
                thermalDefence = EditorGUILayout.IntField("Thermal", selectedItem.AStats.ThermalDefence);
                armourType = (ArmourType)EditorGUILayout.EnumPopup("Slot: ", selectedItem.AStats.ArmourType);
                armourMaterial = (ArmourMaterial)EditorGUILayout.EnumPopup("Material: ", selectedItem.AStats.ArmourMaterial);

                conditions = selectedItem.AStats.Conditions;
                CheckConditions();
                break;

            case (ItemType.Consumable):
                consumableType = (ConsumableType)EditorGUILayout.EnumPopup("Type: ", selectedItem.ConStats.ConsumableType);
                consumableCharges = EditorGUILayout.IntField("Charges", selectedItem.ConStats.Charges);

                conditions = selectedItem.ConStats.Conditions;
                CheckConditions();
                break;

            case (ItemType.Container):
                contentItems = selectedItem.CtnStats.ContentItems;
                contentQuantities = selectedItem.CtnStats.ContentQuantities;
                CheckContents();
                break;

            case (ItemType.Ingredient):
                isStackable = selectedItem.IngStats.IsStackable;
                break;

            case (ItemType.Misc):
                isStackable = selectedItem.MiscStats.IsStackable;
                break;
        }
    }

    void DisplayRequirements() {
        EditorGUILayout.LabelField("Assigned Perks", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        // Display the currently-added perks
        addedPerkScrollPos = EditorGUILayout.BeginScrollView(addedPerkScrollPos, false, false, GUILayout.Width(260), GUILayout.Height(180));
        for (int i = 0; i < itemReqPerkIDs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("X", GUILayout.Width(50)))
            {
                itemReqPerkIDs.RemoveAt(i);
                break;
            }
            EditorGUILayout.LabelField(_perkDatabase.perk(itemReqPerkIDs[i]).PerkName);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        // Search for perks in the perk database
        perkScrollPos = EditorGUILayout.BeginScrollView(perkScrollPos, false, false, GUILayout.Width(260), GUILayout.Height(180));
        for (int j = 0; j < _perkDatabase.Perks.Count; j++)
        {
            if (itemReqPerkIDs.Contains(_perkDatabase.Perks[j].PerkID))
                continue;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+", GUILayout.Width(50)))
            {
                newPerkID = _perkDatabase.Perks[j].PerkID;

                if (PerkRequirementsMet())
                {
                    itemReqPerkIDs.Add(newPerkID);
                    SortPerklist();
                }

            }
            EditorGUILayout.LabelField(_perkDatabase.Perks[j].PerkName + " (ID " + _perkDatabase.Perks[j].PerkID + ")");
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Display a box which allows the user to add new conditions by their IDs
        newPerkID = EditorGUILayout.IntField("Perk ID: ", newPerkID, GUILayout.Width(300));
        if (GUILayout.Button("Add New Perk", GUILayout.Width(300)))
        {
            if (PerkRequirementsMet())
            {
                itemReqPerkIDs.Add(newPerkID);
                SortPerklist();
            }
        }
    }

    bool PerkRequirementsMet() {
        if (!_perkDatabase.Contains(newPerkID))
        {
            Debug.LogError("Perk with ID " + newPerkID + " does not exist in the database.");
            return false;
        }

        if (itemReqPerkIDs.Contains(newPerkID))
        {
            Debug.LogError("This perk has already been added.");
            return false;
        }

        return true;
    }

    void CheckPerks() {
        List<int> indexToRemove = new List<int>();
        for (int i = 0; i < itemReqPerkIDs.Count; i++)
        {
            if (!_perkDatabase.Contains(itemReqPerkIDs[i]))
            {
                indexToRemove.Add(i);
            }
        }

        if (indexToRemove.Count == 0)
            return;

        for (int j = 0; j < indexToRemove.Count; j++)
        {
            itemReqPerkIDs.RemoveAt(j);
            Debug.LogWarning("A perk was removed from this entry as it no longer exists in the perks database.");
        }
        EditorUtility.SetDirty(itemDatabase);
        AssetDatabase.SaveAssets();
    }

    void DisplayConditions() {
        EditorGUILayout.LabelField("Assigned Conditions", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        // Display the currently-added conditions
        addedConditionScrollPos = EditorGUILayout.BeginScrollView(addedConditionScrollPos, false, false, GUILayout.Width(260), GUILayout.Height(180));
        for (int i = 0; i < conditions.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("X", GUILayout.Width(50)))
            {
                conditions.RemoveAt(i);
                break;
            }
            EditorGUILayout.LabelField(_conditionDatabase.Condition(conditions[i]).ConditionName);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        // Search for perks in the perk database
        conditionScrollPos = EditorGUILayout.BeginScrollView(conditionScrollPos, false, false, GUILayout.Width(260), GUILayout.Height(180));
        for (int j = 0; j < _conditionDatabase.Conditions.Count; j++)
        {
            if (conditions.Contains(_conditionDatabase.Conditions[j].ConditionID))
                continue;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+", GUILayout.Width(50)))
            {
                newConditionID = _conditionDatabase.Conditions[j].ConditionID;

                if (ConditionRequirementsMet())
                {
                    conditions.Add(newConditionID);
                    SortConditionList();
                }

            }
            EditorGUILayout.LabelField(_conditionDatabase.Conditions[j].ConditionName + " (ID " + _conditionDatabase.Conditions[j].ConditionID + ")");
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Display a box which allows the user to add new conditions by their IDs
        newConditionID = EditorGUILayout.IntField("Condition ID: ", newConditionID, GUILayout.Width(300));
        if (GUILayout.Button("Add New Condition", GUILayout.Width(300)))
        {
            if (ConditionRequirementsMet())
            {
                conditions.Add(newConditionID);
                SortConditionList();
            }
        }
    }

    bool ConditionRequirementsMet() {
        if (!_conditionDatabase.Contains(newConditionID))
        {
            Debug.LogError("Condition with ID " + newConditionID + " does not exist in the database.");
            return false;
        }

        if (conditions.Contains(newConditionID))
        {
            Debug.LogError("This condition has already been added.");
            return false;
        }

        return true;
    }

    void CheckConditions() {
        List<int> indexToRemove = new List<int>();
        for(int i = 0; i < conditions.Count; i++)
        {
            if(!_conditionDatabase.Contains(conditions[i]))
            {
                indexToRemove.Add(i);
            }
        }

        if (indexToRemove.Count == 0)
            return;

        for(int j = 0; j < indexToRemove.Count; j++)
        {
            conditions.RemoveAt(j);
            Debug.LogWarning("A condition was removed from this entry as it no longer exists in the conditions database.");
        }
        EditorUtility.SetDirty(itemDatabase);
        AssetDatabase.SaveAssets();
    }

    void DisplayContainerEditor() {

        // Display current contents
        for(int i = 0; i < contentItems.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("X", GUILayout.Width(50.0f)))
            {
                // Delete this item from the container.
                contentItems.RemoveAt(i);
                contentQuantities.RemoveAt(i);
                return;
            }

            EditorGUILayout.LabelField(itemDatabase.Item(contentItems[i]).ItemName + " x" + contentQuantities[i].ToString());
            EditorGUILayout.EndHorizontal();
        }

        // Display a box which allows the user to add new conditions by their IDs
        EditorGUILayout.BeginHorizontal();
        contentItemID = EditorGUILayout.IntField("Add Item ID: ", contentItemID, GUILayout.Width(300));
        contentQuantity = EditorGUILayout.IntField("Quantity: ", contentQuantity, GUILayout.Width(300));
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Add New Item", GUILayout.Width(300)))
        {
            if (itemDatabase.Item(contentItemID) == null)
                Debug.LogError("Item with ID " + contentItemID + " does not exist.");
            else if (itemDatabase.Item(contentItemID).ItemID == itemID)
                Debug.LogError("Cannot add self to the container.");
            else
            {
                contentItems.Add(contentItemID);
                contentQuantities.Add(contentQuantity);
            }
        }
    }

    void CheckContents() {
        List<int> indexToRemove = new List<int>();
        for (int i = 0; i < contentItems.Count; i++)
        {
            if (!itemDatabase.Contains(contentItems[i]))
            {
                indexToRemove.Add(i);
            }
        }

        if (indexToRemove.Count == 0)
            return;

        for (int j = 0; j < indexToRemove.Count; j++)
        {
            contentItems.RemoveAt(j);
            contentQuantities.RemoveAt(j);
            Debug.LogWarning("An item was removed from this container as it no longer exists in the items database.");
        }
        EditorUtility.SetDirty(itemDatabase);
        AssetDatabase.SaveAssets();
    }

    // Check that particular conditions are met in order for the item to be added
    // to the database. An item should have a UNIQUE ID.
    bool RequiremetsMet(Item item) {
        foreach(Item i in itemDatabase.Items)
        {
            if (i.ItemID == item.ItemID)
                return false;
        }

        return true;
    }

    // Check that the unique ID is not taken.
    bool RequiremetsMet(int id) {
        if (id == -1)
            return true;

        foreach (Item i in itemDatabase.Items)
        {
            if (i.ItemID == id)
                return false;
        }

        return true;
    }

    // Check whether the unique ID is taken BY ANOTHER ITEM...
    bool RequiremetsMet(int id, Item self) {
        if (id == -1 || self == null)
            return true;

        foreach(Item i in itemDatabase.Items)
        {
            if (i != self && i.ItemID == id)
                return false;
        }

        return true;
    }

    void SortPerklist() {
        itemReqPerkIDs.Sort();
    }

    void SortConditionList() {
        conditions.Sort();
    }
}
