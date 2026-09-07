using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StarterPackButton : MonoBehaviour
{
    public Transform lightingTransform;

    public Button button;

    public Text starterPackTimeText;

    private Coroutine timeCoroutine;

    void Start()
    {
        // 付费新手礼包已下线：永不显示入口（GAME_FLOW_AD_POINTS.md 第4节）。
        gameObject.SetActive(false);
    }

    public void StarterPackButtonPressed()
    {
        // 付费新手礼包已下线：不再创建 PopupStarterPack（GAME_FLOW_AD_POINTS.md 第4节）。
        return;
    }

    public void OnApplicationFocus(bool focus)
    {
        if (timeCoroutine != null) StopCoroutine(timeCoroutine);
        timeCoroutine = StartCoroutine(TimeUpdate());
    }

    public void OnApplicationPause(bool pause)
    {
        if (timeCoroutine != null) StopCoroutine(timeCoroutine);
        timeCoroutine = StartCoroutine(TimeUpdate());
    }

    public IEnumerator TimeUpdate()
    {
        var waitFor1s = new WaitForSeconds(1f);
        var stringBuilder = new StringBuilder();

        float remainingTime = StarterPackUtility.GetRemainingTimeInSeconds();
        float bias = remainingTime - (int)remainingTime;
        remainingTime -= bias;

        DateTimeUtility.ToHourMinuteSecond(stringBuilder, (int)remainingTime);
        starterPackTimeText.text = stringBuilder.ToString();

        yield return new WaitForSeconds(bias);

        while (true)
        {
            DateTimeUtility.ToHourMinuteSecond(stringBuilder, (int)remainingTime);
            starterPackTimeText.text = stringBuilder.ToString();

            remainingTime -= 1f;

            yield return waitFor1s;

            if (remainingTime <= 0f)
            {
                gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        lightingTransform.Rotate(new Vector3(0f, 0f, -30f * Time.deltaTime), Space.Self);
    }
}
