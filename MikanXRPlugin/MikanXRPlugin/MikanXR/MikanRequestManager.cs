using MikanCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

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

	public interface IMikanTextResponseFactory
	{
		MikanResponse CreateResponse(string utfJsonString);
	}

	public class MikanTextResponseFactory<T> : IMikanTextResponseFactory where T : MikanResponse
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

	public interface IMikanBinaryResponseFactory
	{
		MikanResponse CreateResponse(
			int requestId, 
			MikanResult resultCode, 
			string responseType, 
			BinaryReader reader);
	}

	public class MikanBinaryResponseFactory<T> : IMikanBinaryResponseFactory where T : MikanResponse, new()
	{
		public virtual MikanResponse CreateResponse(
			int inRequestId, 
			MikanResult inResultCode,
			string inResponseType, 
			BinaryReader inReader)
		{
			return new T() { 
				requestId = inRequestId, 
				resultCode = inResultCode,
				responseType = inResponseType };
		}
	}

	public class MikanRequestManager
	{
		private MikanCoreAPI _coreAPI= null;
		private MikanLogger _logger= null;
		private Dictionary<string, IMikanTextResponseFactory> _textResponseFactory;
		private Dictionary<string, IMikanBinaryResponseFactory> _binaryResponseFactory;

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
			_textResponseFactory = new Dictionary<string, IMikanTextResponseFactory>();
			_binaryResponseFactory = new Dictionary<string, IMikanBinaryResponseFactory>();
			_pendingRequests = new Dictionary<int, PendingRequest>();
		}

		public MikanResult Initialize()
		{
			if (_coreAPI.MessageClient != null)
			{
				_coreAPI.MessageClient.SetTextResponseHandler(InternalTextResponseHandler);
				_coreAPI.MessageClient.SetBinaryResponseHandler(InternalBinaryResponseHandler);
				return MikanResult.Success;
			}

			return MikanResult.InitFailed;
		}

		public void AddTextResponseFactory<T>() where T : MikanResponse
		{
			var factory = new MikanTextResponseFactory<T>();

			_textResponseFactory.Add(typeof(T).Name, factory);
		}

		public void AddBinaryResponseFactory<F, T>() 
			where F : IMikanBinaryResponseFactory, new()
			where T : MikanResponse, new()
		{
			var factory = new F();

			_binaryResponseFactory.Add(typeof(T).Name, factory);
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

		private bool InternalTextResponseHandler(string responseString, out string errorMesg)
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
			if (_textResponseFactory.TryGetValue(responseType, out IMikanTextResponseFactory factory))
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
					responseType = responseType,
					requestId = requestId,
					resultCode = MikanResult.MalformedResponse
				};
			}

			pendingRequest.promise.SetResult(response);
			return response != null;
		}

		private bool InternalBinaryResponseHandler(byte[] responseBytes, out string errorMesg)
		{
			var binaryReader = new BinaryReader(new MemoryStream(responseBytes));
			bool success = true;

			errorMesg = "";

			try
			{
				// Read the response type
				int requestTypeUTF8StringLength = binaryReader.ReadInt32();
				string responseType =
					requestTypeUTF8StringLength > 0
					? System.Text.Encoding.UTF8.GetString(binaryReader.ReadBytes(requestTypeUTF8StringLength))
					: "";

				// Read the request ID
				int requestId = binaryReader.ReadInt32();

				// Read the result code
				MikanResult resultCode = (MikanResult)binaryReader.ReadInt32();

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
				if (_binaryResponseFactory.TryGetValue(responseType, out IMikanBinaryResponseFactory factory))
				{
					// Parse the binary reader using the factory
					response = factory.CreateResponse(requestId, resultCode, responseType, binaryReader);
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
				success= response != null;
			}
			catch (Exception e)
			{
				errorMesg = $"Malformed binary response: {e.Message}";
				success = false;
			}
			finally
			{
				if (binaryReader != null)
				{
					binaryReader.Dispose();
				}
			}

			return success;
		}
	}	
}