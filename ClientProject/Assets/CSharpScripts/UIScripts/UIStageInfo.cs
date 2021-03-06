﻿using UnityEngine;
using System.Collections;

public class UIStageInfo : UIWindow 
{
    NumberDrawer m_levelNumber;
    NumberDrawer m_totalCostNumber;

    UIWindow m_stageTip;

    UILabel[] m_itemCostLabels = new UILabel[3];
    UIToggle [] m_itemToggles = new UIToggle [3];
    PurchasedItem [] m_items = new PurchasedItem [3];
    UISprite[] m_lockItemSprite = new UISprite[3];
    UISprite[] m_background = new UISprite[3];

    GameObject m_pointer;

    UISprite m_stageIcon;
    UILabel m_stageInfoLabel;


    //使用道具和不使用道具情况下的按钮和地板（需要运行时切换显示）
    GameObject m_playBtn;
    GameObject m_playPayCoinBtn;

    GameObject m_itemBoard;
    GameObject m_itemBoardWithArrow;

    int m_moneyCost = 0;

    public int GetCurCost() { return m_moneyCost; }

    public override void OnCreate()
    {
        base.OnCreate();

        m_levelNumber = GetChildComponent<NumberDrawer>("LevelNumber");
        m_totalCostNumber = GetChildComponent<NumberDrawer>("ItemTotalCost");

        m_stageIcon = GetChildComponent<UISprite>("StageIcon");
        m_stageInfoLabel = GetChildComponent<UILabel>("StageInfoLabel");

        for (int i = 0; i < 3; ++i )
        {
            m_itemCostLabels[i] = GetChildComponent<UILabel>("Item" + (i+1).ToString() +"Cost");
            m_itemToggles[i] = GetChildComponent<UIToggle>("Item" + (i + 1).ToString() + "Btn");
			m_itemToggles[i].SetWithoutTrigger(false);
            m_lockItemSprite[i] = GetChildComponent<UISprite>("LockItem" + (i + 1).ToString());

            m_background[i] = GetChildComponent<UISprite>("Background" + (i + 1).ToString());

            EventDelegate.Set(m_itemToggles[i].onChange, OnToggle);
        }

        m_playBtn = UIToolkits.FindChild(mUIObject.transform, "PlayBtn").gameObject;
        m_playPayCoinBtn = UIToolkits.FindChild(mUIObject.transform, "PlayPlayCoinBtn").gameObject;

        m_itemBoard = UIToolkits.FindChild(mUIObject.transform, "Board").gameObject;
        m_itemBoardWithArrow = UIToolkits.FindChild(mUIObject.transform, "BoardWithArrow").gameObject;
        
        AddChildComponentMouseClick("CloseBtn", OnCloseClicked);
        AddChildComponentMouseClick("PlayBtn", OnPlayClicked);
        AddChildComponentMouseClick("PlayPlayCoinBtn", OnPlayClicked);
    }

    public void OnToggle()
    {
        m_moneyCost = 0;
        int curItemIndex = -1;
        for (int i = 0; i < 3; ++i)
        {
            if (UIToggle.current == m_itemToggles[i])
            {
                curItemIndex = i;
            }
            if (m_itemToggles[i].value)
            {
                GlobalVars.StartStageItem[i] = m_items[i];
                m_moneyCost += CapsConfig.GetItemPrice(m_items[i]);
            }
            else
            {
                GlobalVars.StartStageItem[i] = PurchasedItem.None;
            }
        }

        if (m_moneyCost > Unibiller.GetCurrencyBalance("gold"))
        {
            ClearToggles();

            GlobalVars.StartStageItem[curItemIndex] = PurchasedItem.None;
			
            HideWindow(delegate()
            {
                //弹出购买金币提示
                GlobalVars.OnCancelFunc = delegate()
                {
                    GlobalVars.UsingItem = PurchasedItem.None;
                    ShowWindow();
                };

                GlobalVars.OnPurchaseFunc = delegate()
                {
                    GlobalVars.UsingItem = PurchasedItem.None;
                    ShowWindow();
                };

                GlobalVars.UsingItem = m_items[curItemIndex];

                UIWindowManager.Singleton.GetUIWindow<UIPurchaseNotEnoughMoney>().ShowWindow();
            });
        }
        else
        {
            if (UIToggle.current.value)
            {
                NGUITools.PlaySound(CapsConfig.CurAudioList.PurchaseClip);
            }
        }
        RefreshTotalMoney();
    }

