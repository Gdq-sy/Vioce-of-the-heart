using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Exit : MonoBehaviour, IPointerClickHandler
{
    public GameObject targetObject;
    public static bool BlockNextClick = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        BlockNextClick = true;
        if (targetObject != null)
        {
            targetObject.SetActive(false);
        }
    }
}
