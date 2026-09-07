using System;
using UnityEngine;

public class AdvertisementManager : MonoSingleton<AdvertisementManager>
{
    public Action RewardedVideoSucessfulExtraEvent;

    public static int Ads
    {
        get => PlayerPrefs.GetInt("Ads");
        set => PlayerPrefs.SetInt("Ads", value);
    }

    // Keep ad-gated buttons available while the project uses the no-ad fallback.
    public bool IsRewardedVideoAvailable => true;

    public void ShowInterstitial(Action onClose = null, Action successEvent = null, Action failEvent = null)
    {
        onClose?.Invoke();
    }

    public void ShowRewardedVideo(Action successEvent, Action failEvent = null)
    {
        successEvent?.Invoke();

        if (PlayerData.current != null && PlayerData.current.tempData != null)
        {
            PlayerData.current.tempData.rewardedVideoCount++;
        }

        RewardedVideoSucessfulExtraEvent?.Invoke();
    }
}