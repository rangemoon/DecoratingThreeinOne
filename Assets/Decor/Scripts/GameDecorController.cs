using System;
using System.Collections;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Threading;

namespace Decor {
    public class GameDecorController : MonoBehaviour {
        public CameraSetup cameraSetup;

        public DesignController designController;

        public TutorialController tutorialController;

        public DialogueController dialogueController;

        public PostDecorCompleteEffect postDecorCompleteEffect;

        public GameUITransitionDirector uiTransitionDirector;

        public Camera uiCamera;

        [Header("Random Dialogs")]
        public DialogueData randomUnlockDialogs;

        private RoomData roomData;

        private UnlockedRoomData currentRoomData;

        private DesignItemView[] items;

        private IconTouchHandler tutorialIconTouchHandler;

        private int randomUnlockDialogsCount = 0;

        void Awake() {
            //Model.Instance.Load();

            PlayerData playerData = PlayerData.current;

            roomData = RoomDataTable.Instance.GetRoomDataWithId(playerData.homeDesignData.currentRoomId);
            currentRoomData = playerData.homeDesignData.GetCurrentUnlockedRoomData();
            currentRoomData.enterTimeCount++;

            // StartCoroutine(LoadRoomAndInitialize());
        }

        private void Start() {
            StartCoroutine(LoadRoomAndInitialize());
            var playerData = PlayerData.current;

            EventDispatcher<GlobalEventId>.Instance.NotifyEvent(GlobalEventId.CoinChange, playerData.cointCount);
            EventDispatcher<GlobalEventId>.Instance.NotifyEvent(GlobalEventId.GemChange, playerData.gemCount);

            if (!AudioManager.Instance.musicSource.isPlaying)
                AudioManager.Instance.PlayMusic(AudioClipId.DecorMusic);

            AudioManager.Instance.CrossIn(1f);

            if (!Popup.PopupSystem.IsInstanceDestroyed && Popup.PopupSystem.IsInstanceExisting) {
                Popup.PopupSystem.Instance.ShowPopupEvent += OnPopupShow;
                Popup.PopupSystem.Instance.ClearPopupEvent += OnPopupClear;
            }

            HandleStartPopups();
        }

        private void OnDestroy() {
            if (!Popup.PopupSystem.IsInstanceDestroyed && Popup.PopupSystem.IsInstanceExisting) {
                Popup.PopupSystem.Instance.ShowPopupEvent -= OnPopupShow;
                Popup.PopupSystem.Instance.ClearPopupEvent -= OnPopupClear;
            }

            EventDispatcher<UITransitionEventId>.Instance.ClearEvent();
            EventDispatcher<GlobalEventId>.Instance.ClearEvent();

            //AudioManager.Instance.StopMusic();
            PrepareBgTextureEvent = null;

            if (roomData != null && RoomAssetBundleManager.IsInstanceExisting)
                RoomAssetBundleManager.Instance.UnloadRoom(roomData.assetBundleName);
        }
        
