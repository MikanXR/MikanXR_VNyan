using MikanCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MikanXR
{
	public class MikanResponse
	{
		public string responseType { get; set; }
		public int requestId { get; set; }
		public MikanResult resultCode { get; set; }
		
		public MikanResponse()
		{
			responseType= typeof(MikanResponse).Name;
			requestId= Constants.INVALID_MIKAN_ID;
			resultCode= MikanResult.Success;
		}
		public MikanResponse(string inResponseType)
		{
			responseType= inResponseType;
			requestId= Constants.INVALID_MIKAN_ID;
			resultCode= MikanResult.Success;
		}		
	};

	public interface IMikanResponseFactory
	{
		MikanResponse CreateResponse(string utfJsonString);
	}

	public class MikanResponseFactory<T> : IMikanResponseFactory where T : MikanResponse
	{
		public MikanResponse CreateResponse(string utfJsonString)
		{
			// Deserialize enumerations from strings rather than from integers
			var stringEnumConverter = new Newtonsoft.Json.Converters.StringEnumConverter();
			var settings = new JsonSerializerSettings();
			settings.Converters.Add(stringEnumConverter);

			T response = JsonConvert.DeserializeObject<T>(utfJsonString, settings);

			return response;
		}
	}
	
	public class MikanRequestManager
	{
		private MikanCoreAPI _coreAPI= null;
		private MikanLogger _logger= null;
		private Dictionary<string, IMikanResponseFactory> _responseFactory;

		private class PendingRequest
		{
			public int requestId;
			public TaskCompletionSource<MikanResponse> promise;
		};
		private Dictionary<int, PendingRequest> _pendingRequests;
		
		public MikanRequestManager(MikanCoreAPI coreAPI)
		{
			_coreAPI= coreAPI;
			_logger= coreAPI.Logger;
			_responseFactory = new Dictionary<string, IMikanResponseFactory>();
			_pendingRequests = new Dictionary<int, PendingRequest>();
		}

		public MikanResult Initialize()
		{
			if (_coreAPI.MessageClient != null)
			{
				_coreAPI.MessageClient.SetResponseHandler(InternalResponseHandler);
				return MikanResult.Success;
			}

			return MikanResult.InitFailed;
		}

		public void AddResponseFactory<T>() where T : MikanResponse
		{
			var factory = new MikanResponseFactory<T>();

			_responseFactory.Add(typeof(T).Name, factory);
		}

		public Task<MikanResponse> AddResponseHandler(int requestId, MikanResult result)
		{
			TaskCompletionSource<MikanResponse> promise = new TaskCompletionSource<MikanResponse>();

            if (result == MikanResult.Success)
            {
                PendingRequest pendingRequest = new PendingRequest() { 
					requestId = requestId, 
					promise = promise 
				};

				lock (_pendingRequests)
				{
					_pendingRequests.Add(requestId, pendingRequest);
				}
            }
			else
			{
				MikanResponse response = new MikanResponse()
				{
					requestId = requestId, 
					resultCode = result 
				};

				promise.SetResult(response);
			}

            return promise.Task;
		}
		
		public Task<MikanResponse> SendRequest(string requestType, int version = 0)
		{
			MikanResult result =
				_coreAPI.SendRequest(
					requestType,
					string.Empty,
					version,
					out int requestId);

			return AddResponseHandler(requestId, result);
		}
		
		public Task<MikanResponse> SendRequestWithPayload<T>(string requestType, T payload, int version = 0)
		{
			// Serialize enumerations from strings rather than from integers
			var stringEnumConverter = new Newtonsoft.Json.Converters.StringEnumConverter();
			string payloadString = JsonConvert.SerializeObject(payload, stringEnumConverter);

			MikanResult result =
				_coreAPI.SendRequest(
					requestType,
					payloadString,
					version,
					out int requestId);

			return AddResponseHandler(requestId, result);
		}

		public MikanResult SendRequestWithPayloadNoResponse<T>(string requestType, T payload, int version = 0)
		{
			// Serialize enumerations from strings rather than from integers
			var stringEnumConverter = new Newtonsoft.Json.Converters.StringEnumConverter();
			string payloadString = JsonConvert.SerializeObject(payload, stringEnumConverter);

			return
				_coreAPI.SendRequestNoReply(
					requestType,
					payloadString,
					version);
		}		

		private bool InternalResponseHandler(string responseString, out string errorMesg)
		{
			var root = (JObject)JsonConvert.DeserializeObject(responseString);

			errorMesg = "";

			// Check if the key "responseType" exists
			if (!root.TryGetValue("requestId", out JToken requestIdElement) || 
				requestIdElement.Type != JTokenType.Integer)
			{
				errorMesg = "Missing or malformed requestId";
				return false;
			}

			if (!root.TryGetValue("responseType", out JToken responseTypeElement) || 
				responseTypeElement.Type != JTokenType.String)
			{
				errorMesg = "Missing or malformed responseType";
				return false;
			}

			int requestId = (int)requestIdElement;
			string responseType = (string)responseTypeElement;

			// Look up the pending request
			PendingRequest pendingRequest = null;
			lock (_pendingRequests)
			{
				if (_pendingRequests.TryGetValue(requestId, out pendingRequest))
				{
					_pendingRequests.Remove(requestId);
				}
			}

			// Bail if the corresponding pending request is not found
			if (pendingRequest == null)
			{
				errorMesg = $"Invalid pending request id({requestId}) for response type {responseType}";
				return false;
			}

			// Look up the response factory for the response type
			MikanResponse response = null;
			if (_responseFactory.TryGetValue(responseType, out IMikanResponseFactory factory))
			{
				// Parse the response string using the factory
				response = factory.CreateResponse(responseString);
			}
			else
			{
				errorMesg = $"Unknown response type: {responseType}";
				
			}

			if (response == null)
			{
				response = new MikanResponse()
				{
					requestId = requestId,
					resultCode = MikanResult.MalformedResponse
				};
			}

			pendingRequest.promise.SetResult(response);
			return response != null;
		}
	}	
}