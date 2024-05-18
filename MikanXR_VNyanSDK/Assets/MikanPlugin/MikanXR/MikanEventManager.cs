using MikanCore;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MikanXR
{
	public interface IMikanEventFactory
	{
		MikanEvent CreateEvent(string utfJsonString);
	}

	public class MikanEventFactory<T> : IMikanEventFactory where T : MikanEvent
	{
		public MikanEvent CreateEvent(string utfJsonString)
		{
			// Deserialize enumerations from strings rather than from integers
			var stringEnumConverter = new Newtonsoft.Json.Converters.StringEnumConverter();
			var settings = new JsonSerializerSettings();
			settings.Converters.Add(stringEnumConverter);

			T response= JsonConvert.DeserializeObject<T>(utfJsonString, settings);

			return response;
		}
	}
	
	public class MikanEventManager
	{
		private MikanCoreAPI _coreAPI;
		private MikanLogger _logger;
		private Dictionary<string, IMikanEventFactory> _eventFactories;
	
		public MikanEventManager(MikanCoreAPI coreAPI)
		{
			_coreAPI= coreAPI;
			_logger= coreAPI.Logger;
			_eventFactories = new Dictionary<string, IMikanEventFactory>();
		}

		public void AddEventFactory<T>() where T : MikanEvent
		{
			var factory = new MikanEventFactory<T>();

			_eventFactories.Add(typeof(T).Name, factory);
		}

		public MikanResult FetchNextEvent(out MikanEvent outEvent)
		{
			StringBuilder utf8Buffer = new StringBuilder(1024);
			UIntPtr utf8BufferSize = (UIntPtr)utf8Buffer.Capacity;

			outEvent= null;

			var result = _coreAPI.FetchNextEvent(out string message);
			if (result == MikanResult.Success)
			{
				outEvent = parseEventString(message);
				if (outEvent == null)
				{
					_logger.Log(MikanLogLevel.Error, 
						"fetchNextEvent() - failed to parse event string: " + message);
					result = MikanResult.MalformedResponse;
				}
			}
			
			return (MikanResult)result;
		}

		private MikanEvent parseEventString(string utf8ResponseString)
		{
			MikanEvent mikanEvent = null;

			var root= (JObject)JsonConvert.DeserializeObject(utf8ResponseString);

			// Check if the key "eventType" exists
			if (root.TryGetValue("eventType", out JToken eventTypeElement))
			{
				// Check if the value of "eventType" is a string
				if (eventTypeElement.Type == JTokenType.String)
				{
					// Get the string value of "eventType"
					string eventType = (string)eventTypeElement;
					
					if (_eventFactories.TryGetValue(eventType, out IMikanEventFactory factory))
					{
						mikanEvent = factory.CreateEvent(utf8ResponseString);
					}
					else
					{
						_logger.Log(MikanLogLevel.Error, "Unknown event type: " + eventType);
					}
				}
				else
				{
					_logger.Log(MikanLogLevel.Error, "eventType is not a string.");
				}
			}
			else
			{
				_logger.Log(MikanLogLevel.Error, "eventType key not found.");
			}

			return mikanEvent;
		}
	}	
}