using UnityEngine;

namespace RamRoutes.Model
{
    public enum Stage
    {
        EasternCampus = 0,
        FirstStreet = 1,
        Pedmall = 2
    }

    [System.Serializable]
    public class GameStage
    {
        public Stage area;
        public string stageDisplayName;

        public GameStage() {}

        public GameStage(Stage area, string displayName)
        {
            this.area = area;
            this.stageDisplayName = displayName;
        }

        public static GameStage FromArea(Stage area)
        {
            return new GameStage(area, GetDefaultDisplayName(area));
        }

        public static string GetDefaultDisplayName(Stage area)
        {
            switch (area)
            {
                case Stage.EasternCampus: return "Eastern Campus";
                case Stage.FirstStreet: return "1st Street";
                case Stage.Pedmall: return "Pedmall";
                default: return area.ToString();
            }
        }
    }
}
