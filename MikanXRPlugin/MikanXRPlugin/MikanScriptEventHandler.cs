using MikanCore;
using UnityEngine;

namespace MikanXRPlugin
{
	public class MikanScriptEventHandler : MonoBehaviour
	{
		public MikanManager OwnerManager { get; private set; }
		private MikanClient _client = null;
		private MikanLogger _logger = null;

		public void Setup(MikanManager ownerManager)
		{
			OwnerManager = ownerManager;
			_client = ownerManager.MikanClient;

			// Listen for script messages from Mikan
			_client.OnScriptMessage.AddListener(OnMikanMessage);
		}

		public void Teardown()
		{
			if (_client != null)
			{
				_client.OnScriptMessage.RemoveListener(OnMikanMessage);
			}
		}

		public void OnMikanMessage(string message)
		{
			_logger.Log(MikanLogLevel.Info, $"Received MikanMessage: {message}");
		}
	}
}
