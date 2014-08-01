using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Save{
	public class Serialize {
		// <!!> T is any struct or class marked with [Serializable]
		public static void Save<T> (string prefKey, T serializableObject) {
			MemoryStream memoryStream = new MemoryStream ();
			BinaryFormatter bf = new BinaryFormatter ();
			bf.Serialize (memoryStream, serializableObject);
			string tmp = System.Convert.ToBase64String (memoryStream.ToArray ());
			PlayerPrefs.SetString ( prefKey, tmp );
			Debug.Log ("Saved [Key:" + prefKey + "]");
		}
	
		public static T Load<T> (string prefKey) {
			if (!PlayerPrefs.HasKey(prefKey)) return default(T);
			BinaryFormatter bf = new BinaryFormatter();
			string serializedData = PlayerPrefs.GetString(prefKey, "NoData");
			MemoryStream dataStream = new MemoryStream(System.Convert.FromBase64String(serializedData));
			T deserializedObject = (T)bf.Deserialize(dataStream);
			Debug.Log ("Loaded [Key:" + prefKey + "]");
			return deserializedObject;
		}
	}
}