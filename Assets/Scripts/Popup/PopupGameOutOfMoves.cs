using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Popup;
using Spine.Unity;
using System.Xml.Serialization;

public class PopupGameOutOfMoves : PopupBase
{
	public const int ContinueGemCost = 50;

	public Transform main;

    public CollectBlockPlayView collectViewSample;

	public RectTransform characterTransform;

	public SkeletonGraphic characterGraphic;

	public Text gemCostText;

	private UIEdgeSnapPosition characterEdgeSnap;

	private bool invokeDenyEvent = true;

    private void Start()
    {
		InitRemainingGoals();
	}

	private void InitRemainingGoals()
    {
		var collectBlocks = MapData.main.collectBlocks;
		var collectViews = new List<CollectBlockView>();

		for (int j = 0; j < collectBlocks.Length; j++)
		{
			if (string.IsNullOrEmpty(collectBlocks[j].blockType) || collectBlocks[j].count <= 0 || !(collectViewSample != null))
			{
				continue;
			}

			CollectBlockPlayView component = (j == 0) ? collectViewSample
				: Instantiate(collectViewSample).GetComponent<CollectBlockPlayView>();
			collectViews.Add(component);

			if (!component)
			{
				continue;
			}

			if (!string.IsNullOrEmpty(collectBlocks[j].blockType) && collectBlocks[j].count > 0)
			{
				component.gameObject.transform.SetParent(collectViewSample.transform.parent, worldPositionStays: false);
				CollectBlockType collectBlockType = collectBlocks[j].GetCollectBlockType();

				Sprite sprite;
				switch (collectBlockType)
				{
					case CollectBlockType.NormalRed:
					case CollectBlockType.NormalOrange:
					case CollectBlockType.NormalYellow:
					case CollectBlockType.NormalGreen:
					case CollectBlockType.NormalBlue:
					case CollectBlockType.NormalPurple:
						sprite = CollectIconTable.Instance.GetSprite(MapData.main.collectBlocks[j].blockType);
						break;
					default:
						sprite = CollectIconTable.Instance.GetSprite(MapData.main.collectBlocks[j].blockType);
						break;
					case CollectBlockType.Null:
						continue;
				}

				if (sprite != null)
				{
					int num = GameMain.main.countOfEachTargetCount[(int)collectBlockType];

					component.SetData(collectBlockType, collectBlocks[j].count, sprite);
					if (num > 0)
						component.targetCountText.text = num.ToString();
					else
					{
						component.check.gameObject.SetActive(true);
						component.targetCountText.gameObject.SetActive(false);
					}
				}
			}
			else
			{
				DestroyImmediate(component);
			}
		}

		for (int i = 0; i < collectViews.Count; i++)
		{
			collectViews[i].UpdateSize();
		}
	}

    public override void Show()
    {
		invokeDenyEvent = true;
		GetComponent<CanvasGroup>().alpha = 1f;
		canClose = false;

		characterEdgeSnap = new UIEdgeSnapPosition(characterTransform, new Vector2(0f, -1.2f));
		characterEdgeSnap.SetPositionVisibility(false);
		characterEdgeSnap.Show(0.5f).SetEase(Ease.OutBack).OnComplete(() => canClose = true);

		PopupAnimationUtility.AnimateScale(main, Ease.OutBack, 0.25f, 1f, 0.25f, 0f);

		characterGraphic.AnimationState.AddAnimation(0, "lose", true, 0f);

		if (gemCostText != null)
			gemCostText.text = ContinueGemCost.ToString();
	}

    public override void Close(bool forceDestroying = true)
    {
		characterGraphic.DOFade(0f, 0.075f);

		PopupAnimationUtility.AnimadeAlpha(GetComponent<CanvasGroup>(), Ease.Linear, 1f, 0f, 0.1f, 0f, false);
		PopupAnimationUtility.AnimateScale(main, Ease.OutQuart, 1f, 0.8f, 0.1f, 0f)
            .OnComplete(() =>
			{
				if (invokeDenyEvent && forceDestroying)
					DenyEvent?.Invoke(null);
				
				TerminateInternal(forceDestroying);
			});
    }

    public void ContinueWithGem()
    {
		if (PlayerData.current.gemCount >= ContinueGemCost)
        {
			PlayerData.current.AddGem(-ContinueGemCost);
			EventDispatcher<GlobalEventId>.Instance.NotifyEvent(GlobalEventId.GemChange, PlayerData.current.gemCount);

			AcceptEvent?.Invoke(5);
			invokeDenyEvent = false;

			CloseInternal();
		}
        else
        {
			PopupUtility.OpenPopupLiteMesage("钻石不足");
		}	
    }

}
