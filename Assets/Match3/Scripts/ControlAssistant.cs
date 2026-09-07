using System.Collections.Generic;
using UnityEngine;

public class ControlAssistant : MonoBehaviour
{
	public static float swapMinimumMagnitude = 30f;

	public static readonly bool EnableDiagonicSwap;

	public static ControlAssistant main;

	public Camera controlCamera;

	private bool firstTap;

	private RaycastHit2D hit;

	private readonly List<FruitsBoxDragEnable> listPutToFruitsBoxDrag = new List<FruitsBoxDragEnable>();

	private readonly List<GameObject> listPutToFruitsBoxLineObject = new List<GameObject>();

	private CandyChip pressedCandyChip;

	private Chip pressedChip;

	private FruitsBoxDragEnable pressedFruitsBoxDrag;

	private Vector2 pressPoint;

	public int WaitingKeyDepth;

	public float WaitingKeyTime;

	private readonly float waitingLimitTime = 1f;

	private float waitingTimeAfterFirstTap;

	private void Awake()
	{
		main = this;
	}

	private void Update()
	{
		if (Time.timeScale != 0f)
		{
			if ((bool)GameMain.main && !GameMain.main.isGameResult)
			{
				UpdateInput();
			}
			if ((bool)GameMain.main && GameMain.main.isPlaying && GameMain.main.CanIWait())
			{
				WaitingKeyTime += Time.deltaTime;
			}
			if (firstTap && (bool)GameMain.main && GameMain.main.isPlaying)
			{
				waitingTimeAfterFirstTap += Time.deltaTime;
			}
			if (waitingTimeAfterFirstTap > waitingLimitTime)
			{
				waitingTimeAfterFirstTap = 0f;
				firstTap = false;
				pressedChip = (pressedCandyChip = null);
			}
		}
	}

	private void OnEnable()
	{
		WaitingKeyTime = 0f;
	}

	/// <summary>
	/// 统一输入更新：不再区分 mobile/desktop，全部通过 GameInput.Service 读取。
	/// 逻辑合并自原 MobileUpdate2 + DecktopUpdate2。
	/// </summary>
	private void UpdateInput()
	{
		if (GameInput.Service.PointerDown)
		{
			Vector2 origin = controlCamera.ScreenPointToRay(GameInput.Service.PointerPosition).origin;
			hit = Physics2D.Raycast(origin, Vector2.zero);
			if (!hit.transform)
			{
				return;
			}

			// 第二次点击：检查是否点击了已选中棋子的相邻格子
			if ((bool)pressedChip || (bool)pressedCandyChip)
			{
				Chip chip = (hit.transform.GetComponent<Chip>() != null)
					? hit.transform.GetComponent<Chip>()
					: hit.transform.GetComponent<Slot>().GetChip();

				if (!chip) return;

				Side[] straightSides = Utils.straightSides;
				foreach (Side key in straightSides)
				{
					if (((bool)pressedChip && pressedChip.parentSlot.slot.nearSlot[key] == chip.parentSlot.slot)
						|| ((bool)pressedCandyChip && pressedCandyChip.parentSlot.slot.nearSlot[key] == chip.parentSlot.slot))
					{
						Vector2 move = GameInput.Service.PointerPosition - pressPoint;
						CheckSwap(move);
						return;
					}
				}
			}

			// 首次点击：选中棋子
			if (hit.transform.GetComponent<Chip>() == null)
			{
				pressedChip = hit.transform.GetComponent<Slot>().GetChip();
			}
			else
			{
				pressedChip = hit.transform.GetComponent<Chip>();
			}

			if (pressedChip == null && hit.transform.GetComponent<FruitsBox>() != null)
			{
				if (!GameMain.main.isPlaying || !GameMain.main.CanIAnimate() || GameMain.main.CurrentTurn == VSTurn.CPU)
				{
					return;
				}
				pressedFruitsBoxDrag = hit.transform.GetComponent<FruitsBoxDragEnable>();
				pressedFruitsBoxDrag.SetParentSlot();
				listPutToFruitsBoxDrag.Add(pressedFruitsBoxDrag);
			}

			pressedCandyChip = (pressedChip as CandyChip);
			pressPoint = GameInput.Service.PointerPosition;
			OnTimerAfterFirstTap();
		}

		// 持续按住：检测拖拽交换
		if (GameInput.Service.PointerUp)
		{
			pressedChip = (pressedCandyChip = null);
			return;
		}

		if (!GameInput.Service.PointerHeld) return;

		if ((bool)pressedChip || (bool)pressedCandyChip)
		{
			CheckCantSwap();
			Vector2 origin2 = controlCamera.ScreenPointToRay(GameInput.Service.PointerPosition).origin;
			hit = Physics2D.Raycast(origin2, Vector2.zero);
			if (!hit.transform)
			{
				pressedChip = (pressedCandyChip = null);
				return;
			}
			Vector2 move2 = GameInput.Service.PointerPosition - pressPoint;
			CheckSwap(move2);
		}
		else if ((bool)pressedFruitsBoxDrag)
		{
			Vector2 origin3 = controlCamera.ScreenPointToRay(GameInput.Service.PointerPosition).origin;
			hit = Physics2D.Raycast(origin3, Vector2.zero);
			if (!hit.transform)
			{
				return;
			}
			CheckPutFruitsBox(hit.transform);
		}
	}

