using MikanCore;
using MikanXR;
using UnityEngine;

namespace MikanXRPlugin
{
    public class MikanBehavior : MonoBehaviour
    {
        public MikanClient Client => MikanManager.Instance.MikanClient;
        public MikanAPI ClientAPI => Client.ClientAPI;
        public MikanLogger Logger => ClientAPI.CoreAPI.Logger;

        protected void Log(MikanLogLevel logLevel, string message)
        {
            Logger?.Log(logLevel, message);
        }
    }
}