        /// <summary>
        /// 从房间 AssetBundle 加载 Prefab 并初始化装饰系统。
        /// </summary>
        private IEnumerator LoadRoomAndInitialize() {
            bool roomReady = false;
            yield return RoomAssetBundleManager.Instance.EnsureRoomReady(roomData.assetBundleName, success => roomReady = success);
            if (!roomReady) {
                Debug.LogError("Room bundle failed: " + roomData.assetBundleName);
                enabled = false;
                yield break;
            }

            GameObject roomPrefab = RoomAssetBundleManager.Instance.LoadRoomPrefab(roomData.assetBundleName);
            if (!roomPrefab) {
                Debug.LogError("Room prefab is missing: " + roomData.assetBundleName + "/" + RoomData.RoomPrefabName);
                enabled = false;
                yield break;
            }

            var roomGameobject = Instantiate(roomPrefab);
            var itemsParentTransform = roomGameobject.transform.Find(RoomData.ItemsOfRoomName);
            if (!itemsParentTransform) {
                Debug.LogError("Room prefab is missing the items container: " + roomData.assetBundleName);
                enabled = false;
                yield break;
            }

            var itemsParent = itemsParentTransform.transform;

            var roomFitScreen = roomGameobject.GetComponent<RoomFitScreen>();
            if (roomFitScreen) {
                cameraSetup.Execute(
                    roomFitScreen.minCameraRatio, roomFitScreen.maxCameraOrthoSize,
                    roomFitScreen.maxCameraRatio, roomFitScreen.minCameraOrthoSize);
            } else {
                cameraSetup.Execute();
            }

            int itemCount = itemsParent.childCount;
            if (roomData.maxItemCount != itemCount) {
                Debug.LogWarning("Room Data Max Item Count and Item Count are not match");
                roomData.maxItemCount = itemCount;
            }

            items = new DesignItemView[itemCount];
            DesignItemData[] itemDataArray = new DesignItemData[itemCount];
            for (int i = 0; i < itemCount; i++) {
                items[i] = itemsParent.GetChild(i).GetComponent<DesignItemView>();
                items[i].GetIcon().GetComponent<SpriteRenderer>().sortingLayerName = "Icon";
                itemDataArray[i] = items[i].primaryData;

                if (itemDataArray[i].unlockedCountToUnlock >= 100)
                    itemDataArray[i].unlockedCountToUnlock = 0;

                // 使用不受本地化影响的道具 ID 定位沙发，确保 AB 房间中的教程目标稳定可用。
                if (items[i].primaryData.id == "c1_r1_sofa") {
                    tutorialIconTouchHandler = items[i].GetIcon().GetComponent<IconTouchHandler>();
                }
            }

            Model.Instance.UpdateCurrentRoomItemData(itemDataArray);

            // setup and bind dependencies
            designController.Initialize(items);
            designController.roomName = roomData.name;
            designController.IsTouchEnabled = true;
            PrepareBgTextureEvent = () => designController.SetActiveIconsVisible(false, false);

            dialogueController.Initialize();

            uiTransitionDirector.SetActiveIconsVisible = designController.SetActiveIconsVisibleWithAnim;

            designController.UnlockItemFinishEvent = OnItemUnlocked;
            designController.UnlockItemFailedEvent = (item) => {
                Popup.PopupSystem.Instance.ShowPopup(PopupType.PopupRequirePlayMatch3, Popup.CurrentPopupBehaviour.Close, true, true)
                .AcceptEvent = (param) => ShowPopupMatch3Preparing();
            };

            // check if next room can be unlocked (in case we update database)
            CheckCanUnlockNextRoom();

            // 教程必须等房间和教程点击目标初始化完成后再启动。
            TryStartFirstLivingRoomTutorial();
        }

        /// <summary>
        /// 在房间初始化完成后启动首次进入客厅的对话和新手教程。
        /// </summary>
        private void TryStartFirstLivingRoomTutorial() {
            if (currentRoomData.boughtItemData.Count != 0 || currentRoomData.enterTimeCount != 1 ||
                Model.Instance.playRoomData.id != 1) {
                return;
            }

            if (!tutorialIconTouchHandler) {
                // 目标缺失时不创建会拦截输入的教程任务，避免玩家永久卡住。
                Debug.LogError("[新手教程] 房间 AB 中找不到客厅沙发的 IconTouchHandler，已跳过教程以保留游戏操作。");
                return;
            }

            dialogueController.FinishEvent += tutorialController.OnDialogFinish;
            ShowStartDialogs();
            tutorialController.ScheduleFirstTime_EnterLivingRoom(tutorialIconTouchHandler);
        }

        private void OnPopupShow(Popup.PopupBase popup) {
            uiTransitionDirector.ForceCloseAllView(false);
            designController.SetActiveIconsVisibleWithAnim(false);
        }

        private void OnPopupClear() {
            uiTransitionDirector.ForceCloseAllView(true);
            designController.SetActiveIconsVisibleWithAnim(true);
        }

        private void OnItemUnlocked(DesignItemView item) {
            CheckCanUnlockNextRoom();

            bool playUnlockDialog = true;

            if (!ShowUnlockItemDialogs(item)) {
                if (randomUnlockDialogsCount == 0) {
                    playUnlockDialog = ShowRandomUnlockDialogs();
                    randomUnlockDialogsCount = UnityEngine.Random.Range(1, 3);
                } else {
                    randomUnlockDialogsCount--;
                    playUnlockDialog = false;
                }
            }

            if (designController.IsAllItemsUnlock()) {
                if (currentRoomData.roomId == 1) {
                    // AppEventTracker.PushEventFinishRoom1();
                    // Firebase.Analytics.FirebaseAnalytics.LogEvent("Feature_selected_unlockroom1");
                }

                if (playUnlockDialog) {
                    dialogueController.CombineCurrentWithDialogs(roomData.completeDialogs);
                    dialogueController.FinishEvent = PlayPostDecorCompleteEffect;
                } else {
                    if (ShowEndDialogs())
                        dialogueController.FinishEvent = PlayPostDecorCompleteEffect;
                    else
                        PlayPostDecorCompleteEffect();
                }
            }
        }

