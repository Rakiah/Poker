using UnityEngine;
using System.Collections;

public class CoroutinesLauncher : MonoBehaviour 
{

	public static CoroutinesLauncher Coroutines;

	void Start ()
	{
		Coroutines = this;
	}

	public IEnumerator Translate (GameObject item, Vector3 start, Vector3 end, float timeToLerp)
	{
		float t = 0.0f;
		while(t <= 1.15f)
		{
			if(item == null) break;
			item.transform.position = Vector3.Lerp(start, end, t);
			
			t += Time.deltaTime * CardsSplitter.toolsSplitter.partySpeed;
			yield return null;
		}
		yield return null;
	}

	public IEnumerator TranslateQuaternion (GameObject item, Quaternion start, Quaternion end, float timeToLerp)
	{
		float t = 0.0f;
		
		while(t <= 1.15f)
		{
			if(item == null) break;
			item.transform.rotation = Quaternion.Lerp(start, end, t);
			t += Time.deltaTime * CardsSplitter.toolsSplitter.partySpeed;
			yield return null;
		}
	}
}