    void RefreshTotalMoney()
    {
        if (m_moneyCost == 0)
        {
            m_playBtn.SetActive(true);
            m_playPayCoinBtn.SetActive(false);

            m_itemBoard.SetActive(true);
            m_itemBoardWithArrow.SetActive(false);
        }
        else
        {
            m_itemBoard.SetActive(false);
            m_itemBoardWithArrow.SetActive(true);

            m_playBtn.SetActive(false);
            m_playPayCoinBtn.SetActive(true);
            m_totalCostNumber.SetNumber(m_moneyCost);
        }
    }

    public override void OnShow()
    {
        base.OnShow();

        m_levelNumber.SetNumberRapid(GlobalVars.CurStageNum);

        for (int i = 0; i < 3; ++i )
        {
            UISprite star = GetChildComponent<UISprite>("Star" + (i + 1));
            if (GlobalVars.StageStarArray[GlobalVars.CurStageNum - 1] > i)
            {
                star.spriteName = "Star_Large";
            }
            else
            {
				star.spriteName = "Star_Dark";
            }
        }

        UISprite itemIcon = GetChildComponent<UISprite>("Item1Icon");
        if (GlobalVars.CurStageData.StepLimit > 0)
        {
            m_items[0] = PurchasedItem.ItemPreGame_PlusStep;
        }
        else if(GlobalVars.CurStageData.TimeLimit > 0)
        {
            m_items[0] = PurchasedItem.ItemPreGame_PlusTime;
        }

        m_items[1] = PurchasedItem.ItemPreGame_AddEatColor;
        m_items[2] = PurchasedItem.ItemPreGame_ExtraScore;

        itemIcon.spriteName = m_items[0].ToString();

        for (int i = 0; i < 3; ++i )
        {
            m_itemCostLabels[i].text = CapsConfig.GetItemPrice(m_items[i]).ToString();
            m_itemToggles[i].SetWithoutTrigger(false);

            if (CapsConfig.ItemUnLockLevelArray[(int)m_items[i]] <= GlobalVars.AvailabeStageCount || GlobalVars.DeveloperMode)       //判断道具是否已经解锁?
            {
                m_lockItemSprite[i].gameObject.SetActive(false);
                m_itemToggles[i].enabled = true;
                m_itemCostLabels[i].gameObject.SetActive(true);
                m_background[i].spriteName = "Item_Large";
            }
            else
            {
				m_lockItemSprite[i].gameObject.SetActive(true);
                m_itemToggles[i].enabled = false;
                m_itemCostLabels[i].gameObject.SetActive(false);
                m_background[i].spriteName = "Item_Large_Disable";
            }
        }

        NumberDrawer number = GetChildComponent<NumberDrawer>("StageTarget");
        number.SetNumber(GlobalVars.CurStageData.StarScore[2], 0.0f);

        UIWindowManager.Singleton.GetUIWindow<UIMainMenu>().HideWindow();

        RefreshTotalMoney();

        if (GlobalVars.AvailabeStageCount == GlobalVars.CurStageNum && GlobalVars.AvailabeStageCount == 2)      //若在第二关，显示手指
        {
            Transform gameAreaTrans = GameObject.Find("GameArea").transform;
            if (UIWindowManager.Singleton.GetUIWindow<UIFTUE>() == null)        //若已经出FTUE了
            {
                UIWindowManager.Singleton.CreateWindow<UIFTUE>();
            }
            m_pointer = UIWindowManager.Singleton.GetUIWindow<UIFTUE>().m_pointer;
            GameObject playBtn = GameObject.Find("PlayBtn");
            m_pointer.transform.parent = playBtn.transform;
            m_pointer.transform.localPosition = new Vector3(-26, 48, 0);
            //m_pointer.transform.parent = gameAreaTrans;                     //恢复父窗口
            m_pointer.SetActive(false);
            m_pointer.SetActive(true);
            m_pointer.GetComponent<TweenScale>().enabled = true;
        }

        //根据关卡类型显示提示信息
        m_stageIcon.spriteName = "MapPoint_Type" + CapsConfig.StageTypeArray[GlobalVars.CurStageNum - 1];
        m_stageInfoLabel.text = Localization.instance.Get("StageInfoHelpType" + CapsConfig.StageTypeArray[GlobalVars.CurStageNum - 1]);
    }

