using System;
using System.Collections.Generic;
using UnityEngine;

namespace RamRoutes.Model
{
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
        public List<string> friends { set; get; } = new List<string>();
        
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
