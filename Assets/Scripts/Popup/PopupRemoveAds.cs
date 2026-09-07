using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Popup;
using System.Text;

public class PopupRemoveAds : PopupBase
{
    private static float priceUsd = 1.99f;
    public Text priceText;

    public override void Show()
    {
        priceText.text = "$" + priceUsd;
        canClose = false;
        canvasGroup.alpha = 1f;
        PopupAnimationUtility.AnimateScale(transform, Ease.OutBack, 0.25f, 1f, 0.25f, 0f).OnComplete(() => canClose = true);
    }

    public override void Close(bool forceDestroying = true)
    {
        TerminateInternal(forceDestroying);
    }

    public void PressBuyButton()
    {
        // XH SDK - 去广告（第4节）：属永久权益，不适合一次视频永久解锁，应直接隐藏入口（已在 DirectUIView 隐藏 removeAds）。此处保留按钮仅为占位，不再走诡计宝箱/激励视频。
        PopupUtility.OpenPopupLiteMesage("当前版本未开放购买");
    }


}


