using MikanCore;
using Newtonsoft.Json;
using System;

namespace MikanXR
{
	public class MikanClientInfo
	{
		public string clientId { get; set; } = "";
		public string engineName { get; set; } = "";
		public string engineVersion { get; set; } = "";
		public string applicationName { get; set; } = "";
		public string applicationVersion { get; set; } = "";
		public string xrDeviceName { get; set; } = "";
		public MikanClientGraphicsApi graphicsAPI { get; set; } = MikanClientGraphicsApi.UNKNOWN;
		public int mikanCoreSdkVersion { get; set; } = -1;
		public bool supportsRGB24 { get; set; } = false;
		public bool supportsRGBA32 { get; set; } = false;
		public bool supportsBGRA32 { get; set; } = false;
		public bool supportsDepth { get; set; } = false;
	}

	public class MikanAPI : IDisposable
	{
		private MikanCoreAPI _coreAPI;
		public MikanCoreAPI CoreAPI => _coreAPI;

		private MikanRequestManager _requestManager;
		public MikanRequestManager RequestManager => _requestManager;
		
		private MikanEventManager _eventManager;
		public MikanEventManager EventManager => _eventManager;

		private MikanRenderTargetAPI _renderTargetAPI;
		public MikanRenderTargetAPI RenderTargetAPI => _renderTargetAPI;
		
		private MikanVideoSourceAPI _videoSourceAPI;
		public MikanVideoSourceAPI VideoSourceAPI => _videoSourceAPI;
		
		private MikanVRDeviceAPI _vrDeviceAPI;
		public MikanVRDeviceAPI VRDeviceAPI => _vrDeviceAPI;
		
		private MikanScriptAPI _scriptAPI;
		public MikanScriptAPI ScriptAPI => _scriptAPI;
		
		private MikanStencilAPI _stencilAPI;
		public MikanStencilAPI StencilAPI => _stencilAPI;
		
		private MikanSpatialAnchorAPI _spatialAnchorAPI;
		public MikanSpatialAnchorAPI SpatialAnchorAPI => _spatialAnchorAPI;

		public MikanAPI()
		{
			_coreAPI = new MikanCoreAPI();

			_requestManager = new MikanRequestManager(_coreAPI);
			_eventManager = new MikanEventManager(_coreAPI);
			
			// Create the child APIs
			_renderTargetAPI = new MikanRenderTargetAPI(_requestManager);
			_videoSourceAPI = new MikanVideoSourceAPI(_requestManager);	
			_vrDeviceAPI = new MikanVRDeviceAPI(_requestManager);	
			_scriptAPI = new MikanScriptAPI(_requestManager);
			_stencilAPI = new MikanStencilAPI(_requestManager);
			_spatialAnchorAPI = new MikanSpatialAnchorAPI(_requestManager);
			
			// Register base response types (child API classes will register their own types)
			_requestManager.AddResponseFactory<MikanResponse>();
			
			// Register all event types
			_eventManager.AddEventFactory<MikanConnectedEvent>();
			_eventManager.AddEventFactory<MikanDisconnectedEvent>();
			_eventManager.AddEventFactory<MikanVideoSourceOpenedEvent>();
			_eventManager.AddEventFactory<MikanVideoSourceClosedEvent>();
			_eventManager.AddEventFactory<MikanVideoSourceNewFrameEvent>();
			_eventManager.AddEventFactory<MikanVideoSourceAttachmentChangedEvent>();
			_eventManager.AddEventFactory<MikanVideoSourceIntrinsicsChangedEvent>();
			_eventManager.AddEventFactory<MikanVideoSourceModeChangedEvent>();
			_eventManager.AddEventFactory<MikanVRDevicePoseUpdateEvent>();
			_eventManager.AddEventFactory<MikanVRDeviceListUpdateEvent>();
			_eventManager.AddEventFactory<MikanAnchorPoseUpdateEvent>();
			_eventManager.AddEventFactory<MikanAnchorListUpdateEvent>();
			_eventManager.AddEventFactory<MikanScriptMessagePostedEvent>();
		}

		public void Dispose()
		{
			_coreAPI.Dispose();
		}

		public MikanResult Initialize(MikanLogLevel minLogLevel)
		{		
			MikanResult result = _coreAPI.Initialize(minLogLevel);
			if (result != MikanResult.Success)
			{
				return result;
			}
			
			result= _requestManager.Initialize();
			if (result != MikanResult.Success)
			{
				return result;
			}

			return MikanResult.Success;
		}

		public async void Shutdown()
		{
			// Tell the server to free it's existing render target
			// (ignored if there isn't a render target allocated)
			if (GetIsConnected())
			{
				await _renderTargetAPI.FreeRenderTargetTextures();
			}

			_coreAPI.Shutdown();
		}

		public int GetCoreSDKVersion()
		{
			return _coreAPI.GetCoreSDKVersion();
		}

		public MikanResult SetClientInfo(MikanClientInfo clientInfo)
		{
			// Stamp with the core sdk version
			clientInfo.mikanCoreSdkVersion = GetCoreSDKVersion();
			clientInfo.clientId = _coreAPI.GetClientUniqueID();

			// Serialize enumerations from strings rather than from integers
			var stringEnumConverter = new Newtonsoft.Json.Converters.StringEnumConverter();
			string clientInfoString = JsonConvert.SerializeObject(clientInfo, stringEnumConverter);

			return _coreAPI.SetClientInfo(clientInfoString);
		}

		public MikanResult Connect(string host="", string port="")
		{
			return _coreAPI.Connect(host, port);
		}

		public bool GetIsConnected()
		{
			return _coreAPI.GetIsConnected();
		}

		public MikanResult FetchNextEvent(out MikanEvent outEvent)
		{
			return _eventManager.FetchNextEvent(out outEvent);
		}

		public MikanResult Disconnect()
		{
			return _coreAPI.Disconnect();
		}
	}
}