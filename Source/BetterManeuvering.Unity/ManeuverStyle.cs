#region License
/*
 * Better Maneuvering
 * 
 * ManeuverStyle - Script for controlling the selection of UI style elements
 * 
 * Copyright (C) 2016 DMagic
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation, either version 3 of the License, or 
 * (at your option) any later version. 
 * 
 * This program is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
 * GNU General Public License for more details. 
 * 
 * You should have received a copy of the GNU General Public License 
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. 
 * 
 * 
 */
#endregion

using UnityEngine;
using UnityEngine.UI;

namespace BetterManeuvering.Unity
{
	public class ManeuverStyle : MonoBehaviour
	{
		public enum ElementTypes
		{
			None,
			Window,
			Box,
			Button,
			Toggle,
			Input
		}

		[SerializeField]
		private ElementTypes m_ElementType = ElementTypes.None;

		public ElementTypes ElementType
		{
			get { return m_ElementType; }
		}

		private void setSelectable(Sprite normal, Sprite highlight, Sprite active, Sprite inactive)
		{
			Selectable select = GetComponent<Selectable>();

			if (select == null)
				return;

			select.image.sprite = normal;
			select.image.type = Image.Type.Sliced;
			select.transition = Selectable.Transition.SpriteSwap;

			SpriteState spriteState = select.spriteState;
			spriteState.highlightedSprite = highlight;
			spriteState.pressedSprite = active;
			spriteState.disabledSprite = inactive;
			select.spriteState = spriteState;
		}

		public void setImage(Sprite sprite, Material mat, Color color)
		{
			Image image = GetComponent<Image>();

			if (image == null)
				return;

			image.sprite = sprite;
			image.color = color;
			image.material = mat;
			image.type = Image.Type.Sliced;
		}

		public void setButton(Sprite normal, Sprite highlight, Sprite active, Sprite inactive)
		{
			setSelectable(normal, highlight, active, inactive);
		}

		public void setToggle(Sprite normal, Sprite highlight, Sprite active, Sprite inactive, Sprite topImage, Color topColor)
		{
			setSelectable(normal, highlight, active, inactive);

			Toggle toggle = GetComponent<Toggle>();

			if (toggle == null)
				return;

			Image toggleImage = toggle.graphic as Image;

			if (toggleImage == null)
				return;

			toggleImage.sprite = active;
			toggleImage.type = Image.Type.Sliced;

			Image top = GetComponentsInChildren<Image>(true)[2];

			top.sprite = topImage;
			top.color = topColor;
		}

	}
}
