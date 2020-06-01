using System.Collections.Generic;

namespace WorldMazes
{
    class WorldSettings
    {
#pragma warning disable 414
        private string HowToUseMode = "To use standard mode, use Standard or 0. For memory mode, use Memory or 1.";
        public Mode Mode = Mode.Standard;
        private string HowToUseAutoReset = "Have the module automatically reset on strikes in Memory Mode.";
        public bool AutoReset = true;
        private string HowToVeto = "Mazes will typically take the full name featured in the manual.";
        public List<string> Veto = new List<string> { "If you would like to keep certain mazes from spawning, enter them here." };
#pragma warning restore 414
    }
}