	private void CheckSwap(Vector2 move)
	{
		if (!GameMain.main.canSwap || !GameMain.main.isPlaying || GameMain.main.PauseControl || !GameMain.main.CanIWait() || GameMain.main.isConnectedSweetRoad || GameMain.main.isGameResult || !GameMain.main.isTurnResultEnd || GameMain.main.MoveCount == 0 || GameMain.main.IsGameSuccess(isPreValueCheck: true) || GameMain.main.canBombCandyFactory || Popup.PopupSystem.Instance.IsShowingPopup() || !Slot.isFirstJellyComplete)
		{
			pressedChip = null;
			pressedCandyChip = null;
			return;
		}
		WaitingKeyTime = 0f;
		WaitingKeyDepth = 0;
		if (move.magnitude >= swapMinimumMagnitude && GameMain.main.isTutorial == false)
		{
			if (EnableDiagonicSwap)
			{
				Side side = Side.Null;
				float num = 360f;
				Side side2 = Side.Null;
				for (int i = 0; i < 8; i++)
				{
					side2 = Utils.allSides[i];
					float num2 = Vector2.Angle(move, Utils.SideOffsetX(side2) * Vector2.right + Utils.SideOffsetY(side2) * Vector2.up);
					if (num2 <= 45f && num2 < num)
					{
						num = num2;
						side = side2;
					}
				}
				if ((bool)pressedChip && side != 0)
				{
					pressedChip.Swap(side);
				}
			}
			else
			{
				Side side3 = Side.Null;
				for (int j = 0; j < 4; j++)
				{
					side3 = Utils.straightSides[j];
					if (!(Vector2.Angle(move, Utils.SideOffsetX(side3) * Vector2.right + Utils.SideOffsetY(side3) * Vector2.up) <= 45f))
					{
						continue;
					}
					if ((bool)pressedCandyChip && (bool)pressedCandyChip.parentSlot && GameMain.main.CanIMatch())
					{
						if ((bool)pressedCandyChip.parentSlot.slot && (bool)pressedCandyChip.parentSlot.slot.GetBlock())
						{
							continue;
						}
						bool flag = false;
						Slot slot = pressedCandyChip.parentSlot[side3];
						if (!slot || !slot.canBeControl || ((bool)slot.GetChip() && slot.GetChip().chipType == ChipType.MouseChip))
						{
							continue;
						}
						Chip chip = slot.GetChip();
						if ((bool)chip && slot.GetBlock() == null && BombMixEffect.ContainsPair(ChipType.CandyChip, chip.chipType))
						{
							flag = true;
						}
						if (!flag)
						{
							GameMain.main.TurnStart();
							int x = pressedCandyChip.parentSlot.slot.x;
							int y = pressedCandyChip.parentSlot.slot.y;
							Vector3 position = pressedCandyChip.parentSlot.slot.transform.position;
							pressedCandyChip.CrushCandyChip();
							Transform transform = SpawnStringBlock.GetSpawnBlockObject(SpawnStringBlockType.SmallCandyMixEffect).transform;
							if ((bool)transform)
							{
								transform.position = position;
								transform.localPosition += new Vector3(0f, 0f, -1f);
								SmallCandyMixEffect component = transform.GetComponent<SmallCandyMixEffect>();
								BoardManager.main.GetSlot(x, y).SetChip(component);
								component.Rolling(side3);
							}
							GameMain.main.TurnEnd();
						}
						else
						{
							pressedCandyChip.Swap(side3);
						}
					}
					else if ((bool)pressedChip && (bool)pressedChip.parentSlot)
					{
						Slot slot2 = pressedChip.parentSlot[side3];
						if (!slot2 || slot2.canBeControl)
						{
							pressedChip.Swap(side3);
						}
					}
				}
			}
			pressedChip = null;
			pressedCandyChip = null;
		}
		else if ((bool)pressedChip)
		{
			if ((bool)pressedChip.parentSlot && (bool)pressedChip.parentSlot.slot && !pressedChip.parentSlot.slot.canBeControl)
			{
				pressedChip = null;
				pressedCandyChip = null;
			}
			else
			{
				pressedChip.OnClick();
			}
		}
	}

