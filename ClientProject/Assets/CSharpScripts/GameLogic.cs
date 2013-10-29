﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum TGameState
{
    EGameState_Playing,                         //游戏中
    EGameState_SugarCrushAnim,                  //进入特殊奖励前的动画
    EGameState_EndEatingSpecial,                //结束后开始逐个吃屏幕上的特殊块
    EGameState_EndStepRewarding,                //结束后根据步数奖励
    EGameState_End,
};

enum TDirection
{
    EDir_Up,
    EDir_UpRight,
    EDir_DownRight,
    EDir_Down,
    EDir_LeftDown,
    EDir_LeftUp,
};

struct Paticle
{
    int xSpeed;
    int ySpeed;
    int startX;
    int startY;
    long startTime;
    float gravity;			//重力值
    float lifeTime;			//生命期
    bool changeAlphaByLifeTime;		//是否改变Alpha
    TBlockColor color;
    UISprite pBlockSprite;
};

public class Position{
	public int x;
	public int y;
	public Position()
	{
        x = 0;
        y = 0;
	}
	public Position(int xPos, int yPos)
    {
        x = xPos;
        y = yPos;
    }
	
	public Position(Position pos)
    {
        x = pos.x;
        y = pos.y;
    }

    public override bool Equals(object ob)
    {
        if (this == ob)
        {
            return true;
        }
        if ((ob == null) || !this.GetType().Equals(ob.GetType()))
        {
            return false;
        }

        Position oth = (Position)ob;
        return this.x == oth.x && this.y == oth.y;
    }

    public void Set(int xPos, int yPos)
    {
        x = xPos;
        y = yPos;
    }

    public void Assign(Position p)
    {
        x = p.x;
        y = p.y;
    }

    public int ToInt()
    {
        return y * 10 + x;
    }

    public void FromInt(int val)
    {
        x = val % 10;
        y = val / 10;
    }
};

public enum TGridType
{
    Normal,         //普通
    None,           //空格子
    Jelly,          //果冻
    JellyDouble,    //两层果冻
}

public enum TGridBlockType     //固定障碍
{
    None,               //没有
    Stone,              //石头
    Chocolate,          //巧克力
    Cage,               //笼子
}

public class GridData
{
    public TGridType grid;
    public TGridBlockType gridBlock;
    public bool bBirth;                          //是否为出生点
    public bool bExit;                           //坚果的出口
}

public class GameLogic {
    public static int BlockCountX = 9;	//游戏区有几列
    public static int BlockCountY = 9;	//游戏区有几行
    public static int BLOCKWIDTH = 60;      //宽度
    public static int BLOCKHEIGHT = 69;     //高度
    public static int gameAreaX = 0;		//游戏区域左上角坐标
    public static int gameAreaY = 140;		//游戏区域左上角坐标
    public static int gameAreaWidth = BLOCKWIDTH * BlockCountX;	//游戏区域宽度
    public static int gameAreaHeight = BLOCKHEIGHT * BlockCountY + BLOCKHEIGHT / 2;//游戏区域高度
    public static int TotalColorCount = 7;
    public static int PROGRESSTOWIN = 2000;
    public static int DROP_TIME = 120;			//下落的时间
    public static int MOVE_TIME = 250;    		//移动的时间
    public static int EATBLOCK_TIME = 200;		//消块时间
    public static int GAMETIME = 6000000;		//游戏时间
    public static float CheckAvailableTimeInterval = 1.0f;       //1秒钟后尝试找是否有可消块
    public static float ShowHelpTimeInterval = 5.0f;       //5秒钟后显示可消块
    public static float ShowNoPossibleExhangeTextTime = 1.0f;      //没有可交换的块显示，持续1秒钟
    public static int StepRewardInterval = 500;             //步数奖励的时间间隔
    public static int SugarCrushAnimTime = 1200;            //SugarCrush动画的时间长度
    public int CurSeed;                                     //当前的随机种子
    public StageData PlayingStageData;                      //当前的关卡数据
    public int GetProgress(){ return m_progress; }
    public void AddProgress(int progress)
    {
        m_progress += progress;
        UIWindowManager.Singleton.GetUIWindow<UIGame>().OnChangeProgress(m_progress);
    }

    ///游戏逻辑变量/////////////////////////////////////////////////////////////////
	TDirection m_moveDirection;							                //选择的块1向块2移动的方向
	Position [] m_selectedPos = new Position[2];		                //记录两次点击选择的方块
    CapBlock[,] m_blocks = new CapBlock[BlockCountX, BlockCountY];		//屏幕上方块的数组
	int m_progress;										//当前进度
	TGameState m_gameState;								//游戏状态
	int m_comboCount;				//记录当前连击数
	bool m_changeBack;		//在交换方块动画中标志是否为换回动画
    System.Random m_random;
    long m_gameStartTime = 0;                              //游戏开始时间
    long m_sugarCurshAnimStartTime = 0;                    //sugarCrush动画的开始时间
    long m_lastStepRewardTime = 0;                         //上次生成StepReward的时间

    ///统计数据///////////////////////////////////////////////////////////////////////
    long m_lastClickTime;				//上次点击时间
    long m_rhythmMark;				//节奏感得分
    long m_gameTakeTime;				//一局游戏所用时间
    int m_totalClickCount;		//总共点击次数
    int m_workedClickCount;	//有效点击次数
    int m_maxComboCount;		//最大combo数
    int m_totalComboCount;			//本局总连击数

    LinkedList<long> m_perClickTakeTime = new LinkedList<long>();				//每次点击所用时间记录
    int m_rpMark;								//人品得分

    //计时器
    Timer timerMoveBlock = new Timer();
    Timer timerEatBlock = new Timer();
    Timer timerDropDown = new Timer();
    float m_dropDownEndTime;
    float m_showNoPossibleExhangeTextTime = 0;              //显示
    Position helpP1, helpP2;
    Position touchBeginPos;                                 //触控开始的位置

    	//资源物件
	//CCAnimation *m_blockMoveAni[6][6];			//六种方块向六个方向移动的动画
	//CCSprite * m_selectAni;						//选中动画
	//BlockSprite * m_pComboSprite[5];			//Combo粒子的精灵
    LinkedList<CapBlock> m_capBlockFreeList = new LinkedList<CapBlock>();				//用来存放可用的Sprite
	Dictionary<int, string> m_soundEffectMap = new Dictionary<int, string>();								    //声音

    Dictionary<string, LinkedList<ParticleSystem> > m_particleMap = new Dictionary<string, LinkedList<ParticleSystem> >();
    Dictionary<string, LinkedList<ParticleSystem>> m_freeParticleMap = new Dictionary<string, LinkedList<ParticleSystem>>();


	//std::vector<NumberDrawer *> m_endScoreBoardNumsVec;		//结束面板上的一堆数字

	//NumberDrawer * m_comboNumDrawer;		//用来绘制Combo数字
	NumberDrawer m_progressNumDrawer = new NumberDrawer();		//用来绘制进度数字
	NumberDrawer m_pSecNum = new NumberDrawer();				//用来绘制秒数
	NumberDrawer m_pMicroSecNum = new NumberDrawer();			//用来绘制微秒数

    GameObject m_freePool;
    GameObject m_capsPool;

    int m_nut1Count;
    int m_nut2Count;

    public GameLogic()
    {
        m_freePool = GameObject.Find("FreePool");
        m_capsPool = GameObject.Find("CapsPool");
    }

    public void Init()
    {
        //初始化瓶盖图片池
        string name;
        for (int i = 0; i < TotalColorCount + 2; ++i)            //最多7种颜色，固定死
        {
            name = "Item" + (i + 1);
            GameObject capObj = GameObject.Find("CapInstance");
            for (int j = 0; j < 100; ++j )
            {
                CapBlock capBlock = new CapBlock();
                m_capBlockFreeList.AddLast(capBlock);
            }
        }

        m_selectedPos[0] = new Position();
        m_selectedPos[1] = new Position();
		
		PlayingStageData = StageData.CreateStageData();
        PlayingStageData.LoadStageData(GlobalVars.CurStageNum);
    }

    bool Help(out Position p1, out Position p2)                 //查找到一个可交换的位置
    {
        for (int i = 0; i < BlockCountX; ++i )
        {
            for (int j = 0; j < BlockCountY; ++j )
            {
                if (m_blocks[i, j] == null || m_blocks[i, j].isLocked)                     //空格或空块
                {
                    continue;
                }

                Position position = new Position(i, j);

                for (TDirection dir = TDirection.EDir_Up; dir <= TDirection.EDir_LeftUp; dir = (TDirection)(dir + 1))		//遍历6个方向
                {
                    Position curPos = GoTo(position, dir, 1);
                    if (CheckPosAvailable(curPos))
                    {
                        if (m_blocks[curPos.x, curPos.y] == null || m_blocks[curPos.x, curPos.y].isLocked)             //空格或空块
                        {
                            continue;
                        }

                        ExchangeBlock(curPos, position);        //临时交换
                        if (IsHaveLine(curPos) || IsHaveLine(position))
                        {
                            p1 = curPos;
                            p2 = position;
                            ExchangeBlock(curPos, position);        //换回
                            return true;
                        }
                        ExchangeBlock(curPos, position);        //换回
                    }
                    
                }

            }
        }
        p1 = null;
        p2 = null;
        return false;
    }

    public void AutoResort()           //自动重排功能 Todo 没处理交换后形成消除的情况，不确定要不要处理
    {
        Position[] array = new Position[BlockCountX * BlockCountY];

        int count = 0;
        for (int i = 0; i < BlockCountX; ++i)
        {
            for (int j = 0; j < BlockCountY; ++j)
            {
                if (m_blocks[i, j] == null || m_blocks[i, j].isLocked)                     //空格或被锁块
                {
                    continue;
                }

                array[count] = new Position(i, j);          //先找出可以交换的位置
                ++count;
            }
        }

        Permute<Position>(array, count);       //重排



        CapBlock[,] blocks = new CapBlock[BlockCountX, BlockCountY];

        for (int i = 0; i < BlockCountX; ++i)
        {
            for (int j = 0; j < BlockCountY; ++j)
            {
                blocks[i, j] = m_blocks[i, j];          //先复制份数据
            }
        }

        count = 0;
        for (int i = 0; i < BlockCountX; ++i)
        {
            for (int j = 0; j < BlockCountY; ++j)
            {
                if (m_blocks[i, j] == null || m_blocks[i, j].isLocked)                     //空格或空块
                {
                    continue;
                }
                    
                m_blocks[i, j] = blocks[array[count].x, array[count].y];        //把随机内容取出来保存上
                ++count;
            }
        }

    }

    void Permute<T>(T[] array, int count)
    {
        for (int i = 1; i < count; i++)
        {
            Swap<T>(array, i, m_random.Next(0, i));
        }
    }

    void Swap<T>(T[] array, int indexA, int indexB)
    {
        T temp = array[indexA];
        array[indexA] = array[indexB];
        array[indexB] = temp;
    }

