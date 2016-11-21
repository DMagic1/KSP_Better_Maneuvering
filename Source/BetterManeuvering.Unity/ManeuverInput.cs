using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace BetterManeuvering.Unity
{
	public class ManeuverInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
	{
		public Button ProgradeDownButton = null;
		public Button ProgradeUpButton = null;
		public Button NormalDownButton = null;
		public Button NormalUpButton = null;
		public Button RadialDownButton = null;
		public Button RadialUpButton = null;
		public Button ResetButton = null;
		public TextHandler ProgradeText = null;
		public TextHandler NormalText = null;
		public TextHandler RadialText = null;
		public TextHandler ProIncrementText = null;
		public TextHandler NormIncrementText = null;
		public TextHandler RadIncrementText = null;
		public TextHandler ResetText = null;
		public TextHandler TotaldVText = null;
		public Transform TitleTransform = null;

		public float prograde_increment = 0.1f;
		public float normal_increment = 0.1f;
		public float radial_increment = 0.1f;

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

		public void ProIncrementDown()
		{
			prograde_increment /= 10;

			if (prograde_increment < 0.01f)
				prograde_increment = 0.01f;

			if (ProIncrementText != null)
			{
				string units = "F0";

				if (prograde_increment < 0.09f)
					units = "F2";
				else if (prograde_increment < 0.9f)
					units = "F1";

				ProIncrementText.OnTextUpdate.Invoke(prograde_increment.ToString(units));
			}
		}

		public void ProIncrementUp()
		{
			prograde_increment *= 10;

			if (prograde_increment > 100f)
				prograde_increment = 100f;

			if (NormIncrementText != null)
			{
				string units = "F0";

				if (prograde_increment < 0.09f)
					units = "F2";
				else if (prograde_increment < 0.9f)
					units = "F1";

				ProIncrementText.OnTextUpdate.Invoke(prograde_increment.ToString(units));
			}
		}

		public void NormIncrementDown()
		{
			normal_increment /= 10;

			if (normal_increment < 0.01f)
				normal_increment = 0.01f;

			if (ProIncrementText != null)
			{
				string units = "F0";

				if (normal_increment < 0.09f)
					units = "F2";
				else if (normal_increment < 0.9f)
					units = "F1";

				NormIncrementText.OnTextUpdate.Invoke(normal_increment.ToString(units));
			}
		}

		public void NormIncrementUp()
		{
			normal_increment *= 10;

			if (normal_increment > 100f)
				normal_increment = 100f;

			if (NormIncrementText != null)
			{
				string units = "F0";

				if (normal_increment < 0.09f)
					units = "F2";
				else if (normal_increment < 0.9f)
					units = "F1";

				NormIncrementText.OnTextUpdate.Invoke(normal_increment.ToString(units));
			}
		}

		public void RadIncrementDown()
		{
			radial_increment /= 10;

			if (radial_increment < 0.01f)
				radial_increment = 0.01f;

			if (RadIncrementText != null)
			{
				string units = "F0";

				if (radial_increment < 0.09f)
					units = "F2";
				else if (radial_increment < 0.9f)
					units = "F1";

				RadIncrementText.OnTextUpdate.Invoke(radial_increment.ToString(units));
			}
		}

		public void RadIncrementUp()
		{
			radial_increment *= 10;

			if (radial_increment > 100f)
				radial_increment = 100f;

			if (RadIncrementText != null)
			{
				string units = "F0";

				if (radial_increment < 0.09f)
					units = "F2";
				else if (radial_increment < 0.9f)
					units = "F1";

				RadIncrementText.OnTextUpdate.Invoke(radial_increment.ToString(units));
			}
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
