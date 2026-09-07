using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using Popup;
using System.Globalization;
using System.Text.RegularExpressions;

public class PopupStarterPack : PopupBase
{
	public float priceInUsd;

    public float discountPercent;

	public Text priceText;

	public Text fakePriceText;

	public Transform lightTransform;

    public override void Show()
	{
		canClose = false;
		PopupAnimationUtility.AnimateScale(transform, Ease.OutBack, 0.25f, 1f, 0.25f, 0f).OnComplete(() => canClose = true);

		if (lightTransform)
			StartCoroutine(LightRotate());

		priceText.text = "$"+priceInUsd;
		float fakePrice = (priceInUsd) / (100f - discountPercent) * 100f;
		fakePriceText.text =  "$"+fakePrice;
    }

	public override void Close(bool forceDestroying = true)
	{
		TerminateInternal(forceDestroying);
	}

	public void BuyButtonPress()
	{
	    PopupUtility.OpenPopupLiteMesage("当前版本未开放购买");
	}

	private IEnumerator LightRotate()
    {
		while (true)
        {
			lightTransform.Rotate(new Vector3(0f, 0f, -30f * Time.deltaTime), Space.Self);

			yield return null;
		}		
    }


}

public static class StarterPackUtility
{
	public static int match3LevelToShow = 10;

	public static int expiredDuration = 24 * 3600;

	public static int GetRemainingTimeInSeconds()
    {
		var playerData = PlayerData.current;
		
		if (string.IsNullOrEmpty(playerData.starterPackExpiredTime))
        {
			SaveExpireTime();
			return expiredDuration;
		}
        else 
        {
			DateTime now = DateTimeUtility.GetUtcNow();
			DateTime expiredTime = DateTimeUtility.Get(playerData.starterPackExpiredTime);
			if (expiredTime > now)
            {
				TimeSpan timeSpan = expiredTime - now;

				return (int)timeSpan.TotalSeconds;
            }
            else
            {
				SaveExpireTime();
				return expiredDuration;
			}			
        }
	}

	public static void SaveExpireTime()
    {
		DateTime now = DateTimeUtility.GetUtcNow();

		PlayerData.current.starterPackExpiredTime = now.AddSeconds(expiredDuration).ToString();
	}

	public static void ResetExpireTime()
    {
		PlayerData.current.starterPackExpiredTime = "";
	}

	public static bool Available()
    {
		var playerData = PlayerData.current;
		return playerData.match3Data.level >= match3LevelToShow && playerData.purchasedStartPack == false;
	}
}