    public void StartGame()     //开始游戏（及重新开始游戏）
    {
        Timer.s_currentTime = Time.realtimeSinceStartup;        //更新一次时间
        long time = Timer.millisecondNow();
        m_lastClickTime = time;
        m_gameStartTime = time;

        CurSeed = PlayingStageData.Seed;

        if (CurSeed > 0)
        {
            m_random = new System.Random(CurSeed);
        }
        else
        {
            m_random = new System.Random((int)Time.timeSinceLevelLoad * 1000);
        }

        //查找坚果的出口
        if (PlayingStageData.Target == GameTarget.BringFruitDown)
        {
            PlayingStageData.Nut1Count = 0;
            PlayingStageData.Nut2Count = 0;
        }

        //从随机位置开始
        int randomPos = m_random.Next() % BlockCountX;

        for (int i = 0; i < BlockCountX; i++)
        {
            int xPos = (randomPos + i) % BlockCountX;
            for (int j = 0; j < BlockCountY; j++)
            {
                if (PlayingStageData.CheckFlag(xPos, j, GridFlag.GenerateCap))
                {
                    CreateBlock(xPos, j, true);
                }
            }
        }

        OnProgressChange();

        m_dropDownEndTime = Time.realtimeSinceStartup;
		
		UIWindowManager.Singleton.GetUIWindow<UIGame>().Reset();

        m_gameState = TGameState.EGameState_Playing;                //开始游戏
    }

    public void ClearGame()
    {
        m_progress = 0;
        //处理粒子////////////////////////////////////////////////////////////////////////
        foreach (KeyValuePair<string, LinkedList<ParticleSystem>> pair in m_particleMap)
        {
            LinkedList<ParticleSystem> list = pair.Value;
            foreach (ParticleSystem par in list)
            {
                GameObject.Destroy(par.gameObject);
            }
        }

        m_particleMap.Clear();

        foreach (KeyValuePair<string, LinkedList<ParticleSystem>> pair in m_freeParticleMap)
        {
            LinkedList<ParticleSystem> list = pair.Value;
            foreach (ParticleSystem par in list)
            {
                GameObject.Destroy(par.gameObject);
            }
        }

        m_freeParticleMap.Clear();

        foreach (CapBlock block in m_capBlockFreeList)
        {
            GameObject.Destroy(block.m_blockTransform.gameObject);
        }

        m_capBlockFreeList.Clear();

        for (int i = 0; i < BlockCountX; ++i)
        {
            for (int j = 0; j < BlockCountY; ++j)
            {
                if (m_blocks[i, j] != null)
                {
                    GameObject.Destroy(m_blocks[i, j].m_blockTransform.gameObject);
                }
            }
        }
    }

    int GetXPos(int x)
    {
        return x * BLOCKWIDTH + BLOCKWIDTH / 2;
    }

    int GetYPos(int x, int y)
    {
        return gameAreaY + y * BLOCKHEIGHT + (x + 1) % 2 * BLOCKHEIGHT / 2 + BLOCKHEIGHT / 2;
    }

    public void Update()
    {
        Timer.s_currentTime = Time.realtimeSinceStartup;

        if (m_gameState == TGameState.EGameState_SugarCrushAnim)        //播放sugarcrush动画状态
        {
            UIDrawer.Singleton.DrawText("SugarCrush", 100, 100, "Sugar Crush!!!!!!!!!!!!!!!!");
            if (Timer.millisecondNow() - m_sugarCurshAnimStartTime > SugarCrushAnimTime)        //若时间到
            {
                m_gameState = TGameState.EGameState_EndEatingSpecial;                           //切下一状态
                return;
            }
        }

        //游戏结束后自动吃特殊块的状态，且当前没在消块或下落状态
        if (m_gameState == TGameState.EGameState_EndEatingSpecial && timerEatBlock.GetState() == TimerEnum.EStop && timerDropDown.GetState() == TimerEnum.EStop)          
        {
            for (int i = 0; i < BlockCountX; ++i )
            {
                for (int j = 0; j < BlockCountY; ++j )
                {
                    if (m_blocks[i, j] != null && m_blocks[i, j].special != TSpecialBlock.ESpecial_Normal)
                    {
                        EatBlock(new Position(i, j));
                        timerEatBlock.Play();
                        return;         //消一个特殊块就返回
                    }
                }
            }

            //若执行到这里证明已经没特殊块可以消了
            if (PlayingStageData.StepLimit > 0)     //若剩余步数大于0，进入步数奖励
            {
                m_gameState = TGameState.EGameState_EndStepRewarding;
                m_lastStepRewardTime = Timer.millisecondNow();
            }
            else
            {
                m_gameStartTime = 0;
                m_gameState = TGameState.EGameState_End;
                UIWindowManager.Singleton.GetUIWindow<UIGameEnd>().ShowWindow();
            }
            return;
        }

        //步数奖励状态
        if (m_gameState == TGameState.EGameState_EndStepRewarding)
        {
            if (Timer.millisecondNow() - m_lastStepRewardTime > StepRewardInterval)     //若到了时间间隔， 生成一个步数奖励
            {
                if (PlayingStageData.StepLimit > 0)
                {
                    //if (PlayingStageData.Target == GameTarget.BringFruitDown)
                    {
                        Position pos = FindRandomPos(TBlockColor.EColor_None, null, true);
                        m_blocks[pos.x, pos.y].special = TSpecialBlock.ESpecial_EatLineDir0 + (m_random.Next() % 3);
                        m_blocks[pos.x, pos.y].RefreshBlockSprite(PlayingStageData.GridData[pos.x, pos.y]);
                        --PlayingStageData.StepLimit;           //步数减一
                    }
                }
                else
                {
                    m_gameState = TGameState.EGameState_EndEatingSpecial;
                }

                m_lastStepRewardTime = Timer.millisecondNow();
            }
        }

        if (m_gameStartTime == 0)           //游戏没到开始状态
        {
            return;
        }
		//Timer.s_currentTime = Timer.s_currentTime + 0.02f;		//

        if (m_showNoPossibleExhangeTextTime > 0)        //正在显示“没有可交换块，需要重排”
        {
            if (Timer.millisecondNow() > m_showNoPossibleExhangeTextTime + ShowNoPossibleExhangeTextTime)       //时间已到
            {
                AutoResort();                                   //自动重排
                m_showNoPossibleExhangeTextTime = 0;            //交换完毕，关闭状态
            }
            else
            {
                //显示提示信息
                UIDrawer.Singleton.DrawText("NoExchangeText", 100, 100, "No Block To Exchange! Auto Resort...");
            }
        }

        TimerWork();

        //Color curColor = new Color(1.0f, 1.0f, 1.0f, 1.0f - Mathf.Clamp01((float)timerEatBlock.GetTime() / EATBLOCK_TIME));
		Color defaultColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        //根据数据绘制Sprite
        for (int i = 0; i < BlockCountX; i++)
        {
            for (int j = 0; j < BlockCountY; j++)
            {
                if (PlayingStageData.GridData[i, j] == 0)
                {
                    continue;
                }
                if (m_blocks[i,j] != null)
                {
                    m_blocks[i, j].m_blockTransform.localPosition = new Vector3(GetXPos(i) + m_blocks[i, j].x_move, -(m_blocks[i, j].y_move + GetYPos(i, j)), -105);
                    m_blocks[i, j].m_blockSprite.color = defaultColor;          //Todo 实在不知道为什么加上这句动画控制Alpha才好使

                    if (m_blocks[i, j].IsEating())
                    {
                        UIDrawer.Singleton.DrawNumber("Score" + i + "," + j, (int)m_blocks[i, j].m_blockTransform.localPosition.x, -(int)m_blocks[i, j].m_blockTransform.localPosition.y, 60, "BaseNum", 15, 4);
                    }
                }

                //绘制底图
                if (PlayingStageData.CheckFlag(i, j, GridFlag.Jelly))
                {
                    UIDrawer.Singleton.DrawSprite("Jelly" + i + "," + j, GetXPos(i), GetYPos(i, j), "Jelly");
                }
                else if (PlayingStageData.CheckFlag(i, j, GridFlag.JellyDouble))
                {
                    UIDrawer.Singleton.DrawSprite("Jelly2" + i + "," + j, GetXPos(i), GetYPos(i, j), "JellyDouble");
                }
                else if (PlayingStageData.GridData[i, j] != 0)
                {
                    UIDrawer.Singleton.DrawSprite("Grid" + i + "," + j, GetXPos(i), GetYPos(i, j), "Grid"); 
                }

                if (PlayingStageData.CheckFlag(i, j, GridFlag.Cage))
                {
                    UIDrawer.Singleton.DrawSprite("Cage" + i + "," + j, GetXPos(i), GetYPos(i, j), "Cage", 3);
                }

                if (PlayingStageData.CheckFlag(i, j, GridFlag.Stone))
                {
                    UIDrawer.Singleton.DrawSprite("Stone" + i + "," + j, GetXPos(i), GetYPos(i, j), "Stone", 3);
                }

                if (PlayingStageData.CheckFlag(i, j, GridFlag.Chocolate))
                {
                    UIDrawer.Singleton.DrawSprite("Chocolate" + i + "," + j, GetXPos(i), GetYPos(i, j), "Chocolate", 3);
                }

                //绘制水果出口
                if (PlayingStageData.Target == GameTarget.BringFruitDown && PlayingStageData.CheckFlag(i, j, GridFlag.FruitExit))
                {
                    UIDrawer.Singleton.DrawSprite("Exit" + i + "," + j, GetXPos(i), GetYPos(i, j), "FruitExit", 3); 
                }

                if (GlobalVars.EditStageMode && PlayingStageData.CheckFlag(i, j, GridFlag.Birth))     //若在关卡编辑状态
                {
                    UIDrawer.Singleton.DrawSprite("Birth" + i + "," + j, GetXPos(i), GetYPos(i, j), "Birth", 3);       //出生点
                }
            }
        }
        if (Time.deltaTime > 0.02f)
        {
            Debug.Log("DeltaTime = " + Time.deltaTime);
        }

        if (GlobalVars.CurStageData.StepLimit > 0)          //限制步数的关卡
        {
            UIDrawer.Singleton.DrawNumber("SetpLimit", 210, 864, PlayingStageData.StepLimit, "BaseNum", 24);
        }
        if (GlobalVars.CurStageData.TimeLimit > 0)          //限制时间的关卡
        {
            UIDrawer.Singleton.DrawText("TimeLimitText", 160, 864, "Time:");
            float timeRemain = GlobalVars.CurStageData.TimeLimit - (Timer.millisecondNow() - m_gameStartTime) / 1000.0f;
            timeRemain = Mathf.Max(0, timeRemain);
            UIDrawer.Singleton.DrawNumber("TimeLimit", 210, 864, timeRemain, "HighDown", 14, 3, 1);
        }

        if (GlobalVars.CurStageData.Target == GameTarget.BringFruitDown)
        {
            UIDrawer.Singleton.DrawText("Nut1Count", 100, 600, "Nut1:" + PlayingStageData.Nut1Count + "/" + GlobalVars.CurStageData.Nut1Count);
            UIDrawer.Singleton.DrawText("Nut2Count", 180, 600, "Nut2:" + PlayingStageData.Nut2Count + "/" + GlobalVars.CurStageData.Nut2Count);
        }

        //绘制分数
        UIDrawer.Singleton.DrawNumber("ScoreText", 342, 864, m_progress, "BaseNum", 24, 7);

        //绘制传送门
        foreach(KeyValuePair<int, Portal> pair in PlayingStageData.PortalToMap)
        {
            if (pair.Value.flag == 1)               //可见传送门
            {
                UIDrawer.Singleton.DrawSprite("PortalStart" + pair.Key, GetXPos(pair.Value.from.x), GetYPos(pair.Value.from.x, pair.Value.from.y) + BLOCKHEIGHT / 2, "PortalStart", 3);
                UIDrawer.Singleton.DrawSprite("PortalEnd" + pair.Key, GetXPos(pair.Value.to.x), GetYPos(pair.Value.to.x, pair.Value.to.y) - BLOCKHEIGHT / 2 + 15, "PortalEnd", 3);
            }
            else if (GlobalVars.EditStageMode)      //编辑器模式，画不可见传送门
            {
                UIDrawer.Singleton.DrawSprite("InviPortalStart" + pair.Key, GetXPos(pair.Value.from.x), GetYPos(pair.Value.from.x, pair.Value.from.y), "InviPortalStart", 3);
                UIDrawer.Singleton.DrawSprite("InviPortalEnd" + pair.Key, GetXPos(pair.Value.to.x), GetYPos(pair.Value.to.x, pair.Value.to.y), "InviPortalEnd", 3);
            }
        }

        if (m_dropDownEndTime > 0)
        {
            if (helpP1 == null)     //还没找帮助点
            {
                if (Time.realtimeSinceStartup > m_dropDownEndTime + CheckAvailableTimeInterval)
                {
                    helpP1 = new Position();
                    helpP2 = new Position();
                    if(!Help(out helpP1, out helpP2))
                    {
                        m_showNoPossibleExhangeTextTime = Timer.millisecondNow();                       //显示需要重排
                    }
                }
            }
            else if (Time.realtimeSinceStartup > m_dropDownEndTime + ShowHelpTimeInterval)
            {
                if (m_blocks[helpP1.x, helpP1.y]!=null && !m_blocks[helpP1.x, helpP1.y].m_animation.isPlaying)
                {
                    m_blocks[helpP1.x, helpP1.y].m_animation.Play("Help");
                    m_blocks[helpP2.x, helpP2.y].m_animation.Play("Help");
                }
            }
        }

        //处理粒子////////////////////////////////////////////////////////////////////////
        foreach (KeyValuePair<string, LinkedList<ParticleSystem>> pair in m_particleMap)
        {
            LinkedList<ParticleSystem> list = pair.Value;

            ParticleSystem parToDelete = null;
            foreach (ParticleSystem par in list)
            {
                if (par.isStopped)
                {
                    m_freeParticleMap[pair.Key].AddLast(par);           //添加空闲的
                    parToDelete = par;
                    par.Stop();
                    par.gameObject.SetActive(false);
                    break;                                              //每帧只处理一个
                }
            }

            if (parToDelete != null)
            {
                list.Remove(parToDelete);                               //在原列表中删除
            }
        }
    }

