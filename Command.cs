using System;
using System.Collections.Generic;

namespace WindowsRemoteManager
{
    class Command
    {
        public Int64 ID;
        public List<string> Instructions;

        public Command(Int64 ID, List<string> Instructions)
        {
            this.ID = ID; this.Instructions = Instructions;
        }
    }
}
