using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikanCore
{
	public class MikanCoreAPI : IDisposable
	{
		private bool _isInitialized = false;
		private string _clientUniqueID = "";
		private int _nextRequestId = 0;

		private MikanLogger _logger;
		public MikanLogger Logger => _logger;

		private MikanMessageClient _messageClient;
		public MikanMessageClient MessageClient => _messageClient;

		public MikanCoreAPI()
		{
			_clientUniqueID = Guid.NewGuid().ToString();
			_logger = new MikanLogger();
			_messageClient = new MikanMessageClient(_logger);
		}

		public void Dispose()
		{
			Shutdown();

			_messageClient.Dispose();
			_logger.Dispose();
		}

		public MikanResult Initialize(MikanLogLevel minLogLevel)
		{
			if (_isInitialized)
			{
				return MikanResult.Success;
			}

			LoggerSettings loggerSettings = new LoggerSettings()
			{
				min_log_level = minLogLevel,
				log_filename = "MikanCore.log",
				enable_console = true,
			};
			_logger.Init(loggerSettings);

			MikanResult result = _messageClient.Initialize();
			if (result != MikanResult.Success)
			{
				return result;
			}

			_isInitialized = true;

			return MikanResult.Success;
		}

		public MikanResult Shutdown()
		{
			_messageClient.Disconnect();
			_isInitialized = false;

			return MikanResult.Success;
		}

		public int GetCoreSDKVersion()
		{
			return Constants.MIKAN_CORE_SDK_VERSION;
		}

		public string GetClientUniqueID()
		{
			return _clientUniqueID;
		}

		public bool GetIsInitialized()
		{
			return _isInitialized;
		}

		public MikanResult SetClientProperty(string key, string value)
		{
			return _messageClient.SetClientProperty(key, value);
		}

		public MikanResult Connect(string host = "", string port = "")
		{
			return _messageClient.Connect(host, port);
		}

		public bool GetIsConnected()
		{
			return _messageClient.IsConnected;
		}

		public MikanResult Disconnect()
		{
			if (GetIsConnected())
			{
				_messageClient.Disconnect();
				return MikanResult.Success;
			}

			return MikanResult.NotConnected;
		}

		public MikanResult FetchNextEvent(out string message)
		{
			return _messageClient.FetchNextEvent(out message);
		}

		public MikanResult SendRequest(
			string requestType,
			string payload,
			int version,
			out int requestId)
		{
			return SendRequestInternal(requestType, payload, version, true, out requestId);
		}

		public MikanResult SendRequestNoReply(
			string requestType,
			string payload,
			int version)
		{
			return SendRequestInternal(requestType, payload, version, false, out _);
		}

		private MikanResult SendRequestInternal(
			string requestType,
			string payload,
			int version,
			bool wantsResponse,
			out int requestId)
		{
			if (_messageClient.IsConnected)
			{
				StringBuilder stringBuilder = new StringBuilder();

				stringBuilder.AppendLine("{");
				if (wantsResponse)
				{
					stringBuilder.AppendLine($"	\"requestId\": {_nextRequestId},");
				}
				stringBuilder.AppendLine($"	\"requestType\": \"{requestType}\",");
				if (!string.IsNullOrEmpty(payload))
				{
					stringBuilder.AppendLine($"	\"version\": {version},");
					stringBuilder.AppendLine($"	\"payload\": {payload}");
				}
				else
				{
					stringBuilder.Append($"	\"version\": {version}");
				}
				stringBuilder.AppendLine("}");

				if (wantsResponse)
				{
					requestId = _nextRequestId;
					_nextRequestId++;
				}
				else
				{
					requestId = -1;
				}

				_messageClient.SendRequest(stringBuilder.ToString());

				return MikanResult.Success;
			}
			else
			{
				requestId = -1;
				return MikanResult.NotConnected;
			}
		}
	}
}