    void TimerWork()
    {
        /*------------------处理timerEatBlock------------------*/
        if (timerEatBlock.GetState() == TimerEnum.ERunning)
        {			//如果消块计时器状态为开启
            if (timerEatBlock.GetTime() > EATBLOCK_TIME)
            {		//消块计时器到400毫秒
                timerEatBlock.Stop();

                //消块逻辑，把正在消失的块变成粒子，原块置空
                for (int i = 0; i < BlockCountX; i++)
                    for (int j = 0; j < BlockCountY; j++)
                    {
                        if (m_blocks[i, j] == null)     //为空块
                        {
                            continue;
                        }
                        if (m_blocks[i, j].IsEating())
                        {
                            //PlayAni(i, j, 0, true);
                            //CreateCapParticle(i, j);
                            //清空block信息
                            MakeSpriteFree(i, j);
                        }
                    }

                //AddCombo();

                if (timerDropDown.GetState() == TimerEnum.EStop)		//若没在下落状态，调用下落。（若在下落状态，等下落计时器到了自然会处理）
                {
                    DropDown();										//下落逻辑
                }
            }
        }

        /*------------------处理timerDropDown------------------*/
        if (timerDropDown.GetState() == TimerEnum.ERunning)
        {
            if (timerDropDown.GetTime() > DROP_TIME)	//如果下落计时器到120毫秒
            {
                timerDropDown.Stop();

                //检查那些块需要检查是否能消
                for (int i = 0; i < BlockCountX; i++)
                {
                    for (int j = BlockCountY - 1; j >= 0; j--)			//从下往上遍历
                    {
                        if (m_blocks[i, j] == null)     //为空块
                        {
                            continue;
                        }
                        if (m_blocks[i, j].isDropping)						//若正在下降且
                        {
                            m_blocks[i, j].m_bNeedCheckEatLine = true;		//完成了下落，等待检查消行
                        }
                    }
                }

                //清空方块的下落标志和偏移量
                for (int i = 0; i < BlockCountX; i++)
                {
                    for (int j = 0 ; j < BlockCountY; j++)
                    {
                        if (m_blocks[i, j] == null)     //为空块
                        {
                            continue;
                        }

                        
                        if (m_blocks[i, j].isDropping)
                        {
                            m_blocks[i, j].saveDropDownState = true;
                            m_blocks[i, j].isDropping = false;
                            m_blocks[i, j].x_move = 0;
                            m_blocks[i, j].y_move = 0;
                        }
                    }
                }

                if (!DropDown())		//尝试下落，若不能
                {
                    //PlaySound(dropdown);				//先播个落地声音
                    if (!EatAllLine())					//若没有能消了的
                    {
                        m_comboCount = 0;
                        OnDropEnd();
                    }
                    else								//若有能直接消除的块
                    {
                        ++m_comboCount;                 //增加Combo
                        timerEatBlock.Play();
                    }
                }

                //找出之前在下落，现在已经不再下落的块
                for (int i = 0; i < BlockCountX; i++)
                {
                    for (int j = 0; j < BlockCountY; j++)
                    {
                        if (m_blocks[i, j] == null)     //为空块
                        {
                            continue;
                        }
                        if (m_blocks[i, j].saveDropDownState && !m_blocks[i, j].isDropping)         //之前是下落状态，现在不是下落状态
                        {
                            if (!m_blocks[i, j].IsEating())         //若没被吃
                            {
                                m_blocks[i, j].m_animation.Play("DropDown");                            //就播放下落动画
                                m_blocks[i, j].saveDropDownState = false;
                            }
                        }
                    }
                }
            }
            //如果未到120毫秒，更新各方快的位置
            for (int i = 0; i < BlockCountX; i++)
            {
                for (int j = 0; j < BlockCountY; j++)
                {
                    if (m_blocks[i, j] == null)
                    {
                        continue;
                    }
                    if (m_blocks[i, j].isDropping)
                    {
                        ProcessDropPic(m_blocks[i, j].droppingFrom, new Position(i, j), (int)timerDropDown.GetTime());
                    }
                }
            }
        }

        /*------------------处理timerMoveBlock------------------*/
        if (timerMoveBlock.GetState() == TimerEnum.ERunning)		//如果交换方块计时器状态为开启
        {
            int passTime = (int)timerMoveBlock.GetTime();
            if (passTime > MOVE_TIME)	//交换方块计时器到了MOVE_TIME
            {
                for (int i = 0; i < BlockCountX; ++i)
                {
                    for (int j = 0; j < BlockCountY; ++j)
                    {
                        m_tempBlocks[i, j] = 0;         //清理临时数组
                    }
                }

                timerMoveBlock.Stop();				//停止计时器
                //清空方块的偏移值
                m_blocks[m_selectedPos[0].x, m_selectedPos[0].y].x_move = 0;
                m_blocks[m_selectedPos[0].x, m_selectedPos[0].y].y_move = 0;
                m_blocks[m_selectedPos[1].x, m_selectedPos[1].y].x_move = 0;
                m_blocks[m_selectedPos[1].x, m_selectedPos[1].y].y_move = 0; ;
                if (m_changeBack)//如果处于换回状态
                {
                    m_changeBack = false;	//清除换回标志
                    if (m_selectedPos[0].x == -1 || m_selectedPos[1].x == -1) return;

                    bool hasEatLine1 = EatLine(m_selectedPos[0]);
                    bool hasEatLine2 = EatLine(m_selectedPos[1]);

                    if (hasEatLine1 || hasEatLine2)				//若有能消的
                    {
                        //PlaySound(eat);

                        if (m_selectedPos[0].x == -1 || m_selectedPos[1].x == -1) return;			//若什么都没选这里返回

                        timerEatBlock.Play();												//开启消块计时器
                    }

                    //清空选择的方块
                    ClearSelected();
                }
                else
                {
                    bool hasEatLine1 = EatLine(m_selectedPos[0]);
                    bool hasEatLine2 = EatLine(m_selectedPos[1]);
                    if (!hasEatLine1 && !hasEatLine2)//如果交换不成功,播放交换回来的动画
                    {
                        ++PlayingStageData.StepLimit;           //步数恢复
                        ExchangeBlock(m_selectedPos[0], m_selectedPos[1]);
                        timerMoveBlock.Play();
                        //PlayAni(m_selectedPos[1].x, m_selectedPos[1].y,GetOtherDirection(m_moveDirection));
                        //PlayAni(m_selectedPos[0].x, m_selectedPos[0].y,m_moveDirection);
                        m_changeBack = true;
                    }
                    else
                    {					//如果交换成功
                        m_workedClickCount++;
                        //PlaySound(eat);

                        if (m_selectedPos[0].x == -1 || m_selectedPos[1].x == -1) return;

                        ClearSelected();
                        timerEatBlock.Play();												//开启消块计时器
                    }
                }
                ProcessTempBlocks();
            }

            passTime = (int)timerMoveBlock.GetTime();
            int moveTime = MOVE_TIME - passTime;		//重新获取一次passtime，因为前面有可能被刷新过
            ProcessMovePic(m_selectedPos[0], m_selectedPos[1], moveTime);
        }
    }

