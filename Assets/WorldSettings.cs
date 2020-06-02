using System.Collections.Generic;

namespace WorldMazes
{
    class WorldSettings
    {
#pragma warning disable 414
        //This needs to be public so that it can be written to the JSON file.
        public readonly string[] Info = new string[] {
            "Mode: To use standard mode, use Standard or 0. For memory mode, use Memory or 1.",
            "AutoReset: Have the module automatically reset on strikes in Memory Mode. Set to true or false."
        };
        public Mode Mode = Mode.Standard;
        public bool AutoReset = true;
#pragma warning restore 414
    }
}
