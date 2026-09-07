using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using Popup;
using System.Text;

public class PopupPiggyBank : PopupBase
{
	public Button playButton;

	public Button buyButton;

    public Text priceText;

    public GameObject pigNormal;

    public GameObject pigFull;

    public Transform lightTransform;

    [Header("Time")]
	public GameObject timeObject;

    public Text timeText;

    [Header("Progress")]
	public Text minGemText;

	public Text maxGemText;

	public Image progressImage;

	[Header("Text")]
    public Text gemText;

    public Text smashText;

	public Text normalText;

    private Coroutine timeUpdateCoroutine;

	private PiggyBankData piggyBankData;

    private string productId;

    private float priceUsd;

    private void Start()
    {
		piggyBankData = PlayerData.current.piggyBankData;
        productId = PiggyBankUtility.GetIapProductId();
        priceUsd = PiggyBankUtility.GetIapDefaultPriceUsd();

        gemText.text = piggyBankData.gemCount.ToString();
		IntegerRange smashRange = PiggyBankUtility.GetSmashRange(piggyBankData.level);
		minGemText.text = smashRange.min.ToString();
		maxGemText.text = smashRange.max.ToString();

		progressImage.fillAmount = (float)piggyBankData.gemCount / smashRange.max;

		float percentOfMin = (float)smashRange.min / smashRange.max;
		minGemText.rectTransform.anchoredPosition = 
			new Vector2(progressImage.rectTransform.rect.width * percentOfMin, minGemText.rectTransform.anchoredPosition.y);

		bool canSmash = PiggyBankUtility.CanSmash();

		playButton.gameObject.SetActive(!canSmash);
		buyButton.gameObject.SetActive(canSmash);

		normalText.gameObject.SetActive(!canSmash);
		smashText.gameObject.SetActive(canSmash);

        pigNormal.SetActive(!canSmash);
        pigFull.SetActive(canSmash);

        lightTransform.gameObject.SetActive(canSmash);

        if (canSmash == false)
		{
			timeObject.SetActive(false);
			normalText.text = normalText.text.Replace("300", smashRange.min.ToString());    
        }
		else 
		{
            StartCoroutine(LightRotate());
            timeUpdateCoroutine = StartCoroutine(TimeUpdate());
            priceText.text = "$" + priceUsd;
        }
	}

    public override void Show()
	{
        canClose = false;
        PopupAnimationUtility.AnimateScale(transform, Ease.OutBack, 0.25f, 1f, 0.25f, 0f).OnComplete(() => canClose = true);
    }

	public override void Close(bool forceDestroying = true)
	{
		TerminateInternal(forceDestroying);
	}

    private IEnumerator LightRotate()
    {
        while (true)
        {
            lightTransform.Rotate(new Vector3(0f, 0f, 30 * Time.deltaTime), Space.Self);

            yield return null;
        }
    }

    public void PressPlayButton()
	{
		Popup.PopupSystem.GetOpenBuilder().
						SetType(PopupType.PopupGameStart).
						SetCurrentPopupBehaviour(Popup.CurrentPopupBehaviour.Close).
						Open();
	}

	public void PressBuyButton()
    {
        PopupUtility.OpenPopupLiteMesage("当前版本未开放购买");
    }

    public void BuyButtonPress()
    {
        PopupUtility.OpenPopupLiteMesage("当前版本未开放购买");
    }



    public void OnApplicationFocus(bool focus)
    {
        if (timeUpdateCoroutine != null)
        {
            StopCoroutine(timeUpdateCoroutine);
            timeUpdateCoroutine = null;
        }

        if (PiggyBankUtility.CanSmash())
        {
            timeUpdateCoroutine = StartCoroutine(TimeUpdate());
        }
    }

    public void OnApplicationPause(bool pause)
    {
        if (timeUpdateCoroutine != null)
        {
            StopCoroutine(timeUpdateCoroutine);
            timeUpdateCoroutine = null;
        }

        if (PiggyBankUtility.CanSmash())
        {
            timeUpdateCoroutine = StartCoroutine(TimeUpdate());
        }
    }

    public IEnumerator TimeUpdate()
    {
        timeText.gameObject.SetActive(true);

        float timeToPeriodExpire = PiggyBankUtility.Get_RemainingTime_To_PeriodExpired();

        StringBuilder sb = new StringBuilder();
        var waitFor1s = new WaitForSeconds(1f);

        DateTimeUtility.ToHourMinuteSecond(sb, (int)timeToPeriodExpire);
        timeText.text = sb.ToString();

        float bias = timeToPeriodExpire - (int)timeToPeriodExpire;
        timeToPeriodExpire -= bias;
        yield return new WaitForSeconds(bias);

        while (true)
        {
            DateTimeUtility.ToHourMinuteSecond(sb, (int)timeToPeriodExpire);
            timeText.text = sb.ToString();

            timeToPeriodExpire -= 1f;
            yield return waitFor1s;

            if (timeToPeriodExpire <= 0f)
            {
                timeText.text = "00:00:00";
                break;
            }
        }

        timeUpdateCoroutine = null;
    }
}