	private void CheckPutFruitsBox(Transform hitTransform)
	{
		FruitsBoxDragEnable component = hitTransform.GetComponent<FruitsBoxDragEnable>();
		if (!(component != null))
		{
			return;
		}
		component.SetParentSlot();
		if (!listPutToFruitsBoxDrag.Contains(component))
		{
			FruitsBoxDragEnable fruitsBoxDragEnable = listPutToFruitsBoxDrag[listPutToFruitsBoxDrag.Count - 1];
			if (!(fruitsBoxDragEnable == null) && !(component.parentSlot == null) && !(fruitsBoxDragEnable.parentSlot == null) && (component.putType == FruitsBoxDragEnable.PutType.FruitsBox || fruitsBoxDragEnable.putType == FruitsBoxDragEnable.PutType.FruitsBox || (component.putType == FruitsBoxDragEnable.PutType.Chip && fruitsBoxDragEnable.parentSlot.GetChip().id == component.parentSlot.GetChip().id)) && Mathf.Abs(component.parentSlot.x - fruitsBoxDragEnable.parentSlot.x) <= 1 && Mathf.Abs(component.parentSlot.y - fruitsBoxDragEnable.parentSlot.y) <= 1)
			{
				AddDrawPutFruitsBoxLine(listPutToFruitsBoxDrag[listPutToFruitsBoxDrag.Count - 1], component);
				listPutToFruitsBoxDrag.Add(component);
			}
			return;
		}
		int num = listPutToFruitsBoxDrag.IndexOf(component);
		if (num < listPutToFruitsBoxDrag.Count - 1)
		{
			listPutToFruitsBoxDrag.RemoveRange(num + 1, listPutToFruitsBoxDrag.Count - 1 - num);
			for (int i = num; i < listPutToFruitsBoxLineObject.Count; i++)
			{
				UnityEngine.Object.Destroy(listPutToFruitsBoxLineObject[i]);
			}
			listPutToFruitsBoxLineObject.RemoveRange(num, listPutToFruitsBoxLineObject.Count - num);
		}
	}

	private void AddDrawPutFruitsBoxLine(FruitsBoxDragEnable from, FruitsBoxDragEnable to)
	{
		if (from != null && to != null && from.parentSlot != null && to.parentSlot != null)
		{
			listPutToFruitsBoxLineObject.Add(BoardManager.main.GetNewFruitBoxDragLine(from.parentSlot.transform.position, to.parentSlot.transform.position));
		}
	}

	private void CrushFruitsBox()
	{
		if (listPutToFruitsBoxDrag.Count > 1)
		{
			GameMain.main.TurnEnd();
			for (int i = 0; i < listPutToFruitsBoxDrag.Count; i++)
			{
				if (listPutToFruitsBoxDrag[i].putType == FruitsBoxDragEnable.PutType.FruitsBox)
				{
					listPutToFruitsBoxDrag[i].GetComponent<FruitsBox>().RemoveBlock();
				}
				else if (listPutToFruitsBoxDrag[i].putType == FruitsBoxDragEnable.PutType.Chip)
				{
					int x = listPutToFruitsBoxDrag[i].parentSlot.x;
					int y = listPutToFruitsBoxDrag[i].parentSlot.y;
					BoardManager.main.SlotCrush(x, y, radius: true);
				}
			}
			for (int j = 0; j < listPutToFruitsBoxLineObject.Count; j++)
			{
				UnityEngine.Object.Destroy(listPutToFruitsBoxLineObject[j]);
			}
			listPutToFruitsBoxLineObject.Clear();
		}
		pressedFruitsBoxDrag = null;
		listPutToFruitsBoxDrag.Clear();
	}

	/// <summary>
	/// 统一的"不可交换"检测，合并自 CheckCantSwapOfMobile + CheckCantSwapOfDesktop。
	/// </summary>
	private void CheckCantSwap()
	{
		Vector2 origin = controlCamera.ScreenPointToRay(GameInput.Service.PointerPosition).origin;
		hit = Physics2D.Raycast(origin, Vector2.zero);
		if (!hit.transform)
		{
			AnimationAssistant.main.ShakeChip((!(pressedChip != null)) ? pressedCandyChip : pressedChip);
			return;
		}
		Slot slot = null;
		if (hit.transform.GetComponent<Chip>() == null)
		{
			slot = hit.transform.GetComponent<Slot>();
		}
		else if ((bool)hit.transform.GetComponent<Chip>().parentSlot)
		{
			slot = hit.transform.GetComponent<Chip>().parentSlot.slot;
		}
		if ((bool)slot)
		{
			BlockInterface block = slot.GetBlock();
			if ((bool)block)
			{
				AnimationAssistant.main.ShakeChip((!(pressedChip != null)) ? pressedCandyChip : pressedChip);
			}
		}
	}

	private void OnTimerAfterFirstTap()
	{
		if ((bool)pressedChip || (bool)pressedCandyChip)
		{
			firstTap = true;
			waitingTimeAfterFirstTap = 0f;
		}
	}

	public Slot GetSlotFromTouch()
	{
		Vector2 origin = controlCamera.ScreenPointToRay(GameInput.Service.PointerPosition).origin;
		hit = Physics2D.Raycast(origin, Vector2.zero);
		Slot result = null;
		if ((bool)hit)
		{
			if (!hit.transform)
			{
				result = null;
			}
			if (hit.transform.GetComponent<Chip>() == null)
			{
				result = hit.transform.GetComponent<Slot>();
			}
			else if (hit.transform.GetComponent<Chip>().parentSlot != null)
			{
				result = hit.transform.GetComponent<Chip>().parentSlot.slot;
			}
		}
		return result;
	}

	public void ReleasePressedChip()
	{
		if ((bool)pressedChip)
		{
			pressedChip = null;
		}
		if ((bool)pressedCandyChip)
		{
			pressedCandyChip = null;
		}
	}
}
