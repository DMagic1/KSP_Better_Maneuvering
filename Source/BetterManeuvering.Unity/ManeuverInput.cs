#region license
/*The MIT License (MIT)

ManeuverInput - Script attached to the manual deltaV input UI object

Copyright (c) 2016 DMagic

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace BetterManeuvering.Unity
{
	public class ManeuverInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
	{
		public Toggle WindowToggle = null;
		public Button ProgradeDownButton = null;
		public Button ProgradeUpButton = null;
		public Button NormalDownButton = null;
		public Button NormalUpButton = null;
		public Button RadialDownButton = null;
		public Button RadialUpButton = null;
		public Button ResetButton = null;
		public Button ProgradeIncDownButton = null;
		public Button ProgradeIncUpButton = null;
		public Button NormalIncDownButton = null;
		public Button NormalIncUpButton = null;
		public Button RadialIncDownButton = null;
		public Button RadialIncUpButton = null;
		public TextHandler ProgradeText = null;
		public TextHandler NormalText = null;
		public TextHandler RadialText = null;
		public TextHandler ProIncrementText = null;
		public TextHandler NormIncrementText = null;
		public TextHandler RadIncrementText = null;
		public TextHandler ResetText = null;
		public TextHandler TotaldVText = null;
		public Transform TitleTransform = null;

		private RectTransform rect;
		private Vector2 mouseStart;
		private Vector3 windowStart;

		public class OnDragEvent : UnityEvent<RectTransform> { }
		public class OnMouseEvent : UnityEvent<bool> { }

		public OnDragEvent DragEvent = new OnDragEvent();
		public OnMouseEvent MouseOverEvent = new OnMouseEvent();

		private void Awake()
		{
			rect = GetComponent<RectTransform>();
		}

		private void OnDestroy()
		{
			gameObject.SetActive(false);
			Destroy(gameObject);
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (rect == null)
				return;

			mouseStart = eventData.position;
			windowStart = rect.position;
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (rect == null)
				return;

			rect.position = windowStart + (Vector3)(eventData.position - mouseStart);

			DragEvent.Invoke(rect);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			MouseOverEvent.Invoke(true);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			MouseOverEvent.Invoke(false);
		}
	}
}
