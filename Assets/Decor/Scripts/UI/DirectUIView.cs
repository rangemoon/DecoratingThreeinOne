using DG.Tweening;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Decor
{
    public class DirectUIView : MonoBehaviour
    {
        public GameObject currencyPanel;
        public GameObject playButton;
        public GameObject roomViewButton;
        public GameObject settingButton;
        public GameObject fanpageButton;
        public GameObject editItemsButton;
        public GameObject roomPageView;
        public GameObject fullIconsCanvas;
        public GameObject freeGemAd;
        public GameObject dailyBonus;
        public GameObject starterPack;
        public GameObject piggyBank;
        public GameObject dailyQuest;
        public GameObject removeAds;
        public GameObject spinButton;

        public float animateDuration = 0.25f;

        private UIEdgeSnapPosition currencySnapPos;
        private UIEdgeSnapPosition playSnapPos;
        private UIEdgeSnapPosition roomViewSnapPos;
        private UIEdgeSnapPosition editItemsSnapPos;
        private UIEdgeSnapPosition settingSnapPos;
        private UIEdgeSnapPosition fanpageSnapPos;
        private UIEdgeSnapPosition freeGemAdSnapPos;
        private UIEdgeSnapPosition dailyBonusSnapPos;
        private UIEdgeSnapPosition starterPackSnapPos;
        private UIEdgeSnapPosition piggyBankSnapPos;
        private UIEdgeSnapPosition dailyQuestSnapPos;
        private UIEdgeSnapPosition removeAdsSnapPos;
        private UIEdgeSnapPosition spinButtonSnapPos;

        private RectTransform currencyPanelRectTransform;
        private RectTransform playButtonRectTransform;
        private RectTransform roomViewButtonRectTransform;
        private RectTransform editItemsButtonRectTransform;
        private RectTransform settingButtonRectTransform;
        private RectTransform fanpageButtonButtonRectTransform;

        private RectTransform freeGemAdRectTransform;
        private RectTransform dailyBonusRectTransform;
        private RectTransform starterPackRectTransform;
        private RectTransform piggyBankRectTransform;
        private RectTransform dailyQuestRectTransform;
        private RectTransform removeAdsRectTransform;
        private RectTransform spinButtonRectTransform;

        void Awake()
        {               
            roomViewButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioManager.Instance.PlaySFX(AudioClipId.PanelIn);
                EventDispatcher<UITransitionEventId>.Instance.NotifyEvent(UITransitionEventId.ShowRoomView);
            });

            editItemsButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioManager.Instance.PlaySFX(AudioClipId.PanelIn);
                EventDispatcher<UITransitionEventId>.Instance.NotifyEvent(UITransitionEventId.ShowEdit);
            });

            settingButton.GetComponent<Button>().onClick.AddListener(() => 
            {                   
                Popup.PopupSystem.GetOpenBuilder()
                .SetType(PopupType.PopupSetting)
                .Open();
            });
            fanpageButton.gameObject.SetActive(false);

            dailyBonus.GetComponent<Button>().onClick.AddListener(() =>
            {
                Popup.PopupSystem.GetOpenBuilder().
                   SetType(PopupType.PopupDailyBonus).
                   SetCurrentPopupBehaviour(Popup.CurrentPopupBehaviour.Close).
                   SetBackBlockerEvent(null).
                   Open();
            });
           
            currencyPanelRectTransform = currencyPanel.GetComponent<RectTransform>();
            playButtonRectTransform = playButton.GetComponent<RectTransform>();
            roomViewButtonRectTransform = roomViewButton.GetComponent<RectTransform>();
            editItemsButtonRectTransform = editItemsButton.GetComponent<RectTransform>();
            settingButtonRectTransform = settingButton.GetComponent<RectTransform>();
            fanpageButtonButtonRectTransform = fanpageButton.GetComponent<RectTransform>();
            freeGemAdRectTransform = freeGemAd.GetComponent<RectTransform>();
            dailyBonusRectTransform = dailyBonus.GetComponent<RectTransform>();
            starterPackRectTransform = starterPack.GetComponent<RectTransform>();
            piggyBankRectTransform = piggyBank.GetComponent<RectTransform>();
            dailyQuestRectTransform = dailyQuest.GetComponent<RectTransform>();
            removeAdsRectTransform = removeAds.GetComponent<RectTransform>();
            spinButtonRectTransform = spinButton.GetComponent<RectTransform>();

            currencySnapPos = new UIEdgeSnapPosition(currencyPanelRectTransform, new Vector2(0f, 1.2f));
            playSnapPos = new UIEdgeSnapPosition(playButtonRectTransform, new Vector2(0f, -1.2f));
            roomViewSnapPos = new UIEdgeSnapPosition(roomViewButtonRectTransform, new Vector2(0f, -1.2f));
            editItemsSnapPos = new UIEdgeSnapPosition(editItemsButtonRectTransform, new Vector2(0f, -1.2f));
            settingSnapPos = new UIEdgeSnapPosition(settingButtonRectTransform, new Vector2(0f, 1.2f));
            fanpageSnapPos = new UIEdgeSnapPosition(fanpageButtonButtonRectTransform, new Vector2(0f, 1.2f));
            freeGemAdSnapPos = new UIEdgeSnapPosition(freeGemAdRectTransform, new Vector2(1.2f, 0f));
            dailyBonusSnapPos = new UIEdgeSnapPosition(dailyBonusRectTransform, new Vector2(0f, -1.2f));
            starterPackSnapPos = new UIEdgeSnapPosition(starterPackRectTransform, new Vector2(-1.2f, 0f));
            piggyBankSnapPos = new UIEdgeSnapPosition(piggyBankRectTransform, new Vector2(1.2f, 0f));
            dailyQuestSnapPos = new UIEdgeSnapPosition(dailyQuestRectTransform, new Vector2(0f, -1.2f));
            removeAdsSnapPos = new UIEdgeSnapPosition(removeAdsRectTransform, new Vector2(-1.2f, 0f));
            spinButtonSnapPos = new UIEdgeSnapPosition(spinButtonRectTransform, new Vector2(1.2f, 0f));

            starterPack.SetActive(false);
            piggyBank.SetActive(false);
            removeAds.SetActive(false);
        }

        void Start()
        {
            playButton.transform.GetChild(0).GetComponent<Text>().text =
                PlayerData.current.match3Data.level.ToString();
        }

        public void Show()
        {
            currencySnapPos.Show(animateDuration);
            playSnapPos.Show(animateDuration);
            roomViewSnapPos.Show(animateDuration);
            editItemsSnapPos.Show(animateDuration);
            settingSnapPos.Show(animateDuration);
            fanpageSnapPos.Show(animateDuration);
            freeGemAdSnapPos.Show(animateDuration);
            dailyBonusSnapPos.Show(animateDuration);
            starterPackSnapPos.Show(animateDuration);
            piggyBankSnapPos.Show(animateDuration);
            dailyQuestSnapPos.Show(animateDuration);
            removeAdsSnapPos.Show(animateDuration);
            spinButtonSnapPos.Show(animateDuration);

            if (PlayerData.current.noAds == true)
                removeAds.SetActive(false);
        }

        public void Hide(bool withCurrencyPanel = true)
        {
            if (withCurrencyPanel)
                currencySnapPos.Hide(animateDuration);

            playSnapPos.Hide(animateDuration);
            roomViewSnapPos.Hide(animateDuration);
            editItemsSnapPos.Hide(animateDuration);
            settingSnapPos.Hide(animateDuration);
            fanpageSnapPos.Hide(animateDuration);
            freeGemAdSnapPos.Hide(animateDuration);
            dailyBonusSnapPos.Hide(animateDuration);
            starterPackSnapPos.Hide(animateDuration);
            piggyBankSnapPos.Hide(animateDuration);
            dailyQuestSnapPos.Hide(animateDuration);
            removeAdsSnapPos.Hide(animateDuration);
            spinButtonSnapPos.Hide(animateDuration);

        }   
    }
}