        private void HandleStartPopups() {
            if (GameMain.played == false) return;

            var playerData = PlayerData.current;
            bool dailyBonusAvailable = DailyBonusUtility.Available() && playerData.match3Data.level >= 4;
            bool ratingAvailable = playerData.appRated == false && (playerData.match3Data.level + 20 - 6) % 20 == 0;

            if (ratingAvailable) {
                Popup.PopupSystem.GetOpenBuilder().
                    SetType(PopupType.PopupRating).
                    SetCurrentPopupBehaviour(Popup.CurrentPopupBehaviour.HideTemporary).
                    SetBackBlockerEvent(null).
                    SetDelayTime(1.5f).
                    Open();
            } else if (dailyBonusAvailable) {
                Popup.PopupSystem.GetOpenBuilder().
                    SetType(PopupType.PopupDailyBonus).
                    SetCurrentPopupBehaviour(Popup.CurrentPopupBehaviour.HideTemporary).
                    SetDelayTime(1.5f).
                    SetBackBlockerEvent(null).
                    Open();
            } else if (UnityEngine.Random.Range(0, 100) < 50 && ServiceUtility.InternetAvailable) {
                Popup.PopupSystem.GetOpenBuilder().
                    SetType(PopupType.PopupFreeGemAds).
                    SetCurrentPopupBehaviour(Popup.CurrentPopupBehaviour.HideTemporary).
                    SetDelayTime(1.5f).
                    Open();
            }
        }

        private bool ShowStartDialogs() {
            if (roomData.startDialogs != null && roomData.startDialogs.Length > 0) {
                dialogueController.ShowDialogues(roomData.startDialogs, 1.5f);
                return true;
            }

            return false;
        }

        private bool ShowEndDialogs() {
            if (roomData.completeDialogs != null && roomData.completeDialogs.Length > 0) {
                dialogueController.ShowDialogues(roomData.completeDialogs, 1.5f);
                return true;
            }

            return false;
        }

        private bool ShowRandomUnlockDialogs() {
            dialogueController.ShowDialogues(new Dialogue[] { randomUnlockDialogs.GetRandom() }, 1f);

            return true;
        }

        private bool ShowUnlockItemDialogs(DesignItemView item) {
            var itemDialogComponent = item.GetComponent<DesignItemDialogue>();

            if (itemDialogComponent != null && itemDialogComponent.dialogues != null && itemDialogComponent.dialogues.Length != 0) {
                dialogueController.ShowDialogues(itemDialogComponent.dialogues, 1.0f);
                return true;
            }

            return false;
        }

        private void PlayPostDecorCompleteEffect() {
            AudioSource musicSource = AudioManager.Instance.musicSource;

            // pause music
            AudioManager.Instance.CrossOut(1f);

            designController.IsTouchEnabled = false;

            this.ExecuteAfterSeconds(0.15f, () => uiTransitionDirector.ForceCloseAllView(false));

            Action PlayCompleteEvent = () => {
                designController.IsTouchEnabled = true;
                uiTransitionDirector.ForceCloseAllView(true);
                dialogueController.FinishEvent = null;

                AudioManager.Instance.CrossIn(3f).SetDelay(1.5f);

                if (Model.Instance.playRoomData.id == 1) {
                    tutorialController.ScheduleFirstTime_CompleteLivingRoom();
                }
            };

            postDecorCompleteEffect.Setup(items);
            postDecorCompleteEffect.Play(0.25f, PlayCompleteEvent);
        }

        private void CheckCanUnlockNextRoom() {
            if (roomData.nextRoomId > 0 && designController.GetUnlockedItemCount() >= roomData.maxItemCount) {
                PlayerData.current.homeDesignData.SetUnlockRoom(roomData.nextRoomId);
            }
        }

        private void ShowPopupMatch3Preparing() {
            if (PlayerData.current.stamina.Available()) {
                Popup.PopupSystem.GetOpenBuilder()
               .SetType(PopupType.PopupGameStart)
               .Open();
            } else {
                Popup.PopupSystem.GetOpenBuilder()
              .SetType(PopupType.PopupStaminaStore)
              .Open();
            }
        }

        void Update() {
            if (GameInput.Service.GetKeyDown(KeyCode.Escape) && Popup.PopupSystem.Instance.IsShowingPopup() == false)
                Popup.PopupSystem.GetOpenBuilder()
                .SetType(PopupType.PopupQuitGame)
                .Open();

            // if (Input.GetKeyDown(KeyCode.H))
            // {
            //     PlayPostDecorCompleteEffect();
            // }
        }


        private static Action PrepareBgTextureEvent;

        public static void PrepareBgTexture() {
            PrepareBgTextureEvent?.Invoke();

            Camera mainCamera = Camera.main;
            mainCamera.targetTexture = DecorBlurImageManager.GetSourceTexture();
            mainCamera.Render();
            mainCamera.targetTexture = null;
        }
    }
}
