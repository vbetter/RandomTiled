using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class PauseButton : MonoBehaviour, IPointerClickHandler {
	#region IPointerClickHandler implementation
	public void OnPointerClick (PointerEventData eventData)
	{
		Game2.instance.Pause ();
	}
	#endregion
}