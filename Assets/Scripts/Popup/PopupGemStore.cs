using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using Popup;

public class PopupGemStore : PopupBase
{
    [Header("Refs")]
	public BundlePack[] bundlePacks;

    public Image closeButton;

    [Header("Open Animation")]
    public AnimationCurve scaleUpCurve;

    public float scaleUpDuration = 0.5f;

    public float fadeInDuration = 0.5f;

    public float openDelayInterval = 0.1f;

    [Header("Close Animation")]
    public AnimationCurve scaleDownCurve;

    public AnimationCurve fadeOutCurve;

    public float closeDuration = 0.35f;

    public float closeDelayInterval = 0.1f;

    private BundlePack currentPack;

    private float openTime;

    private void Start()
    {
        for (int i = 0; i < bundlePacks.Length; i++)
        {
            bundlePacks[i].SetPrice("$"+bundlePacks[i].priceInUsd);
            bundlePacks[i].BuyEvent = PurchaseButtonClick;
            bundlePacks[i].index = i;  
        }

        openTime = Time.realtimeSinceStartup;

        //AppEventTracker.LogEventShop(Analytics.Feature_SHOP.ACTION_TYPE._start);
    }

    public override void Show()
	{
        canClose = false;

		for (int i = 0; i < bundlePacks.Length; i++)
        {
            var transform = bundlePacks[i].transform;
            var canvasGroup = transform.GetComponent<CanvasGroup>();
            float delay = (i != 4) ? i * openDelayInterval : (i - 1) * openDelayInterval;

            float scale = transform.localScale.x;
            transform.localScale = Vector3.zero;
            var tween = transform.DOScale(scale, scaleUpDuration).SetDelay(delay).SetEase(scaleUpCurve);

            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeInDuration).SetDelay(delay).SetEase(Ease.Linear);

            if (i == 4)
            {
                tween.OnComplete(() => canClose = true);
            }
        }

        canClose = true;
    }

	public override void Close(bool forceDestroying = true)
	{
        TerminateInternal();

        // float openDuration = Time.realtimeSinceStartup - openTime;
        //AppEventTracker.LogEventShop(Analytics.Feature_SHOP.ACTION_TYPE._end,
        //    Analytics.Feature_SHOP.ACTION_NAME.NONE,
        //    Analytics.Feature_SHOP.TYPE_ITEM.NONE,
        //    openDuration);

        //float maxDelayDuration = (bundlePacks.Length - 1) * closeDelayInterval;

        //for (int i = 0; i < bundlePacks.Length; i++)
        //{
        //    var transform = bundlePacks[i].transform;
        //    var canvasGroup = transform.GetComponent<CanvasGroup>();
        //    float delay = maxDelayDuration - ((i != 4) ? i * openDelayInterval : (i - 1) * openDelayInterval);

        //    var tween = transform.DOScale(0f, closeDuration).SetDelay(delay).SetEase(scaleDownCurve);
        //    canvasGroup.DOFade(0f, closeDuration - 1f / 30f).SetDelay(delay).SetEase(fadeOutCurve);

        //    if (i == 0)
        //    {
        //        tween.OnComplete(() => TerminateInternal(forceDestroying));
        //    }
        //}
    }

    public void PurchaseButtonClick(BundlePack gemPack)
    {
        PopupUtility.OpenPopupLiteMesage("当前版本未开放购买");
    }
}