    void ProcessTempBlocks()
    {
        for (int i = 0; i < BlockCountX; ++i)
        {
            for (int j = 0; j < BlockCountY; ++j)
            {
                if (m_tempBlocks[i, j] > 0)         //若有消除（正常或功能消除）
                {
                    if (PlayingStageData.CheckFlag(i, j, GridFlag.Stone))
                    {
                        PlayingStageData.ClearFlag(i, j, GridFlag.Stone);
                        PlayingStageData.ClearFlag(i, j, GridFlag.NotGenerateCap);
                        PlayingStageData.AddFlag(i, j, GridFlag.GenerateCap);

                        AddPartile("StoneEffect", i, j);
                    }
                    else if (PlayingStageData.CheckFlag(i, j, GridFlag.Chocolate))
                    {
                        PlayingStageData.ClearFlag(i, j, GridFlag.Chocolate);
                        PlayingStageData.ClearFlag(i, j, GridFlag.NotGenerateCap);
                        PlayingStageData.AddFlag(i, j, GridFlag.GenerateCap);
                        AddPartile("ChocolateEffect", i, j);
                    }
                    else if (PlayingStageData.CheckFlag(i, j, GridFlag.Cage))
                    {
                        PlayingStageData.ClearFlag(i, j, GridFlag.Cage);
                        AddPartile("CageEffect", i, j);
                        m_blocks[i, j].isLocked = false;
                    }
                    else if (PlayingStageData.CheckFlag(i, j, GridFlag.JellyDouble))
                    {
                        PlayingStageData.ClearFlag(i, j, GridFlag.JellyDouble);
                        PlayingStageData.AddFlag(i, j, GridFlag.Jelly);
                        AddPartile("JellyEffect", i, j);
                    }
                    else if (PlayingStageData.CheckFlag(i, j, GridFlag.Jelly))
                    {
                        PlayingStageData.ClearFlag(i, j, GridFlag.Jelly);
                        AddPartile("JellyEffect", i, j);
                    }

                    ClearChocolateAround(i, j);
                }

                if (m_tempBlocks[i, j] == 2)        //若有正常消除
                {
                    ClearStoneAround(i, j);         //清周围的石块
                }
            }
        }
        for (int i = 0; i < BlockCountX; ++i)
        {
            for (int j = 0; j < BlockCountY; ++j)
            {
                m_tempBlocks[i, j] = 0;         //清理临时数组
            }
        }
    }

    void ProcessMovePic(Position from, Position to, int moveTime)
    {
        if (from.x == -1 || to.x == -1) return;
        if (from.x != to.x)		//若x方向上的值不一样，就有x方向上的移动
        {
            m_blocks[from.x, from.y].x_move = (to.x - from.x) * moveTime * BLOCKWIDTH / MOVE_TIME;
            m_blocks[to.x, to.y].x_move = (from.x - to.x) * moveTime * BLOCKWIDTH / MOVE_TIME;
        }
        if (from.x - to.x == 0)
        {
            m_blocks[from.x, from.y].y_move = (to.y - from.y) * moveTime * BLOCKHEIGHT / MOVE_TIME;
            m_blocks[to.x, to.y].y_move = (from.y - to.y) * moveTime * BLOCKHEIGHT / MOVE_TIME;
        }
        else
        {
            if (from.y != to.y)
            {
                m_blocks[from.x, from.y].y_move = (to.y - from.y) * moveTime * (BLOCKHEIGHT / 2) / MOVE_TIME;
                m_blocks[to.x, to.y].y_move = (from.y - to.y) * moveTime * (BLOCKHEIGHT / 2) / MOVE_TIME;
            }
            else
            {
                if (from.x % 2 == 0)
                {
                    m_blocks[from.x, from.y].y_move = 0 - moveTime * (BLOCKHEIGHT / 2) / MOVE_TIME;
                    m_blocks[to.x, to.y].y_move = moveTime * (BLOCKHEIGHT / 2) / MOVE_TIME;
                }
                else
                {
                    m_blocks[from.x, from.y].y_move = moveTime * (BLOCKHEIGHT / 2) / MOVE_TIME;
                    m_blocks[to.x, to.y].y_move = 0 - moveTime * (BLOCKHEIGHT / 2) / MOVE_TIME;
                }
            }
        }
    }

    void ProcessDropPic(Position from, Position to, int dropTime)
    {
        if (from == null)        //生成点，只能垂直向下移动
        {
            m_blocks[to.x, to.y].y_move = dropTime * BLOCKHEIGHT / DROP_TIME - BLOCKHEIGHT;
            return;
        }

        if (from.x != to.x)		//若x方向上的值不一样，就有x方向上的移动
        {
            m_blocks[to.x, to.y].x_move = (to.x - from.x) * (dropTime * BLOCKWIDTH / DROP_TIME - BLOCKWIDTH);
        }
        if (from.x - to.x == 0)
        {
            m_blocks[to.x, to.y].y_move = dropTime * BLOCKHEIGHT / DROP_TIME - BLOCKHEIGHT;
        }
        else
        {
            if (from.y != to.y)
            {
                m_blocks[to.x, to.y].y_move = dropTime * (BLOCKHEIGHT / 2) / DROP_TIME - BLOCKHEIGHT / 2;
            }
            else
            {
                if (from.x % 2 == 1)
                {
                    m_blocks[to.x, to.y].y_move = dropTime * (BLOCKHEIGHT / 2) / DROP_TIME - BLOCKHEIGHT / 2;
                }
                else
                {
                    m_blocks[to.x, to.y].y_move = 0 - (dropTime * (BLOCKHEIGHT / 2) / DROP_TIME - BLOCKHEIGHT / 2);
                }
            }
        }
    }

    bool DropDown()
    {
        //下落块，所有可下落区域下落一行////////////////////////////////////////////////////////////////////////
        bool tag = false;
        Position dropDest = new Position();          //掉落的目标点
        Position dropFrom = new Position();          //从哪里开始掉落
        bool bNeedCheckAgain = true;
        while (bNeedCheckAgain)
        {
            bNeedCheckAgain = false;
            for (int j = BlockCountY - 1; j >= 0; j--)		//从最下面开始遍历
            {
                for (int i = 0; i < BlockCountX; i++)				//一次遍历一行
                {
                    bool bDrop = false;
                    bool UpLocked = false;              //上面被锁住

                    if (m_blocks[i, j] == null)       //找到空块
                    {
                        if (PlayingStageData.GridData[i, j] == 0 || PlayingStageData.CheckFlag(i, j, GridFlag.Stone | GridFlag.Cage | GridFlag.Chocolate))		//若为镂空块，石头，不判断
                        {
                            continue;
                        }

                        if (PlayingStageData.CheckFlag(i, j, GridFlag.Birth))
                        {
                            continue;                   //出生点空了时，不在这里处理下落
                        }
                        dropDest.Set(i, j);
                        //先看是否在传送点
                        if (PlayingStageData.CheckFlag(dropDest.x, dropDest.y, GridFlag.PortalEnd))
                        {
                            dropFrom.Assign(PlayingStageData.PortalToMap[dropDest.ToInt()].from);

                            if (m_blocks[dropFrom.x, dropFrom.y] != null                    //传送门入口处有有效块
                                && m_blocks[dropFrom.x, dropFrom.y].isLocked == false)      //可以下落 
                            {
                                bDrop = true;       //可以下落
                            }
                        }

                        if (!bDrop)                    //若没找到掉落点
                        {
                            dropFrom.Set(i, j - 1);     //向上看一格
                            if (CheckPosAvailable(dropFrom))            //先看看格子是否有效
                            {
                                if (PlayingStageData.GridData[dropFrom.x, dropFrom.y] == 0 ||
                                    PlayingStageData.CheckFlag(dropFrom.x, dropFrom.y, GridFlag.Stone | GridFlag.Cage | GridFlag.Chocolate))
                                {
                                    UpLocked = true;
                                }
                                else if (m_blocks[dropFrom.x, dropFrom.y] != null && !m_blocks[dropFrom.x, dropFrom.y].isDropping && !m_blocks[dropFrom.x, dropFrom.y].isLocked)           //找到有效块
                                {
                                    bDrop = true;       //可以下落
                                }
                            }
                        }
                        if (UpLocked)                    //若没找到掉落点，且上方的点被锁
                        {
                            dropFrom = GoTo(new Position(i, j), TDirection.EDir_LeftUp, 1);         //向左上看一格
                            if (CheckPosAvailable(dropFrom) && m_blocks[dropFrom.x, dropFrom.y] != null && !m_blocks[dropFrom.x, dropFrom.y].isDropping && !m_blocks[dropFrom.x, dropFrom.y].isLocked)        //若是有效点，且为空，形成掉落
                            {
                                bDrop = true;
                            }
                        }
                        if (!bDrop && UpLocked)                    //若没找到掉落点，且上方的点被锁
                        {
                            dropFrom = GoTo(new Position(i, j), TDirection.EDir_UpRight, 1);         //向右上看一格
                            if (CheckPosAvailable(dropFrom) && m_blocks[dropFrom.x, dropFrom.y] != null && !m_blocks[dropFrom.x, dropFrom.y].isDropping && !m_blocks[dropFrom.x, dropFrom.y].isLocked)        //若是有效点，且为空，形成掉落
                            {
                                bDrop = true;
                            }
                        }

                        if (bDrop)      //处理下落
                        {
                            m_blocks[dropDest.x, dropDest.y] = m_blocks[dropFrom.x, dropFrom.y];
                            m_blocks[dropDest.x, dropDest.y].isDropping = true;
                            m_blocks[dropDest.x, dropDest.y].droppingFrom = new Position(dropFrom);

                            //若是出生点，生成新块////////////////////////////////////////////////////////////////////////
                            if (PlayingStageData.CheckFlag(dropFrom.x, dropFrom.y, GridFlag.Birth))
                            {
                                //补充新的方块
                                CreateBlock(dropFrom.x, dropFrom.y, false);
                                m_blocks[dropFrom.x, dropFrom.y].isDropping = true;
                                m_blocks[dropFrom.x, dropFrom.y].droppingFrom = null;       //droppingFrom置空代表从上方掉落
                            }
                            else
                            {
                                m_blocks[dropFrom.x, dropFrom.y] = null;            //原先点置空

                                if (dropFrom.x % 2 == 1)
                                {
                                    bNeedCheckAgain = true;             //，因为中间产生了新的空位，需要再检查一次空位
                                }
                            }
                            tag = true;
                        }
                    }        
                }
            }
        }


        //需要补充遍历所有出生点和离开点（坚果）
        for (int j = BlockCountY - 1; j >= 0; j--)		//从最下面开始遍历
        {
            for (int i = 0; i < BlockCountX; i++)				//一次遍历一行
            {
                if (PlayingStageData.CheckFlag(i, j, GridFlag.Birth) && m_blocks[i, j] == null)     //若为出生点
                {
                    CreateBlock(i, j, false);
                    m_blocks[i, j].isDropping = true;
					m_blocks[i, j].droppingFrom = null;
                    tag = true;
                }

                if (PlayingStageData.Target == GameTarget.BringFruitDown && PlayingStageData.CheckFlag(i, j, GridFlag.FruitExit) && 
                    m_blocks[i, j] != null && m_blocks[i, j].color > TBlockColor.EColor_Grey)       //若到退出点且为坚果
                {
					//记录吃一个坚果
					if (m_blocks[i, j].color == TBlockColor.EColor_Nut1)
					{
                        PlayingStageData.Nut1Count++;
                        --m_nut1Count;
					}

                    if (m_blocks[i, j].color == TBlockColor.EColor_Nut2)
                    {
                        PlayingStageData.Nut2Count++;
                        --m_nut2Count;
                    }
					
                    MakeSpriteFree(i, j);           //离开点吃掉坚果
                }
            }
        }

        if (tag)
            timerDropDown.Play();								//开启自由下落计时器

        return tag;			//返回是否发生了掉落
    }

