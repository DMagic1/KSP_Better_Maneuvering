#region license
/*The MIT License (MIT)

ManeuverStyle - Script for controlling the selection of UI style elements

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
			Input,
            TabBackground,
            TabButtonBackground,
            TabTextBackground,
            ResetButton,
            ScrollBar,
		}

		[SerializeField]
		private ElementTypes m_ElementType = ElementTypes.None;

		public ElementTypes ElementType
		{
			get { return m_ElementType; }
		}

		private void setSelectable(Sprite normal, Sprite highlight, Sprite active, Sprite inactive, Image.Type type)
		{
			Selectable select = GetComponent<Selectable>();

			if (select == null)
				return;

			select.image.sprite = normal;
			select.image.type = type;
			select.transition = Selectable.Transition.SpriteSwap;

			SpriteState spriteState = select.spriteState;
			spriteState.highlightedSprite = highlight;
			spriteState.pressedSprite = active;
			spriteState.disabledSprite = inactive;
			select.spriteState = spriteState;
		}

		private void setSelectable(Sprite normal)
		{
			Selectable select = GetComponent<Selectable>();

			if (select == null)
				return;

			select.image.sprite = normal;
			select.image.type = Image.Type.Sliced;
			select.transition = Selectable.Transition.None;
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

        public void setImage(Sprite sprite)
        {
            Image image = GetComponent<Image>();

            if (image == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }

        public void setButton(Sprite normal, Sprite highlight, Sprite active, Sprite inactive, Image.Type type = Image.Type.Sliced)
		{
			setSelectable(normal, highlight, active, inactive, type);
		}

		public void setToggle(Sprite normal, Sprite checkmark)
		{
			setSelectable(normal);

			Toggle toggle = GetComponent<Toggle>();

			if (toggle == null)
				return;

			Image toggleImage = toggle.graphic as Image;

			if (toggleImage == null)
				return;

			toggleImage.sprite = checkmark;
			toggleImage.type = Image.Type.Sliced;
		}

        public void setScrollbar(Sprite background, Sprite thumb)
        {
            Image back = GetComponent<Image>();

            if (back == null)
                return;

            back.sprite = background;

            Scrollbar scroll = GetComponent<Scrollbar>();

            if (scroll == null)
                return;

            if (scroll.targetGraphic == null)
                return;

            Image scrollThumb = scroll.targetGraphic.GetComponent<Image>();

            if (scrollThumb == null)
                return;

            scrollThumb.sprite = thumb;
        }
    }
}
