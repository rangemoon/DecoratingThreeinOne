using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeGemAdsButton : MonoBehaviour
{
    public void ButtonPressed()
    {
        Popup.PopupSystem.GetOpenBuilder()
            .SetType(PopupType.PopupFreeGemAds)
            .Open();
    }
}
