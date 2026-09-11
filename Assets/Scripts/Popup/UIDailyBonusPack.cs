using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class UIDailyBonusPack : MonoBehaviour
{
    public Button x2RewardButton;

    public Button claimButton;

    public Image selectImage;

    public GameObject claimedObject;

    public Text dayText;

    public Text comingsoonText;

    public void SetDay(int day)
    {
        dayText.text = "第" + day.ToString() + "天";
    }

    public void SetCallback(UnityAction claimEvent, UnityAction x2rewardAction)
    {
        selectImage.GetComponent<Button>().onClick.AddListener(claimEvent);
        if (claimButton != null && claimButton != selectImage.GetComponent<Button>())
            claimButton.onClick.AddListener(claimEvent);
        x2RewardButton.onClick.AddListener(x2rewardAction);
        HideX2Button();
    }

    public void SetAsCurrent()
    {
        selectImage.gameObject.SetActive(true);
        claimedObject.SetActive(false);
        comingsoonText.gameObject.SetActive(false);
        HideX2Button();
    }

    public void SetAsNext()
    {
        selectImage.gameObject.SetActive(false);
        claimedObject.SetActive(false);
        comingsoonText.gameObject.SetActive(true);
        HideX2Button();
    }

    public void SetAsClaimed()
    {
        selectImage.gameObject.SetActive(false);
        claimedObject.SetActive(true);
        comingsoonText.gameObject.SetActive(false);
        HideX2Button();
    }

    /// <summary>
    /// 隐藏看广告翻倍按钮，签到卡片交互与 D1 的点击领取保持一致。
    /// </summary>
    private void HideX2Button()
    {
        if (x2RewardButton != null)
            x2RewardButton.gameObject.SetActive(false);
    }
}