    bool EatAllLine()
    {
        bool tag = false;
        for (int i = 0; i < BlockCountX; i++)
        {
            for (int j = 0; j < BlockCountY; j++)
            {
                if (m_blocks[i, j] == null)
                {
                    continue;
                }
                if (m_blocks[i, j].m_bNeedCheckEatLine && !m_blocks[i, j].IsEating())		//只有没在消除状态的块才遍历，防止重复记分
                {
                    if (EatLine(new Position(i, j)))
                    {
                        tag = true;
                    }
                }
            }
        }

        ProcessTempBlocks();
        return tag;
    }

    int [,] m_tempBlocks = new int[BlockCountX, BlockCountY];		//一个临时数组，用来记录哪些块要消除, 0 代表不消除 1 代表功能块消除  2代表正常消除

    bool EatLine(Position position)
    {
        int countInSameLine = 1;					//在同一条线上相同的颜色的数量
        int totalSameCount = 1;						//总共相同颜色的数量
        int maxCountInSameDir = 1;					//最大的在同一个方向上相同颜色的数量
        Position[] eatBlockPos = new Position[10];
        eatBlockPos[0] = position;
        Position availablePos = null;

        TBlockColor color = GetBlockColor(position);
        if (color > TBlockColor.EColor_Grey)
        {
            return false;
        }
        Position curPos;
        for (TDirection dir = TDirection.EDir_Up; dir <= TDirection.EDir_DownRight; dir = (TDirection)(dir + 1))		//遍历3个方向
        {
            countInSameLine = 1;
            curPos = position;
            while (true)
            {
                curPos = GoTo(curPos, dir, 1);									//沿着Dir方向走一步
                if (!CheckPosAvailable(curPos))
                {
                    break;
                }
                if (GetBlockColor(curPos) != color)								//若碰到不一样的颜色就停下来
                {
                    break;
                }
                eatBlockPos[countInSameLine] = curPos;								//把Block存起来（用来后面消除）
                ++countInSameLine;
            }
            curPos = position;														//重置位置
            while (true)
            {
                curPos = GoTo(curPos, GetOtherDirection(dir), 1);					//沿着Dir反方向走
                if (!CheckPosAvailable(curPos))
                {
                    break;
                }
                if (GetBlockColor(curPos) != color)								//若碰到不一样的颜色就停下来
                {
                    break;
                }
                eatBlockPos[countInSameLine] = curPos;
                ++countInSameLine;
            }
            if (countInSameLine > maxCountInSameDir)
            {
                maxCountInSameDir = countInSameLine;								//记录在单行中的最大消除数量
                if (maxCountInSameDir > 3)          //若可以生成特殊块
                {
                    //先查找一个可以生成特殊块的位置
                    if (m_blocks[position.x, position.y].special == TSpecialBlock.ESpecial_Normal)
                    {
                        availablePos = position;
                    }
                    else
                    {
                        for (int i = 0; i < countInSameLine; ++i)
                        {
                            if (m_blocks[eatBlockPos[i].x, eatBlockPos[i].y].special == TSpecialBlock.ESpecial_Normal)
                            {
                                availablePos = eatBlockPos[i];
                                break;
                            }
                        }
                    }
                }
            }

            //一条线处理一次消除
            if (countInSameLine >= 3)
            {
                for (int i = 0; i < countInSameLine; ++i)
                {
                    m_tempBlocks[eatBlockPos[i].x, eatBlockPos[i].y] = 2;           //记录正常消除
                }
                totalSameCount += (countInSameLine - 1);							//记录总的消除数量，减1是因为起始块是各条线公用的
            }
        }

        if (maxCountInSameDir < 3)		//若没产生消除，返回
        {
            return false;
        }

        int kItem = 0;                  //自然消除为0

        TSpecialBlock generateSpecial = TSpecialBlock.ESpecial_Normal;      //用来记录生成的特殊块

        if (totalSameCount == 3)		//总共就消了3个
        {
            m_tempBlocks[position.x, position.y] = 2;           //记录正常消除
        }
        //根据结果来生成道具////////////////////////////////////////////////////////////////////////
		else if (maxCountInSameDir >= 5)		//若最大每行消了5个
        {
            if (m_blocks[position.x, position.y].special == TSpecialBlock.ESpecial_NormalPlus5)
            {
                m_gameStartTime += 5000;               //增加5秒时间
            }
            generateSpecial = TSpecialBlock.ESpecial_EatAColor;         //生成彩虹
            AddProgress(2000);
            kItem = 3;
        }
        else if (maxCountInSameDir == 4)		//若最大每行消了4个
        {
            if (m_blocks[position.x, position.y].special == TSpecialBlock.ESpecial_NormalPlus5)
            {
                m_gameStartTime += 5000;               //增加5秒时间
            }
            if (m_moveDirection == TDirection.EDir_Up || m_moveDirection == TDirection.EDir_Down)
            {
                generateSpecial = TSpecialBlock.ESpecial_EatLineDir0;
            }
            if (m_moveDirection == TDirection.EDir_UpRight || m_moveDirection == TDirection.EDir_LeftDown)
            {
                generateSpecial = TSpecialBlock.ESpecial_EatLineDir1;
            }
            if (m_moveDirection == TDirection.EDir_DownRight || m_moveDirection == TDirection.EDir_LeftUp)
            {
                generateSpecial = TSpecialBlock.ESpecial_EatLineDir2;
            }
            AddProgress(500);
            kItem = 1;
        }
        else if (totalSameCount >= 6)			//若总共消除大于等于6个（3,4消除或者多个3消）
        {
            if (m_blocks[position.x, position.y].special == TSpecialBlock.ESpecial_NormalPlus5)
            {
                m_gameStartTime += 5000;               //增加5秒时间
            }
            generateSpecial = TSpecialBlock.ESpecial_Bomb;
            AddProgress(600);
            kItem = 2;
        }
        else if (totalSameCount > 4)			//若总共消除大于4个
        {
            if (m_blocks[position.x, position.y].special == TSpecialBlock.ESpecial_NormalPlus5)
            {
                m_gameStartTime += 5000;               //增加5秒时间
            }
            generateSpecial = TSpecialBlock.ESpecial_Bomb;
            AddProgress(600);
            kItem = 1;
        }

        if (availablePos != null && generateSpecial != TSpecialBlock.ESpecial_Normal)              //若生成了特殊块，且有可放的地方
        {
            m_blocks[availablePos.x, availablePos.y].special = generateSpecial;                                                 //生成特殊块
            m_blocks[availablePos.x, availablePos.y].RefreshBlockSprite(PlayingStageData.GridData[position.x, position.y]);     //刷新图标
            m_tempBlocks[availablePos.x, availablePos.y] = 2;                                                                   //记录正常消除
        }

        for (int i = 0; i < BlockCountX; ++i )
        {
            for (int j = 0; j < BlockCountY; ++j )
            {
                if (m_tempBlocks[i, j] == 2 && (availablePos == null || i != availablePos.x || j != availablePos.y))      //正常消除且不为新生成块部位
                {
                    EatBlock(new Position(i, j));       //吃掉
                }
            }
        }

        //TODO 记分
        ////根据结果来记分
        int kQuantity = 1;
        int kCombo = 1;
        int kLevel = 0;

        if (totalSameCount >= CapsConfig.Instance.MaxKQuanlity)
        {
            kQuantity = CapsConfig.Instance.KQuanlityTable[CapsConfig.Instance.MaxKQuanlity - 3];
        }
        else
        {
            kQuantity = CapsConfig.Instance.KQuanlityTable[totalSameCount - 3];
        }

        if (m_comboCount + 1 >= CapsConfig.Instance.MaxKCombo)
        {
            kCombo = CapsConfig.Instance.KComboTable[CapsConfig.Instance.MaxKCombo - 1];
        }
        else
        {
            kCombo = CapsConfig.Instance.KComboTable[m_comboCount + 1];
        }

        AddProgress(50 * (kQuantity + kCombo + kItem + kLevel)); 
        OnProgressChange();
        return true;
    }

    Position FindRandomPos(TBlockColor excludeColor, Position [] excludePos,bool excludeSpecial = false)       //找到某个颜色的随机一个块, 简易算法，性能不好
    {
        int ranNum = m_random.Next()%(BlockCountX*BlockCountY);
        int count = 0;
        for (int i = 0; i < BlockCountX; i++)				//遍历一行
        {
            for (int j = 0; j < BlockCountY; j++)		//遍历一列
            {
				if (count < ranNum)     //先找开始位置
                {
					++count;
                    continue;
                }
				
                if (m_blocks[i, j] == null || m_blocks[i,j].color > TBlockColor.EColor_Grey
					|| m_blocks[i, j].color == excludeColor || m_blocks[i,j].special == TSpecialBlock.ESpecial_EatAColor || m_blocks[i, j].IsEating())
                {
					if(i == BlockCountX -1 && j == BlockCountY -1)//Repeat the loop till get a result
					{
						i=0;
						j=0;
					}
                    continue;
                }

                if (excludeSpecial && m_blocks[i, j].special != TSpecialBlock.ESpecial_Normal)      //检查不是Speical
                {
                    if (i == BlockCountX - 1 && j == BlockCountY - 1)//Repeat the loop till get a result
                    {
                        i = 0;
                        j = 0;
                    }
                    continue;
                }

                Position pos = new Position(i, j);
                bool bFind = false;
                if (excludePos != null)
                {
                    for (int k = 0; k < excludePos.Length; ++k)
                    {
                        if (excludePos[k] == pos)
                        {
                            bFind = true;
                            break;
                        }
                    }
                }
                if (!bFind)
                {
                    return pos;
                }

				if(count >= ranNum + BlockCountX*BlockCountY)
				{
					break;
				}
				
				if(i == BlockCountX -1 && j == BlockCountY -1)//Repeat the loop till get a result
				{
					i=0;
					j=0;
				}
            }
        }
        return new Position(-1, -1);
    }

    void ChangeColor(Position pos, TBlockColor color)
    {
        //更改颜色的操作
        m_blocks[pos.x, pos.y].color = color;
        m_blocks[pos.x, pos.y].RefreshBlockSprite(PlayingStageData.GridData[pos.x, pos.y]);
    }

