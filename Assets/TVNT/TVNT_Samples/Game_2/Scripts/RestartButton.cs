using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class RestartButton : MonoBehaviour, IPointerClickHandler {
	#region IPointerClickHandler implementation
	public void OnPointerClick (PointerEventData eventData)
	{
		Game2.instance.RestartGame ();
	}
	#endregion
}
