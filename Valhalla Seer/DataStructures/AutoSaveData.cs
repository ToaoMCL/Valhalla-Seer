using System;
using System.Collections.Generic;
using System.Text;

namespace Valhalla_Seer.DataStructures
{
    [System.Serializable]
    class AutoSaveData
    {
        public bool AutoSaveOnStartUp { get; private set; }
        public int AutoSaveInterval { get; private set; }

        public AutoSaveData (uint autosaveInterval, bool autoOnStart)
        {
            this.AutoSaveInterval = (int)autosaveInterval;
            this.AutoSaveOnStartUp = autoOnStart;
        }

        internal void Update(uint saveInterval, bool autoSaveOnStartUp)
        {
            int asi = Math.Clamp((int)saveInterval, 30000, int.MaxValue);
            this.AutoSaveInterval = asi;
            this.AutoSaveOnStartUp = autoSaveOnStartUp;
        }
    }
}