    public void OnCloseClicked()
    {
        HideWindow();

        if (CapsApplication.Singleton.CurStateEnum == StateEnum.Game)
        {
            CapsApplication.Singleton.ChangeState((int)StateEnum.Login);        //返回地图界面
            UIWindowManager.Singleton.GetUIWindow<UIMainMenu>().ShowWindow();
            UIWindowManager.Singleton.GetUIWindow<UIMap>().ShowWindow();        //返回地图，不需要刷新按钮
            LoginState.Instance.CurFlow = TLoginFlow.LoginFlow_Map;         //切换流程到显示地图
        }
        else
        {
            UIWindowManager.Singleton.GetUIWindow<UIMainMenu>().ShowWindow();
        }

        ClearToggles();

        if (m_pointer != null)
        {
            m_pointer.SetActive(false);
        }
    }

    public void ClearToggles()
    {
        //清理CheckBox
        for (int i = 0; i < 3; ++i)
        {
            if (m_itemToggles[i].value)
            {
                m_itemToggles[i].SetWithoutTrigger(false);
            }
        }
        m_moneyCost = 0;
    }

    private void OnPlayClicked()
    {
        if (Unibiller.DebitBalance("gold", m_moneyCost))        //消费
        {
            //使用道具
            for (int i = 0; i < 3; ++i)
            {
                if (m_itemToggles[i].value)
                {
                    GlobalVars.StartStageItem[i] = m_items[i];

                    CapsApplication.Singleton.SubmitUseItemData(m_items[i].ToString());
                }
                else
                {
                    GlobalVars.StartStageItem[i] = PurchasedItem.None;
                }
            }
            ClearToggles();
        }
        else        //若钱不够
        {
            return;
        }

        GlobalVars.UseHeart();      //使用一颗心

        if (CapsApplication.Singleton.CurStateEnum != StateEnum.Game)
        {
            UIWindowManager.Singleton.GetUIWindow<UIMap>().HideWindow();
            HideWindow();

            
            UIWindowManager.Singleton.GetUIWindow("UILoading").ShowWindow(
            delegate()
            {
                CapsApplication.Singleton.ChangeState((int)StateEnum.Game);
            }
            );

            UIWindowManager.Singleton.GetUIWindow<UIMainMenu>().HideWindow();
        }
        else
        {
            HideWindow(delegate()
            {
                UIWindowManager.Singleton.GetUIWindow<UIDialog>().TriggerDialog(GlobalVars.CurStageNum, DialogTriggerPos.StageStart, delegate()
                {
                    GameLogic.Singleton.Init();
                    GameLogic.Singleton.PlayStartEffect();
                    UIWindowManager.Singleton.GetUIWindow<UIMainMenu>().ShowWindow();
                    UIWindowManager.Singleton.GetUIWindow<UIGameHead>().ShowWindow();
                    UIWindowManager.Singleton.GetUIWindow<UIGameBottom>().ShowWindow();
                });
            });
        }
    }
}
