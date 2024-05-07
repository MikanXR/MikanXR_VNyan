using MikanXR;
using System;
using System.Collections.Generic;
using System.Text;

namespace MikanXRPlugin
{
    public interface IMikanLogger
    {
        void Log(MikanLogLevel mikanLogLevel, string log_message);
    }
}
