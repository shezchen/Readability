using System;

namespace Architecture
{
    [Serializable]
    public struct GameSave
    {
        public DateTime LastSaveTime;
        public int GameDay;

        public GameSave(int gameDay)
        {
            LastSaveTime = DateTime.Now;
            GameDay = gameDay;
        }
    }
}