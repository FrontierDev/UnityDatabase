using UnityEngine;
using System.Collections;

namespace GameUtilities {
    public static class ItemUtility {

    }

    public static class GameUtility {
        public static string CleanItemResourcePath(string sourceString, string removeString) {
            int index = sourceString.IndexOf(removeString);
            string cleanPath = (index < 0)
                ? sourceString
                : sourceString.Remove(index, removeString.Length);

            return cleanPath;
        }
    }

    public enum ItemQuality {
        Quality1,   //Level 0-20. Basic gear and cosmetic items.
        Quality2,   //Level 10-50. 'Uncommon' items.
        Quality3,   //Level 15-50+. 'Rare' items.
        Quality4,   //Level 25-50+. Somewhere between blue and epic items.
        Quality5,   //Level 50+ 'epic' items.
        Quality6,   //Level 65+ 'legendary' items.
        Quality7,   //Level 75+ faction items.
        Quality8   //Unique level 80+ items
    }

    public enum ItemType {
        Weapon,
        Armour,
        Consumable,
        Ingredient,
        Misc,
        Container
    }

    /*
     * To be expanded to make handling of animations /combat easier
     */
    public enum WeaponType {
        Melee,
        Ranged,
        Thrown
    }

    public enum ArmourType {
        Head,
        Back,
        Torso,
        Legs,
        Boots
    }

    public enum ArmourMaterial {
        Light,
        Medium,
        Heavy
    }

    public enum AttackType {
        Blunt,
        Pierce,
        Slash
    }

    public enum DefenseType {
        Blunt,
        Heat,
        Nature,
        Pierce,
        Slash
    }

    public enum ConsumableType {
        Food,
        Drink,
        Medical
    }
}


