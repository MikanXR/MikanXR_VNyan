using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.VersionControl;
using WebSocketSharp;

namespace MikanCore
{
	public class MikanMessageClient : IDisposable
	{
		private MikanLogger _logger= null;

		private WebSocket _wsConnection;
		private string _clientInfo;

		class WebsocketEvent
		{
			public enum EventType
			{
				Connect,
				Disconnect,
				Message,
				Error
			};
			public EventType eventType;
			public string payload;
		}
		private Queue<WebsocketEvent> _websocketThreadEventQueue= null;
		private Queue<WebsocketEvent> _mainThreadEventQueue= null;
		private Queue<string> _mainThreadMessageQueue= null;

		public bool IsConnected
		{
			get
			{
				return (_wsConnection != null && _wsConnection.ReadyState == WebSocketSharp.WebSocketState.Open);
			}
		}

		public delegate bool ResponseHandler(string responseString, out string errorMesg);
		private ResponseHandler _responseHandler = null;
		public void SetResponseHandler(ResponseHandler handler)
		{
			_responseHandler = handler;
		}

		public MikanMessageClient(MikanLogger logger)
		{
			_logger = logger;
			_clientInfo = "";
			_websocketThreadEventQueue = new Queue<WebsocketEvent>();
			_mainThreadEventQueue= new Queue<WebsocketEvent>();
			_mainThreadMessageQueue= new Queue<string>();
		}

		public MikanResult Initialize()
		{
			return MikanResult.Success;
		}

		public void Dispose()
		{
			Disconnect();
		}

		public MikanResult SetClientInfo(string clientInfo)
		{
			if (!clientInfo.IsNullOrEmpty())
			{
				_clientInfo= clientInfo;
				return MikanResult.Success;
			}

			return MikanResult.GeneralError;
		}

		public MikanResult Connect(string host, string port)
		{
			if (_wsConnection != null && _wsConnection.ReadyState == WebSocketSharp.WebSocketState.Open)
			{
				return MikanResult.Success;
			}

			string url = $"{host}:{port}";
			_wsConnection = new WebSocket(url);

			_wsConnection.OnOpen += EnqueueWebsocketConnectEvent;
			_wsConnection.OnMessage += EnqueueWebsocketMessageEvent;
			_wsConnection.OnError += EnqueueWebsocketErrorEvent;
			_wsConnection.OnClose += EnqueueWebsocketDisconnectEvent;

			_wsConnection.ConnectAsync();

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

		private void EnqueueWebsocketConnectEvent(object sender, EventArgs e)
		{
			lock (_websocketThreadEventQueue)
			{
				_websocketThreadEventQueue.Enqueue(new WebsocketEvent()
				{
					eventType = WebsocketEvent.EventType.Connect,
					payload = string.Empty
				});
			}
		}

		private void EnqueueWebsocketDisconnectEvent(object sender, CloseEventArgs d)
		{
			lock (_websocketThreadEventQueue)
			{
				_websocketThreadEventQueue.Enqueue(new WebsocketEvent()
				{
					eventType = WebsocketEvent.EventType.Disconnect,
					payload = d?.Reason ?? "<NONE>"
				});
			}
		}

		private void EnqueueWebsocketErrorEvent(object sender, ErrorEventArgs e)
		{
			EnqueueWebsocketErrorMessage(e.Message);
		}

		private void EnqueueWebsocketErrorMessage(string errorMessage)
		{
			lock (_websocketThreadEventQueue)
			{
				_websocketThreadEventQueue.Enqueue(new WebsocketEvent()
				{
					eventType = WebsocketEvent.EventType.Error,
					payload = errorMessage
				});
			}
		}

		private void EnqueueWebsocketMessageEvent(object sender, MessageEventArgs e)
		{
			if (e.IsText)
			{
				string message = e.Data;
				var root = (JObject)JsonConvert.DeserializeObject(message);

				if (root.TryGetValue("eventType", out JToken eventTypeElement) &&
					eventTypeElement.Type == JTokenType.String)
				{
					lock (_websocketThreadEventQueue)
					{
						_websocketThreadEventQueue.Enqueue(new WebsocketEvent()
						{
							eventType = WebsocketEvent.EventType.Message,
							payload = message
						});
					}
				}
				else if (root.TryGetValue("responseType", out JToken responseTypeElement) &&
						responseTypeElement.Type == JTokenType.String)
				{
					if (_responseHandler != null)
					{
						if (!_responseHandler(message, out string errorMesg))
						{
							EnqueueWebsocketErrorMessage(errorMesg);
						}
					}
					else
					{
						EnqueueWebsocketErrorMessage($"Received response message but no handler set: {message}");
					}
				}
				else
				{
					EnqueueWebsocketErrorMessage($"Received unsupported message: {message}");
				}
			}
			else
			{
				EnqueueWebsocketErrorMessage("Received unsupported binary message");
			}
		}

		public MikanResult FetchNextEvent(out string message)
		{
			// Copy events from the websocket thread to the main thread
			lock (_websocketThreadEventQueue)
			{
				while (_websocketThreadEventQueue.Count > 0)
				{
					_mainThreadEventQueue.Enqueue(_websocketThreadEventQueue.Dequeue());
				}
			}

			// Process the main thread events
			while (_mainThreadEventQueue.Count > 0)
			{
				WebsocketEvent websocketEvent = _mainThreadEventQueue.Dequeue();

				switch(websocketEvent.eventType)
				{
					case WebsocketEvent.EventType.Connect:
						HandleConnectEvent(websocketEvent);
						break;
					case WebsocketEvent.EventType.Disconnect:
						HandleDisconnectEvent(websocketEvent);
						break;
					case WebsocketEvent.EventType.Message:
						HandleMessageEvent(websocketEvent);
						break;
					case WebsocketEvent.EventType.Error:
						HandleErrorEvent(websocketEvent);
						break;
				}
			}

			// Return the next message event in the queue
			if (_mainThreadEventQueue.Count > 0)
			{
				message = _mainThreadMessageQueue.Dequeue();
				return MikanResult.Success;
			}

			message = string.Empty;
			return MikanResult.NoData;
		}

		private void HandleConnectEvent(WebsocketEvent websocketEvent)
		{
			_logger.Log(MikanLogLevel.Info, "Connected to web socket server");

			// Send clinetInfo payload in a connect request immediately upon connection
			StringBuilder ss = new StringBuilder();
			ss.AppendLine("{");
			ss.AppendLine($" \"requestId\":-1,");
			ss.AppendLine($" \"requestType\":\"connect\",");
			ss.AppendLine($" \"version\":0,");
			ss.AppendLine($" \"payload\":{_clientInfo}");
			ss.AppendLine("}");

			string requestString = ss.ToString();
			_wsConnection.Send(requestString);
		}

		private void HandleDisconnectEvent(WebsocketEvent websocketEvent)
		{
			_logger.Log(MikanLogLevel.Info, $"Disconnected from web socket server: {websocketEvent.payload}");
		}

		private void HandleMessageEvent(WebsocketEvent websocketEvent)
		{
			_mainThreadMessageQueue.Enqueue(websocketEvent.payload);
		}

		private void HandleErrorEvent(WebsocketEvent websocketEvent)
		{
			_logger.Log(MikanLogLevel.Info, $"Web socket Error: {websocketEvent.payload}");
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
