using System;
using System.Collections.Generic;
using UnityEngine;

namespace RamRoutes.Model
{
    [Serializable]
    public enum EquippedSkin
    {
        Default,
        Rainbow,
        Summer,
        Winter
    }

    [Serializable]
    public enum EquippedAccessory
    {
        None,
        Torch,
        Horns
    }

    [Serializable]
    public class User
    {
        public string userId { set; get; }
        public string notificationToken  { set; get; }
        public string name  { set; get; }
        public string email  { set; get; }
        public string lastLogin  { set; get; }
        public int coins { set; get; }
        public int knowledgePoints { set; get; }
        public string currentBuilding { set; get; }
        public string residenceHall { set; get; }
        public string bio { set; get; } = "";
        public EquippedSkin equippedSkin { set; get; } = EquippedSkin.Default;
        public EquippedAccessory equippedAccessory { set; get; } = EquippedAccessory.None;
        public List<string> friends { set; get; } = new List<string>();
        public string GetEquippedSkinAsString()
        {
            return equippedSkin.ToString();
        }
        public void SetEquippedSkinFromString(string skinName)
        {
            if (Enum.TryParse<EquippedSkin>(skinName, out var parsedSkin))
            {
                equippedSkin = parsedSkin;
            }
            else
            {
                equippedSkin = EquippedSkin.Default; // Fallback to default if parsing fails
            }
        }

        public string GetEquippedAccessoryAsString()
        {
            return equippedAccessory.ToString();
        }
        public void SetEquippedAccessoryFromString(string accessoryName)
        {
            if (Enum.TryParse<EquippedAccessory>(accessoryName, out var parsedAccessory))
            {
                equippedAccessory = parsedAccessory;
            }
            else
            {
                equippedAccessory = EquippedAccessory.None; // Fallback to none if parsing fails
            }
        }

        public User(string userId, string notificationToken, string name, string email)
        {
            this.userId = userId;
            this.notificationToken = notificationToken;
            this.name = name;
            this.email = email;
            this.friends = new List<string>();
        }
    }
}