    void ClearChocolateAround(int x, int y)      //清除周围的巧克力
    {
        for (int i = 0; i < 6; ++i)
        {
            Position pos = GoTo(new Position(x, y), (TDirection)i, 1);
            if (CheckPosAvailable(pos))
            {
                if (PlayingStageData.CheckFlag(pos.x, pos.y, GridFlag.Chocolate))
                {
                    PlayingStageData.ClearFlag(pos.x, pos.y, GridFlag.Chocolate);
                    PlayingStageData.ClearFlag(x, y, GridFlag.NotGenerateCap);
                    PlayingStageData.AddFlag(x, y, GridFlag.GenerateCap);
                }
            }
        }
    }

    void ClearStoneAround(int x, int y)        //消除周围的石块
    {
        for (int i = 0; i < 6; ++i )
        {
            Position pos = GoTo(new Position(x, y), (TDirection)i, 1);
            if (CheckPosAvailable(pos))
            {
                if (PlayingStageData.CheckFlag(pos.x, pos.y, GridFlag.Stone))
                {
                    PlayingStageData.ClearFlag(pos.x, pos.y, GridFlag.Stone);
                    PlayingStageData.ClearFlag(x, y, GridFlag.NotGenerateCap);
                    PlayingStageData.AddFlag(x, y, GridFlag.GenerateCap);

                    AddPartile("StoneEffect", pos.x, pos.y);
                }
            }
        }
    }

    void ClearHelpPoint()
    {
        if (m_dropDownEndTime > 0 && helpP1 != null)
        {
            if (m_blocks[helpP1.x, helpP1.y] != null)
            {
                m_blocks[helpP1.x, helpP1.y].m_animation.Stop();
                m_blocks[helpP1.x, helpP1.y].m_animation.transform.localScale = new Vector3(60.0f, 60.0f, 60.0f);          //恢复缩放
            }
            if (m_blocks[helpP2.x, helpP2.y] != null)
            {
                m_blocks[helpP2.x, helpP2.y].m_animation.Stop();
                m_blocks[helpP2.x, helpP2.y].m_animation.transform.localScale = new Vector3(60.0f, 60.0f, 60.0f);          //恢复缩放
            }
            m_dropDownEndTime = 0;                          //清除dropDownEnd的时间记录
            helpP1 = null;                                  //清除帮助点
            helpP2 = null;                                  //清除帮助点
        }
    }

    void EatBlock(Position position)
    {
        if (position.x >= BlockCountX || position.y >= BlockCountY || position.x < 0 || position.y < 0)
            return;

        m_tempBlocks[position.x, position.y] = 1;       //记录吃块，用来改变Grid属性
		
		if (m_blocks[position.x, position.y] == null) return;

        if (m_blocks[position.x, position.y].IsEating())        //不重复消除
        {
            return;
        }

        ClearHelpPoint();           //Todo 这个不该放这里，应该只需要调一次

        if (PlayingStageData.CheckFlag(position.x, position.y, GridFlag.Cage)) return;                       //有笼子的块不消(先消笼子)

        if (m_blocks[position.x, position.y].color > TBlockColor.EColor_Grey) return;                        //

        if (m_blocks[position.x, position.y].x_move > 0 || m_blocks[position.x, position.y].y_move > 0)     //正在移动的不能消除
            return;

        m_blocks[position.x, position.y].Eat();			//吃掉当前块

        switch (m_blocks[position.x, position.y].special)
        {
            case TSpecialBlock.ESpecial_Bomb:
                {
                    for (TDirection dir = TDirection.EDir_Up; dir <= TDirection.EDir_LeftUp; ++dir )
                    {
                        //TODO 这里要放特效
                        Position newPos = GoTo(position, dir, 1);
                        EatBlock(newPos);
                    }
                }
                break;
            case TSpecialBlock.ESpecial_NormalPlus5:
                {
                    m_gameStartTime += 5000;               //增加5秒时间
                }
                break;
            case TSpecialBlock.ESpecial_Painter:
                {
                    Position[] excludePos = new Position[4];
                    for (int i = 0; i < 4; ++i )
                    {
                        excludePos[i] = position;           //先用当前位置初始化排除的位置数组
                    }
                    for (int i = 0; i < 3; ++i )            //取得随机点
                    {
                        excludePos[i + 1] = FindRandomPos(m_blocks[position.x, position.y].color, excludePos);
                    }
                    //TODO 这里要放特效
                    for (int i = 1; i < 4; ++i )
                    {
                        ChangeColor(excludePos[i], m_blocks[position.x, position.y].color);
                    }
                }
                break;
            case TSpecialBlock.ESpecial_EatLineDir0:
                {
                    for (int i = 0; i < BlockCountX; ++i )
                    {
                        EatBlock(GoTo(position, TDirection.EDir_Down, i));
                        EatBlock(GoTo(position, TDirection.EDir_Up, i));
                    }
                }
                break;
            case TSpecialBlock.ESpecial_EatLineDir1:
                {
                    for (int i = 1; i < BlockCountX - 1; ++i)
                    {
                        EatBlock(GoTo(position, TDirection.EDir_UpRight, i));
                        EatBlock(GoTo(position, TDirection.EDir_LeftDown, i));
                    }
                }
                break;
            case TSpecialBlock.ESpecial_EatLineDir2:
                {
                    for (int i = 0; i < BlockCountX; ++i)
                    {
                        EatBlock(GoTo(position, TDirection.EDir_LeftUp, i));
                        EatBlock(GoTo(position, TDirection.EDir_DownRight, i));
                    }
                }
                break;
            case TSpecialBlock.ESpecial_EatAColor:
                {
                    EatAColor(GetRandomColor(false));
                }
                break;
        }

        m_blocks[position.x, position.y].m_animation.Play("Eat");
        AddPartile("EatEffect", position.x, position.y);
    }

    public void AddPartile(string name, int x, int y)
    {

        

        //先看freeParticleList里面有没有可用的
        LinkedList<ParticleSystem> freeParticleList;
        if (!m_freeParticleMap.TryGetValue(name, out freeParticleList))
        {
            freeParticleList = new LinkedList<ParticleSystem>();
            m_freeParticleMap.Add(name, freeParticleList);
        }

        GameObject gameObj = null;
        ParticleSystem par = null;

        if (freeParticleList.Count > 0)     //若有,从列表里取用
        {
            par = freeParticleList.First.Value;
            gameObj = freeParticleList.First.Value.gameObject;
            freeParticleList.RemoveFirst();

            par.gameObject.SetActive(true);
            par.Play();                     //播放
        }
        else   //没有，创建新粒子
        {
            //Todo 临时加的粒子代码
            Object obj = Resources.Load(name);
            gameObj = GameObject.Instantiate(obj) as GameObject;
            gameObj.transform.parent = m_capsPool.transform;
            par = gameObj.GetComponent<ParticleSystem>();
        }

        gameObj.transform.localPosition = new Vector3(GetXPos(x), -GetYPos(x, y), -200);        //指定位置

        //放到正在播放的列表里
        LinkedList<ParticleSystem> particleList;
        if (!m_particleMap.TryGetValue(name, out particleList))
        {
            particleList = new LinkedList<ParticleSystem>();
            m_particleMap.Add(name, particleList);
        }

        particleList.AddLast(par);
    }

    public bool CheckStageFinish()                  //检测关卡结束条件
    {
        if (PlayingStageData.Target == GameTarget.ClearJelly)       //若目标为清果冻，计算果冻数量
        {
            if (PlayingStageData.GetJellyCount() == 0)       //若完成目标
            {
                return true;
            }
        }
        else if (PlayingStageData.Target == GameTarget.BringFruitDown)      //看看水果有没有都落地
        {
            if (PlayingStageData.Nut1Count == GlobalVars.CurStageData.Nut1Count && PlayingStageData.Nut2Count == GlobalVars.CurStageData.Nut2Count)
            {
                return true;
            }
        }

        if (GlobalVars.CurStageData.StepLimit > 0 && PlayingStageData.StepLimit == 0)            //限制步数的关卡步用完了
        {
            return true;
        }

        if (GlobalVars.CurStageData.TimeLimit > 0 && (Timer.millisecondNow() - m_gameStartTime) / 1000.0f > GlobalVars.CurStageData.TimeLimit)
        {
            return true;
        }
        return false;
    }

    void OnDropEnd()            //所有下落和移动结束时被调用
    {
        if (CheckStageFinish())                 //检查游戏是否结束
        {
            bool foundSpecial = false;
            for (int i = 0; i < BlockCountX; ++i )
            {
                for (int j = 0; j < BlockCountY; ++j )
                {
                    if (m_blocks[i, j] != null && m_blocks[i, j].special != TSpecialBlock.ESpecial_Normal)
                    {
                        foundSpecial = true;
                        break;
                    }
                }
            }
            if (foundSpecial || PlayingStageData.StepLimit > 0)     //若能进SugarCrush
            {
                m_gameState = TGameState.EGameState_SugarCrushAnim;
                m_sugarCurshAnimStartTime = Timer.millisecondNow();
            }
            else
            {
                //否则结束游戏
                m_gameStartTime = 0;
                m_gameState = TGameState.EGameState_End;
                UIWindowManager.Singleton.GetUIWindow<UIGameEnd>().ShowWindow();
            }
            return;
        }

        m_dropDownEndTime = Time.realtimeSinceStartup;
    }

