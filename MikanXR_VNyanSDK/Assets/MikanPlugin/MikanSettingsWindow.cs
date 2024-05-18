using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MikanXRPlugin
{
    class MikanSettingsWindow : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        public RectTransform dragRect;

        public void OnDrag(PointerEventData eventData)
        {
            if (dragRect != null)
            {
				dragRect.anchoredPosition += eventData.delta;
			}
		}

        public void OnPointerDown(PointerEventData eventData)
        {
            if (dragRect != null)
            {
				dragRect.SetAsLastSibling();
			}
		}
    }
}
