using MikanXR;

namespace MikanXRPlugin
{
	public class MikanSceneEventHandler : MikanBehavior
	{
		private MikanManager _mikanManager;
		private MikanScene _mikanScene;

		// Start is called before the first frame update
		void Start()
		{
			_mikanManager = MikanManager.Instance;
			if (_mikanManager != null)
			{
				_mikanManager.MikanClient.OnScriptMessage.AddListener(OnMikanMessage);
			}

			_mikanScene = this.gameObject.GetComponent<MikanScene>();
		}

		void OnDisable()
		{
			Cleanup();
		}

		void Cleanup()
		{
			_mikanManager = MikanManager.Instance;
			if (_mikanManager != null)
			{
				_mikanManager.MikanClient.OnScriptMessage.AddListener(OnMikanMessage);
			}
		}

		public void OnMikanMessage(string message)
		{
			Log(MikanLogLevel.Info, $"Received MikanMessage: {message}");
		}
	}

}