    public void OnTap(int x, int y)
    {
        //不在游戏区，不处理
        if (x < gameAreaX || y < gameAreaY || x > gameAreaX + gameAreaWidth || y > gameAreaY + gameAreaHeight)
        {
            return;
        }

        Position p = new Position();
        p.x = (x - gameAreaX) / BLOCKWIDTH;
        if (p.x % 2 == 0)
            p.y = (y - gameAreaY - BLOCKHEIGHT / 2) / BLOCKHEIGHT;
        else
            p.y = (y - gameAreaY) / BLOCKHEIGHT;
        if (p.y > BlockCountY) p.y = BlockCountY;

        if (GlobalVars.EditState == TEditState.ChangeColor)
        {
            ChangeColor(p, GlobalVars.EditingColor);
            m_nut1Count = 0;
            m_nut2Count = 0;
            for (int i = 0; i < BlockCountX; ++i )
            {
                for (int j = 0; j < BlockCountY; ++j )
                {
                    if (m_blocks[i, j].color == TBlockColor.EColor_Nut1)
                    {
                        m_nut1Count++;
                    }

                    if (m_blocks[i, j].color == TBlockColor.EColor_Nut2)
                    {
                        m_nut2Count++;
                    }
                }
            }
        }

        if (GlobalVars.EditState == TEditState.ChangeSpecial)
        {
            m_blocks[p.x, p.y].special = GlobalVars.EditingSpecial;
            m_blocks[p.x, p.y].RefreshBlockSprite(PlayingStageData.GridData[p.x, p.y]);
        }

        if (GlobalVars.EditState == TEditState.EditStageGrid)
        {
            PlayingStageData.GridData[p.x, p.y] = GlobalVars.EditingGrid;
            if ((GlobalVars.EditingGrid & (int)GridFlag.Cage) > 0)
            {
                m_blocks[p.x, p.y].isLocked = true;
            }
            if ((GlobalVars.EditingGrid & (int)GridFlag.Stone) > 0 || (GlobalVars.EditingGrid & (int)GridFlag.Chocolate) > 0)
            {
                MakeSpriteFree(p.x, p.y);       //把块置空
            }
        }

        if (GlobalVars.EditState == TEditState.Eat)
        {
            EatBlock(p);
            timerEatBlock.Play();												//开启消块计时器
        }

        if (GlobalVars.EditState == TEditState.EditPortal)
        {
            if (GlobalVars.EditingPortal.from == null)                          //在编辑第一个点
            {
                if (PlayingStageData.CheckFlag(p.x, p.y, GridFlag.PortalStart)) //若所选的位置已经是开始点了，不能编辑
                {
                    GlobalVars.EditingPortalTip = "选择了重复的开始点, 重新选择Pos1";
                    return;
                }
                GlobalVars.EditingPortal.from = p;
                GlobalVars.EditingPortalTip = "Edit Portal: 选择Pos2";
            }
            else
            {
                if (p == GlobalVars.EditingPortal.from)                         //起点终点不能重合
                {
                    GlobalVars.EditingPortalTip = "起始点和终点不能是同一点, 重新选择Pos2";
                    return;
                }

                if (PlayingStageData.CheckFlag(p.x, p.y, GridFlag.PortalEnd)) //若所选的位置已经是终点了，不能编辑
                {
                    GlobalVars.EditingPortalTip = "选择了重复的结束点, 重新选择Pos2";
                    return;
                }

                GlobalVars.EditingPortal.to = p;

                PlayingStageData.PortalToMap.Add(p.ToInt(), GlobalVars.EditingPortal);    //把在编辑的Portal存起来
                PlayingStageData.PortalFromMap.Add(GlobalVars.EditingPortal.from.ToInt(), GlobalVars.EditingPortal);    //把在编辑的Portal存起来

                PlayingStageData.AddFlag(GlobalVars.EditingPortal.from.x, GlobalVars.EditingPortal.from.y, GridFlag.Portal);
                PlayingStageData.AddFlag(GlobalVars.EditingPortal.from.x, GlobalVars.EditingPortal.from.y, GridFlag.PortalStart);
                PlayingStageData.AddFlag(GlobalVars.EditingPortal.to.x, GlobalVars.EditingPortal.to.y, GridFlag.Portal);
                PlayingStageData.AddFlag(GlobalVars.EditingPortal.to.x, GlobalVars.EditingPortal.to.y, GridFlag.PortalEnd);

                int flag = GlobalVars.EditingPortal.flag;
                GlobalVars.EditingPortal = new Portal();
                GlobalVars.EditingPortal.flag = flag;

                GlobalVars.EditingPortalTip = "Edit Portal: 添加成功， 重新选择Pos1";
            }
        }
    }

    public void OnTouchBegin(int x, int y)
    {
	    //不在游戏区，先不处理
	    if(x < gameAreaX || y < gameAreaY || x > gameAreaX+gameAreaWidth || y > gameAreaY+gameAreaHeight)
	    {
		    return;
	    }

        if (timerMoveBlock.GetState() == TimerEnum.ERunning) return;

	    Position p = new Position();
	    p.x=(x-gameAreaX)/BLOCKWIDTH;
	    if(p.x%2==0)
            p.y = (y - gameAreaY - BLOCKHEIGHT / 2) / BLOCKHEIGHT;
	    else
            p.y = (y - gameAreaY) / BLOCKHEIGHT;
	    if(p.y>BlockCountY)p.y=BlockCountY;

        //如果选中一个状态处于不可移动的块，或者一个特殊块，置选中标志为空，返回
        if (m_blocks[p.x, p.y] == null || !m_blocks[p.x, p.y].SelectAble())
        {
            ClearSelected();
            return;
        }

        touchBeginPos = new Position(x, y);
        m_selectedPos[0] = p;
        if (m_selectedPos[0].x == -1) return;
        m_totalClickCount++;
        m_selectedPos[1].x = -1;
        long clickTime = Timer.millisecondNow();
        m_perClickTakeTime.AddLast(clickTime - m_lastClickTime);
        m_lastClickTime = clickTime;
        //SetSelectAni(p.x, p.y);
    }

    public void OnTouchMove(int x, int y)
    {
        if (timerDropDown.GetState() != TimerEnum.EStop)
        {
            return;
        }

        if (timerMoveBlock.GetState() == TimerEnum.ERunning) return;        //移动时不能再移动

        if (GlobalVars.CurStageData.StepLimit > 0 && PlayingStageData.StepLimit == 0)       //若已经没有步数了
        {
            return;
        }

        if (m_selectedPos[0].x == -1)		//若没选好第一个块，不处理
        {
            return;
        }

        float lenth = Vector2.Distance(new Vector2(x, y), new Vector2(touchBeginPos.x, touchBeginPos.y));       //移动距离
        if (lenth < BLOCKWIDTH * 0.6f)             //移动距离不够不行
        {
            return;
        }

        //不在游戏区，先不处理
        if (x < gameAreaX - BLOCKWIDTH || y < gameAreaY - BLOCKHEIGHT || x > gameAreaX + gameAreaWidth + BLOCKWIDTH || y > gameAreaY + gameAreaHeight + BLOCKHEIGHT)
        {
            return;
        }

        TDirection dir = TDirection.EDir_Up;
        Position p;
        float oringinX = GetXPos(m_selectedPos[0].x);
        float oringinY = GetYPos(m_selectedPos[0].x, m_selectedPos[0].y);
        float tan60 = Mathf.Tan(Mathf.PI / 3);
        float tan = 0.0f;
        if (x != oringinX)
        {
            tan = Mathf.Abs(y - oringinY) / Mathf.Abs(x - oringinX);
        }
        else
        {
            tan = 1000.0f;
        }

        if (y > oringinY)            //向下方向
        {
            if (tan < tan60)
            {
                if (x > oringinX)
                {
                    dir = TDirection.EDir_DownRight;
                }
                else
                {
                    dir = TDirection.EDir_LeftDown;
                }
            }
            else
            {
                dir = TDirection.EDir_Down;
            }
        }
        else
        {
            if (tan < tan60)
            {
                if (x > oringinX)       //向右移动
                {
                    dir = TDirection.EDir_UpRight;
                }
                else      //向左移动
                {
                    dir = TDirection.EDir_LeftUp;
                }
            }
            else
            {
                dir = TDirection.EDir_Up;
            }
        }

        //选中第二个
        p = GoTo(m_selectedPos[0], dir, 1);

        if (GetBlock(p) == null || !GetBlock(p).SelectAble())
        {
            return;
        }

        m_selectedPos[1] = p;

        ProcessMove();
    }

    void ProcessMove()
    {
        //if (GlobalVars.CurStageData.StepLimit > 0)
        {
            --PlayingStageData.StepLimit;
        }
        
        TSpecialBlock special0 = m_blocks[m_selectedPos[0].x, m_selectedPos[0].y].special;
        TSpecialBlock special1 = m_blocks[m_selectedPos[1].x, m_selectedPos[1].y].special;
        if (special0 == TSpecialBlock.ESpecial_Normal
            && special1 == TSpecialBlock.ESpecial_Normal)
        {
            MoveBlockPair(m_selectedPos[0], m_selectedPos[1]);
        }
        else
        {
            //处理五彩块
            if (special0 == TSpecialBlock.ESpecial_EatAColor)
            {
                if (special1 == TSpecialBlock.ESpecial_Normal)
                {
                    m_blocks[m_selectedPos[0].x, m_selectedPos[0].y].Eat(); //自己消失
                    EatAColor(m_blocks[m_selectedPos[1].x, m_selectedPos[1].y].color);
                }
                if (special1 == TSpecialBlock.ESpecial_EatLineDir0 || special1 == TSpecialBlock.ESpecial_EatLineDir2 ||
                    special1 == TSpecialBlock.ESpecial_EatLineDir1)
                {
                    ChangeColorToLine(m_blocks[m_selectedPos[1].x, m_selectedPos[1].y].color);
                }
                if (special1 == TSpecialBlock.ESpecial_EatAColor)
                {
                    EatAColor(TBlockColor.EColor_None);         //消全部
                }
            }
            else if (special1 == TSpecialBlock.ESpecial_EatAColor)
            {
                if (special0 == TSpecialBlock.ESpecial_Normal)
                {
                    m_blocks[m_selectedPos[1].x, m_selectedPos[1].y].Eat(); //自己消失
                    EatAColor(m_blocks[m_selectedPos[0].x, m_selectedPos[0].y].color);      //消颜色
                }
                if (special0 == TSpecialBlock.ESpecial_EatLineDir0 || special0 == TSpecialBlock.ESpecial_EatLineDir2 ||
                    special0 == TSpecialBlock.ESpecial_EatLineDir1)
                {
                    ChangeColorToLine(m_blocks[m_selectedPos[0].x, m_selectedPos[0].y].color);
                }
            }
            else
            {
                MoveBlockPair(m_selectedPos[0], m_selectedPos[1]);
            }

            //处理条状块
            if (special0 == TSpecialBlock.ESpecial_EatLineDir0 || special0 == TSpecialBlock.ESpecial_EatLineDir1 || special0 == TSpecialBlock.ESpecial_EatLineDir2)
            {
                if (special1 == TSpecialBlock.ESpecial_EatLineDir0 || special1 == TSpecialBlock.ESpecial_EatLineDir1 || special1 == TSpecialBlock.ESpecial_EatLineDir2)
                {

                }
            }
        }
        ProcessTempBlocks();
    }

    void EatAColor(TBlockColor color)
    {
        for (int i = 0; i < BlockCountX; ++i )
        {
            for (int j = 0; j < BlockCountY; ++j )
            {
                if (m_blocks[i, j] == null)
                {
                    continue;
                }
                if (color != TBlockColor.EColor_None && m_blocks[i, j].special == TSpecialBlock.ESpecial_EatAColor)
                {
                    continue;
                }
                if (color == TBlockColor.EColor_None)
                {
                    EatBlock(new Position(i, j));
                }
                else if (m_blocks[i, j].color == color)
                {
                    EatBlock(new Position(i, j));
                }
            }
        }
		timerEatBlock.Play();
    }

    void ChangeColorToLine(TBlockColor color)
    {

    }

