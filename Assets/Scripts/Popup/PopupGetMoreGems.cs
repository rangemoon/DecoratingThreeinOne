using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using Popup;

public class PopupGetMoreGems : PopupBase
{
    public Transform mainTransform;

    public GemStorePack[] gemPack;

    public Text gemCountText;

    public Image gemImage;

    public CollectParticles collectParticles;

    private GemStorePack currentPack;

    private PlayerData playerData;

    private bool canPlayAddCoinSFX;

    public override void Show()
	{
        playerData = PlayerData.current;

        gemCountText.text = playerData.gemCount.ToString();
        mainTransform.GetComponent<CanvasGroup>().alpha = 1f;
        canClose = false;

		PopupAnimationUtility.AnimateScale(mainTransform, Ease.OutBack, 0.75f, 1f, 0.25f, 0f).OnComplete(() => canClose = true);
	}

	public override void Close(bool forceDestroying = true)
	{
		PopupAnimationUtility.AnimadeAlpha(mainTransform.GetComponent<CanvasGroup>(), Ease.Linear, 1f, 0f, 0.1f, 0f, false);
		PopupAnimationUtility.AnimateScale(mainTransform, Ease.OutQuart, 1f, 0.8f, 0.1f, 0f)
			.OnComplete(() =>
			{
				TerminateInternal(forceDestroying);
			});
	}

	public void Start()
	{
        for (int i = 0; i < gemPack.Length; i++)
        {
            gemPack[i].SetPrice("$"+gemPack[i].priceInUsd);
            gemPack[i].BuyEvent = PurchaseButtonClick;
        }
    }
   
    public void PurchaseButtonClick(GemStorePack gemPack)
    {
        PopupUtility.OpenPopupLiteMesage("当前版本未开放购买");
    }



    public IEnumerator PlayAddGemSFXCoroutine()
    {
        var waitForInterval = new WaitForSeconds(0.15f);

        canPlayAddCoinSFX = true;
        while (canPlayAddCoinSFX)
        {
            AudioManager.Instance.PlaySFX(AudioClipId.AddCoin);

            yield return waitForInterval;
        }
    }
}

