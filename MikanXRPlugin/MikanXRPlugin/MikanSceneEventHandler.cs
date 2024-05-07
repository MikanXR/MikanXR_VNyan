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
				_mikanManager.OnMessageEvent += OnMikanMessage;
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
				_mikanManager.OnMessageEvent -= OnMikanMessage;
			}
		}

		public void OnMikanMessage(string message)
		{
			Log(MikanLogLevel.Info, $"Received MikanMessage: {message}");
		}
	}

}
