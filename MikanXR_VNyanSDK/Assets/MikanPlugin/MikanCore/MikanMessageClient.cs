using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using WebSocketSharp;

namespace MikanCore
{
	public class MikanMessageClient : IDisposable
	{
		private MikanLogger _logger= null;

		private WebSocket _wsConnection;
		private Dictionary<string, WebSocketSharp.Net.Cookie> _clientProperties = null;

		private Queue<string> _eventQueue= null;

		public bool IsConnected
		{
			get
			{
				return (_wsConnection != null && _wsConnection.ReadyState == WebSocketSharp.WebSocketState.Open);
			}
		}

		public delegate void ResponseHandler(string responseString);
		private ResponseHandler _responseHandler = null;
		public void SetResponseHandler(ResponseHandler handler)
		{
			_responseHandler = handler;
		}

		public MikanMessageClient(MikanLogger logger)
		{
			_logger = logger;
			_clientProperties = new Dictionary<string, WebSocketSharp.Net.Cookie>();
			_eventQueue= new Queue<string>();
		}

		public MikanResult Initialize()
		{
			return MikanResult.Success;
		}

		public void Dispose()
		{
			Disconnect();
		}

		public MikanResult SetClientProperty(string key, string value)
		{
			if (_clientProperties.ContainsKey(key))
			{
				_clientProperties[key].Value = value;
			}
			else
			{
				_clientProperties.Add(key, new WebSocketSharp.Net.Cookie(key, value));
			}

			return MikanResult.Success;
		}

		public MikanResult Connect(string host, string port)
		{
			if (_wsConnection != null && _wsConnection.ReadyState == WebSocketSharp.WebSocketState.Open)
			{
				return MikanResult.Success;
			}

			string url = $"{host}:{port}";
			_wsConnection = new WebSocket(url);

			foreach (var cookie in _clientProperties.Values)
			{
				_wsConnection.SetCookie(cookie);
			}

			_wsConnection.OnOpen += OnWebsocketConnect;
			_wsConnection.OnMessage += WebsocketMessageHandler;
			_wsConnection.OnError += OnWebsocketError;
			_wsConnection.OnClose += OnWebsocketDisconnect;

			return MikanResult.Success;
		}

		public void Disconnect()
		{
			if (_wsConnection != null)
			{
				// Attempt to both close and dispose the existing connection
				try
				{
					_wsConnection.Close(CloseStatusCode.Normal, "User requested disconnect");
					((IDisposable)_wsConnection).Dispose();
				}
				catch (Exception ex)
				{ 
					_logger.Log(MikanLogLevel.Error, $"Error closing websocket connection: {ex.Message}");
				}

				_wsConnection = null;
			}
		}

		private void OnWebsocketConnect(object sender, EventArgs e)
		{
			_logger.Log(MikanLogLevel.Info, "Connected to web socket server");
		}

		private void OnWebsocketDisconnect(object sender, CloseEventArgs d)
		{
			string reason= d?.Reason ?? "<NONE>";

			_logger.Log(MikanLogLevel.Info, $"Disconnected from web socket server: {reason}");
		}

		private void OnWebsocketError(object sender, ErrorEventArgs e)
		{
			_logger.Log(MikanLogLevel.Info, $"Web socket Error: {e.Message}");
		}

		private void WebsocketMessageHandler(object sender, MessageEventArgs e)
		{
			if (e.IsText)
			{
				string message = e.Data;
				var root = (JObject)JsonConvert.DeserializeObject(message);

				if (root.TryGetValue("eventType", out JToken eventTypeElement) &&
					eventTypeElement.Type == JTokenType.String)
				{
					_eventQueue.Enqueue(message);
				}
				else if (root.TryGetValue("responseType", out JToken responseTypeElement) &&
						responseTypeElement.Type == JTokenType.String)
				{
					if (_responseHandler != null)
					{
						_responseHandler(message);
					}
					else
					{
						_logger.Log(MikanLogLevel.Error, 
							$"Received response message but no handler set: {message}");
					}
				}
				else
				{
					_logger.Log(MikanLogLevel.Error,
						$"Received unsupported message: {message}");
				}
			}
			else
			{
				_logger.Log(MikanLogLevel.Info, "Received unsupported binary message");
			}
		}

		public MikanResult FetchNextEvent(out string message)
		{
			if (_eventQueue.Count > 0)
			{
				message = _eventQueue.Dequeue();
				return MikanResult.Success;
			}

			message = string.Empty;
			return MikanResult.NoData;
		}

		public MikanResult SendRequest(string message)
		{
			if (IsConnected)
			{
				_wsConnection.Send(message);

				return MikanResult.Success;
			}
			
			return MikanResult.NotConnected;
		}
	}
}
