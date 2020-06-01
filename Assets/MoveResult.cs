using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorldMazes
{
    struct MoveResult
    {
        public string StrikeMessage;    // null if move was valid
        public string NewState;         // null if move was invalid
    }
}