    void MoveBlockPair(Position position1, Position position2)
    {
        ExchangeBlock(position1, position2);			//交换方块

        //PlaySound(capsmove);							//移动
        timerMoveBlock.Play();							//开启计时器

        //计算方向
        if (position1.y > position2.y)
        {
            if (position1.x == position2.x)
            {
                m_moveDirection = TDirection.EDir_Up;
            }
            else if (position1.x > position2.x)
            {
                m_moveDirection = TDirection.EDir_DownRight;
            }
            else
            {
                m_moveDirection = TDirection.EDir_UpRight;
            }
        }
        else if (position1.y < position2.y)
        {
            if (position1.x == position2.x)
            {
                m_moveDirection = TDirection.EDir_Down;
            }
            else if (position1.x > position2.x)
            {
                m_moveDirection = TDirection.EDir_LeftDown;
            }
            else
            {
                m_moveDirection = TDirection.EDir_LeftUp;
            }
        }
        else
        {
            if (position1.x > position2.x)
            {
                if (position1.x % 2 == 0)
                {
                    m_moveDirection = TDirection.EDir_DownRight;
                }
                else
                {
                    m_moveDirection = TDirection.EDir_LeftDown;
                }
            }
            else
            {
                if (position1.x % 2 == 0)
                {
                    m_moveDirection = TDirection.EDir_UpRight;
                }
                else
                {
                    m_moveDirection = TDirection.EDir_LeftUp;
                }
            }
        }
        //播放动画
        //PlayAni(position1.x, position1.y,m_moveDirection);
        //PlayAni(position2.x, position2.y,GetOtherDirection(m_moveDirection));

        //SetSelectAni(-1, -1);
    }

    void ExchangeBlock(Position position1, Position position2)
    {
        CapBlock tempBlock = m_blocks[position1.x, position1.y];
        m_blocks[position1.x, position1.y] = m_blocks[position2.x, position2.y];
        m_blocks[position2.x, position2.y] = tempBlock;
    }

    CapBlock GetFreeCapBlock(TBlockColor color)
    {
        CapBlock block = m_capBlockFreeList.Last.Value;
        block.m_blockTransform.gameObject.SetActive(true);
        m_capBlockFreeList.RemoveLast();
        return block;
    }

    int m_idCount = 0;
    int m_lastPlus5Step = 0;            //上次+5的步数
    int m_plus5Count = 0;

    void CreateBlock(int x, int y, bool avoidLine)
    {
        TBlockColor color = GetRandomColor(PlayingStageData.CheckFlag(x, y, GridFlag.Birth));		//最上方获取新的方块
        m_blocks[x, y] = GetFreeCapBlock(color);            //创建新的块 Todo 变成用缓存
        m_blocks[x, y].color = color;               //设置颜色
        m_blocks[x, y].id = m_idCount++;

        if (Timer.millisecondNow() - m_gameStartTime > PlayingStageData.PlusStartTime * 1000)       //若超过了开始掉+5的时间
        {
            //处理+5
            if (PlayingStageData.TimeLimit > 0 && GlobalVars.CurStageData.StepLimit - PlayingStageData.StepLimit > m_lastPlus5Step + PlayingStageData.PlusStep)
            {
                m_blocks[x, y].special = TSpecialBlock.ESpecial_NormalPlus5;            //生成一个+5
                m_lastPlus5Step = GlobalVars.CurStageData.StepLimit - PlayingStageData.StepLimit;
            }
        }

        if (avoidLine)
        {
            while (IsHaveLine(new Position(x, y))) m_blocks[x, y].color = GetNextColor(m_blocks[x, y].color);		//若新生成瓶盖的造成消行，换一个颜色
        }
        m_blocks[x, y].RefreshBlockSprite(PlayingStageData.GridData[x, y]);                                         //刷新下显示内容
    }

    void MakeSpriteFree(int x, int y)
    {
        m_blocks[x, y].m_animation.Stop();
        m_capBlockFreeList.AddLast(m_blocks[x, y]);
        m_blocks[x, y].m_blockTransform.gameObject.SetActive(false);
        m_blocks[x, y].Reset();
        m_blocks[x, y] = null;
    }

    void OnProgressChange()
    {

    }

    void ClearSelected()
    {
        m_selectedPos[0].x = -1;
        m_selectedPos[1].x = -1;
    }

    TBlockColor GetRandomColor(bool bBirth)
    {
        if (bBirth && PlayingStageData.Target == GameTarget.BringFruitDown          //若为出生点，且水果下落模式，且还没掉够坚果
            && m_nut1Count + PlayingStageData.Nut1Count + m_nut2Count + PlayingStageData.Nut2Count < GlobalVars.CurStageData.Nut1Count + GlobalVars.CurStageData.Nut2Count)
        {

            bool AddNut = false;        //记录是否生成坚果的结果

            if (m_nut1Count + m_nut2Count + PlayingStageData.Nut2Count + PlayingStageData.Nut1Count < PlayingStageData.NutInitCount)     //若还没到初始数量
            {
                AddNut = true;          //生成
            }

            if (PlayingStageData.NutMaxCount == 0 || m_nut1Count + m_nut2Count < PlayingStageData.NutMaxCount)       //画面上坚果数量已经小于最大数量，不生成
            {
                if (GlobalVars.CurStageData.StepLimit - PlayingStageData.StepLimit > 
                    (m_nut1Count + m_nut2Count + PlayingStageData.Nut1Count + PlayingStageData.Nut2Count - PlayingStageData.NutInitCount + 1) * PlayingStageData.NutStep)      //若已经到步数了
                {
                    AddNut = true;
                }
            }

            if (AddNut)
            {
                TBlockColor nutColor;
                if (m_nut1Count + PlayingStageData.Nut1Count >= GlobalVars.CurStageData.Nut1Count)      //若已掉 够了，就不再掉
                {
                    nutColor = TBlockColor.EColor_Nut2;
                }

                else if (m_nut2Count + PlayingStageData.Nut2Count >= GlobalVars.CurStageData.Nut2Count)      //若已掉 够了，就不再掉
                {
                    nutColor = TBlockColor.EColor_Nut1;
                }
                else
                {
                    nutColor = TBlockColor.EColor_Nut1 + m_random.Next() % 2;
                }

                if (nutColor == TBlockColor.EColor_Nut1)
                {
                    ++m_nut1Count;
                }
                else
                {
                    ++m_nut2Count;
                }
                return nutColor;
            }
        }

        return TBlockColor.EColor_White + m_random.Next() % PlayingStageData.ColorCount;
    }

    TBlockColor GetNextColor(TBlockColor color)
    {
        int index = color - TBlockColor.EColor_White;
        return (TBlockColor)((index + 1) % PlayingStageData.ColorCount + TBlockColor.EColor_White);
    }

    bool IsHaveLine(Position position)
    {
	    int countInSameLine = 1;					//在同一条线上相同的颜色的数量
	    int totalSameCount = 1;						//总共相同颜色的数量
	    int step = 1;
	    TBlockColor color = GetBlockColor(position);
	    Position curPos;
        for (TDirection dir = TDirection.EDir_Up; dir <= TDirection.EDir_DownRight; dir = (TDirection)(dir + 1))		//遍历3个方向
	    {
		    countInSameLine = 1;
		    curPos = position;
		    while (true)			
		    {
			    curPos = GoTo(curPos, dir, 1);									//沿着Dir方向走一步
			    if (GetBlockColor(curPos) != color)								//若碰到不一样的颜色就停下来
			    {
				    break;
			    }
			    ++countInSameLine;
			    if (countInSameLine >= 3)
			    {
				    return true;
			    }
		    }
		    curPos = position;														//重置位置
		    while (true)			
		    {
			    curPos = GoTo(curPos, GetOtherDirection(dir), 1);					//沿着Dir反方向走
			    if (GetBlockColor(curPos) != color)								//若碰到不一样的颜色就停下来
			    {
				    break;
			    }
			    ++countInSameLine;
			    if (countInSameLine >= 3)
			    {
				    return true;
			    }
		    }
	    }
	    return false;
    }

    TDirection GetOtherDirection(TDirection dir)					//从一个方向获得相反的方向
	{
		return (TDirection)((int)(dir + 3) % 6);
	}

	CapBlock GetBlock(Position p)							//获得某位置的块对象
	{
		return m_blocks[p.x, p.y];
	}

	bool CheckPosAvailable(Position p)							//获得某位置是否可用
	{
		if (p.x < 0 || p.x >=BlockCountX || p.y < 0 || p.y >= BlockCountY)
		{
			return false;
		}
		return true;
	}

	TBlockColor GetBlockColor(Position p)						//获得位置的颜色
	{
        if (!CheckPosAvailable(p) || m_blocks[p.x, p.y] == null)
		{
            return TBlockColor.EColor_None;
		}
        if (m_blocks[p.x, p.y].special == TSpecialBlock.ESpecial_EatAColor)         //Todo EatAColor是否变成一个Color而不是一个Specail?
        {
            return TBlockColor.EColor_None;
        }
		return m_blocks[p.x, p.y].color;
	}

    Position GoTo(Position pos,TDirection direction,int step)
    {
        if (step == 0) return pos;
        int i = pos.x;
        int j = pos.y;
        Position p = new Position();
        if ((i + 10) % 2 == 0)
        {
            switch (direction)
            {
                case TDirection.EDir_Up:
                    {
                        j--;
                    }
                    break;
                case TDirection.EDir_UpRight:
                    {
                        i++;
                    }
                    break;
                case TDirection.EDir_DownRight:
                    {
                        i++;
                        j++;
                    }
                    break;
                case TDirection.EDir_Down:
                    {
                        j++;
                    }
                    break;
                case TDirection.EDir_LeftDown:
                    {
                        i--;
                        j++;
                    }
                    break;
                case TDirection.EDir_LeftUp:
                    {
                        i--;
                    }
                    break;
                default:
                    break;
            }
        }
        else
        {
            switch (direction)
            {
                case TDirection.EDir_Up:
                    {
                        j--;
                    }
                    break;
                case TDirection.EDir_UpRight:
                    {
                        i++;
                        j--;
                    }
                    break;
                case TDirection.EDir_DownRight:
                    {
                        i++;
                    }
                    break;
                case TDirection.EDir_Down:
                    {
                        j++;
                    }
                    break;
                case TDirection.EDir_LeftDown:
                    {
                        i--;
                    }
                    break;
                case TDirection.EDir_LeftUp:
                    {
                        j--;
                        i--;
                    }
                    break;
                default:
                    break;
            }
        }
        p.x = i;
        p.y = j;

        if (step - 1 > 0)
        {
            return GoTo(p, direction, step - 1);
        }
        else
        {
            return p;
        }
    }

    bool CheckTwoPosLinked(Position position1, Position position2)
{
    if (System.Math.Abs(position2.x - position1.x) + System.Math.Abs(position2.y - position1.y) == 1
        || (position1.x % 2 == 1 && position1.y - position2.y == 1 && System.Math.Abs(position2.x - position1.x) == 1)
        || (position1.x % 2 == 0 && position2.y - position1.y == 1 && System.Math.Abs(position2.x - position1.x) == 1))
		return true;
	return false;
}

}
