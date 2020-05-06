using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    //定义游戏方块的种类
    public enum SweetsType
    {
        EMPTY,
        NORMAL,
        BARRIER,
        ROW_CLEAR,
        COLUMN_CLEAR,
        RAINBOWCANDY,//彩虹糖
        COUNT//标记类型
    }

    //定义游戏方块字典
    public Dictionary<SweetsType, GameObject> sweetPrefabDict;

    [System.Serializable]//添加特性序列化，可以让结构体的数组显示在unity监视面板里
    public struct SweetPrefab
    {
        public SweetsType type;
        public GameObject prefab;
    }

    public SweetPrefab[] sweetPrefabs;

    //游戏方块数组 二维数组
    private GameSweet[,] sweets;

    //单例实例化
    private static GameManager _instance;   
    public static GameManager Instance
    {
        get
        {
            return _instance;
        }

        set
        {
            _instance = value;
        }
    }

    public int xColumn;//列
    public int yRow;//行

    public float fillTime;//填充时间

    public GameObject gridPrefabs;

    public GameSweet pressedSweet;//第一个
    public GameSweet enteredSweet;//第二个

    //UI&rule
    public Text timeText;
    private float gameTime = 10;
    private bool gameOver;
    public int playerScore;
    public Text playerScoreText;
    public Text fScoreText;
    public GameObject gameOverPanel;

    private void Awake()
    {

        _instance = this;
    }

    // Use this for initialization
    void Start()
    {
        //字典实例化
        sweetPrefabDict = new Dictionary<SweetsType, GameObject>();
        for (int i = 0; i < sweetPrefabs.Length; i++)
        {
            if (!sweetPrefabDict.ContainsKey(sweetPrefabs[i].type))
            {
                sweetPrefabDict.Add(sweetPrefabs[i].type, sweetPrefabs[i].prefab);
            }

        }

        for (int x = 0; x < xColumn; x++)
        {
            for (int y = 0; y < yRow; y++)
            {
                GameObject chocolate = Instantiate(gridPrefabs, CorrectPostion(x, y), Quaternion.identity);
                chocolate.transform.SetParent(transform);
            }
        }

        sweets = new GameSweet[xColumn, yRow];
        for (int x = 0; x < xColumn; x++)
        {
            for (int y = 0; y < yRow; y++)
            {
                //GameObject newSweet = Instantiate(sweetPrefabDict[SweetsType.NORMAL], CorrectPostion2(x, y), Quaternion.identity);
                //newSweet.transform.SetParent(transform);

                //sweets[x, y] = newSweet.GetComponent<GameSweet>();
                //sweets[x, y].Init(x, y, this, SweetsType.NORMAL);

                //if (sweets[x, y].CanMove())
                //{
                //    sweets[x, y].MovedComponent.Move(x, y);
                //}

                //if (sweets[x, y].CanColor())
                //{
                //    sweets[x, y].ColoredComponent.SetColor((ColorSweet.ColorType)Random.Range(0, sweets[x, y].ColoredComponent.NumColors));
                //}

                CreateNewSweet(x, y, SweetsType.EMPTY);

            }
        }

        //障碍物方块初始化
        for(int i = 1; i < 10; i++)
        {
                Destroy(sweets[i, i-1].gameObject);
                CreateNewSweet(i, i-1, SweetsType.BARRIER);
        }

        StartCoroutine(AllFill());
    }

    // Update is called once per frame
    void Update()
    {
        if (gameOver)
        {
            return;
        }
        gameTime -= Time.deltaTime;
        if (gameTime <= 0)
        {
            gameTime = 0;
            gameOver = true;
            fScoreText.text = playerScore.ToString();
            gameOverPanel.SetActive(true);
            return;
        }
        timeText.text = gameTime.ToString("0");

        //score
        playerScoreText.text = playerScore.ToString();
    }

    //位置纠正
    public Vector3 CorrectPostion(int x, int y)
    {
        return new Vector3(transform.position.x - xColumn / 2f + x, transform.position.y + yRow / 2f - y);
    }
    //public Vector3 CorrectPostion2(int x, int y)
    //{
    //    return new Vector3(transform.position.x - xColumn / 2f + x - 0.19f, transform.position.y + yRow / 2f - y + 0.12f);
    //}

    //生成游戏方块的方法
    public GameSweet CreateNewSweet(int x, int y, SweetsType type)
    {
        GameObject newSweet = Instantiate(sweetPrefabDict[type], CorrectPostion(x, y), Quaternion.identity);
        newSweet.transform.parent = transform;
        sweets[x, y] = newSweet.GetComponent<GameSweet>();
        sweets[x, y].Init(x, y, this, type);

        return sweets[x, y];
    }

    //全部填充的方法
    public IEnumerator AllFill()
    {
        bool needRefill = true;
        while (needRefill)
        {
            yield return new WaitForSeconds(fillTime);
            while (Fill())
            {
                yield return new WaitForSeconds(fillTime);
            }

            //开局清除
            needRefill = ClearAllMatchedSweet();
        }
    }

    //分步填充的方法
    public bool Fill()
    {
        bool filledNotFinished = false;//判断本次填充是否完成

        for (int y = yRow - 2; y >= 0; y--)
        {
            for (int x = 0; x < xColumn; x++)
            {
                GameSweet sweet = sweets[x, y];//得到当前位置的方块对象

                if (sweet.CanMove())//NO，则无法往下填充 
                {
                    GameSweet sweetBelow = sweets[x, y + 1];

                    if (sweetBelow.Type == SweetsType.EMPTY)//空方块，垂直填充
                    {
                        Destroy(sweetBelow.gameObject);
                        sweet.MovedComponent.Move(x, y + 1, fillTime);
                        sweets[x, y + 1] = sweet;
                        CreateNewSweet(x, y, SweetsType.EMPTY);
                        filledNotFinished = true;
                    }
                    else //斜向填充
                    {
                        for (int down = -1; down <= 1; down++)//down -1左0中1右
                        {
                            if (down != 0)
                            {
                                int downX = x + down;

                                if (downX >= 0 && downX < xColumn)
                                {
                                    GameSweet downSweet = sweets[downX, y + 1];

                                    if (downSweet.Type == SweetsType.EMPTY)
                                    {
                                        bool canfill = true;

                                        for (int aboveY = y; aboveY >= 0; aboveY--)
                                        {
                                            GameSweet sweetAbove = sweets[downX, aboveY];
                                            if (sweetAbove.CanMove())
                                            {
                                                break;
                                            }
                                            else if (!sweetAbove.CanMove() && sweetAbove.Type != SweetsType.EMPTY)
                                            {
                                                canfill = false;
                                                break;
                                            }
                                        }
                                        if (!canfill)
                                        {
                                            Destroy(downSweet.gameObject);
                                            sweet.MovedComponent.Move(downX, y + 1, fillTime);
                                            sweets[downX, y + 1] = sweet;
                                            CreateNewSweet(x, y, SweetsType.EMPTY);
                                            filledNotFinished = true;
                                            break;
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
            }
        }
        //-1行
        for (int x = 0; x < xColumn; x++)
        {
            
            GameSweet sweet = sweets[x, 0];
            if (sweet.Type == SweetsType.EMPTY)
            {
                GameObject newSweet = Instantiate(sweetPrefabDict[SweetsType.NORMAL], CorrectPostion(x, -1), Quaternion.identity);
                newSweet.transform.parent = transform;

                sweets[x, 0] = newSweet.GetComponent<GameSweet>();
                sweets[x, 0].Init(x, -1, this, SweetsType.NORMAL);
                sweets[x, 0].MovedComponent.Move(x, 0, fillTime);
                sweets[x, 0].ColoredComponent.SetColor((ColorSweet.ColorType)Random.Range(0, sweets[x, 0].ColoredComponent.NumColors));
                filledNotFinished = true;
            }
        }
        return filledNotFinished;
    }

    //游戏方块交换
    public bool Adjoin(GameSweet sweet1,GameSweet sweet2)
    {
        return (sweet1.X == sweet2.X && Mathf.Abs(sweet1.Y - sweet2.Y) == 1) || 
            (sweet1.Y == sweet2.Y && Mathf.Abs(sweet1.X - sweet2.X) == 1);
    }
    private void ExchangeSweets(GameSweet sweet1, GameSweet sweet2)
    {
        if (sweet1.CanMove() && sweet2.CanMove())
        {
            sweets[sweet1.X, sweet1.Y] = sweet2;
            sweets[sweet2.X, sweet2.Y] = sweet1;

            if (MatchSweets(sweet1,sweet2.X,sweet2.Y)!=null || MatchSweets(sweet2, sweet1.X, sweet1.Y) != null || sweet1.Type == SweetsType.RAINBOWCANDY || sweet2.Type == SweetsType.RAINBOWCANDY)
            {
                int tempX = sweet1.X;
                int tempY = sweet1.Y;

                sweet1.MovedComponent.Move(sweet2.X, sweet2.Y, fillTime);
                sweet2.MovedComponent.Move(tempX, tempY, fillTime);

                if (sweet1.Type == SweetsType.RAINBOWCANDY && sweet1.CanClear() && sweet2.CanClear())
                {
                    ClearColorSweet clearColor = sweet1.GetComponent<ClearColorSweet>();

                    if (clearColor != null)
                    {
                        clearColor.ClearColor = sweet2.ColoredComponent.Color;
                    }

                    ClearSweet(sweet1.X, sweet1.Y);
                }
                if (sweet2.Type == SweetsType.RAINBOWCANDY && sweet2.CanClear() && sweet1.CanClear())
                {
                    ClearColorSweet clearColor = sweet2.GetComponent<ClearColorSweet>();

                    if (clearColor != null)
                    {
                        clearColor.ClearColor = sweet1.ColoredComponent.Color;
                    }

                    ClearSweet(sweet2.X, sweet2.Y);
                }

                ClearAllMatchedSweet();
                //全部填充协程
                StartCoroutine(AllFill());
                pressedSweet = null;
                enteredSweet = null;
            }
            else
            {
                sweets[sweet1.X, sweet1.Y] = sweet1;
                sweets[sweet2.X, sweet2.Y] = sweet2;
            }
        }
    }
   

    //匹配方法
    public List<GameSweet> MatchSweets(GameSweet sweet, int newX, int newY)
    {
        if (sweet.CanColor())
        {
            ColorSweet.ColorType color = sweet.ColoredComponent.Color;
            List<GameSweet> matchRowSweets = new List<GameSweet>();     //行
            List<GameSweet> matchLineSweets = new List<GameSweet>();    //列
            List<GameSweet> finishedMatchingSweets = new List<GameSweet>();//完成匹配列表

            //行
            matchRowSweets.Add(sweet);

            //0 左  1 右
            for (int i = 0; i <= 1; i++)
            {
                for (int xDistance = 1; xDistance < xColumn; xDistance++)
                {
                    int x;
                    if (i == 0)
                    {
                        x = newX - xDistance;
                    }
                    else
                    {
                        x = newX + xDistance;
                    }
                    if (x < 0 || x >= xColumn)
                    {
                        break;
                    }
                    if (sweets[x, newY].CanColor() && sweets[x, newY].ColoredComponent.Color == color)//添加进列表
                    {
                        matchRowSweets.Add(sweets[x, newY]);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (matchRowSweets.Count >= 3)//当前列表中的元素>=3
            {
                for (int i = 0; i < matchRowSweets.Count; i++)
                {
                    finishedMatchingSweets.Add(matchRowSweets[i]);
                }
            }

            //L T
            if (matchRowSweets.Count >= 3)
            {
                for (int i = 0; i < matchRowSweets.Count; i++)
                {
                    //行匹配列表中满足匹配条件的每个元素上下依次进行列遍历
                    // 0 上   1 下
                    for (int j = 0; j <= 1; j++)
                    {
                        for (int yDistance = 1; yDistance < yRow; yDistance++)  //yDistance = 0是其本身 不需要遍历
                        {
                            int y;
                            if (j == 0)
                            {
                                y = newY - yDistance;
                            }
                            else
                            {
                                y = newY + yDistance;
                            }
                            if (y < 0 || y >= yRow)
                            {
                                break;
                            }

                            if (sweets[matchRowSweets[i].X, y].CanColor() && sweets[matchRowSweets[i].X, y].ColoredComponent.Color == color)
                            {
                                matchLineSweets.Add(sweets[matchRowSweets[i].X, y]);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (matchLineSweets.Count < 2)
                    {
                        matchLineSweets.Clear();
                    }
                    else
                    {
                        for (int j = 0; j < matchLineSweets.Count; j++)
                        {
                            finishedMatchingSweets.Add(matchLineSweets[j]);
                        }
                        break;
                    }
                }
            }

            if (finishedMatchingSweets.Count >= 3)
            {
                return finishedMatchingSweets;
            }

            matchRowSweets.Clear();
            matchLineSweets.Clear();

            //列
            matchLineSweets.Add(sweet);
            //i 左   i 右
            for (int i = 0; i <= 1; i++)
            {
                for (int yDistance = 1; yDistance < yRow; yDistance++)
                {
                    int y;
                    if (i == 0)
                    {
                        y = newY - yDistance;
                    }
                    else
                    {
                        y = newY + yDistance;
                    }
                    if (y < 0 || y >= yRow)
                    {
                        break;
                    }

                    if (sweets[newX, y].CanColor() && sweets[newX, y].ColoredComponent.Color == color)
                    {
                        matchLineSweets.Add(sweets[newX, y]);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (matchLineSweets.Count >= 3)
            {
                for (int i = 0; i < matchLineSweets.Count; i++)
                {
                    finishedMatchingSweets.Add(matchLineSweets[i]);
                }
            }

            //L T
            if (matchLineSweets.Count >= 3)
            {
                for (int i = 0; i < matchLineSweets.Count; i++)
                {
                    // 0 上  1 下
                    for (int j = 0; j <= 1; j++)
                    {
                        for (int xDistance = 1; xDistance < xColumn; xDistance++)
                        {
                            int x;
                            if (j == 0)
                            {
                                x = newY - xDistance;
                            }
                            else
                            {
                                x = newY + xDistance;
                            }
                            if (x < 0 || x >= xColumn)
                            {
                                break;
                            }

                            if (sweets[x, matchLineSweets[i].Y].CanColor() && sweets[x, matchLineSweets[i].Y].ColoredComponent.Color == color)
                            {
                                matchRowSweets.Add(sweets[x, matchLineSweets[i].Y]);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (matchRowSweets.Count < 2)
                    {
                        matchRowSweets.Clear();
                    }
                    else
                    {
                        for (int j = 0; j < matchRowSweets.Count; j++)
                        {
                            finishedMatchingSweets.Add(matchRowSweets[j]);
                        }
                        break;
                    }
                }
            }

            if (finishedMatchingSweets.Count >= 3)
            {
                return finishedMatchingSweets;
            }
        }

        return null;
    }

    //清除
    public bool ClearSweet(int x,int y)//游戏方块
    {
        if (sweets[x, y].CanClear() && !sweets[x, y].ClearedComponent.IsClearing)
        {
            sweets[x, y].ClearedComponent.Clear();
            CreateNewSweet(x, y, SweetsType.EMPTY);
            ClearBarrier(x, y);
            return true;
        }
        return false;
    }
    private void ClearBarrier(int x, int y)//障碍物
    {
        for (int friendX = x - 1; friendX <= x + 1; friendX++)
        {
            if (friendX != x && friendX >= 0 && friendX < xColumn)
            {
                if (sweets[friendX, y].Type == SweetsType.BARRIER && sweets[friendX, y].CanClear())
                {
                    sweets[friendX, y].ClearedComponent.Clear();
                    CreateNewSweet(friendX, y, SweetsType.EMPTY);
                }
            }
        }
        for (int friendY = y - 1; friendY <= y + 1; friendY++)
        {
            if (friendY != y && friendY >= 0 && friendY < yRow)
            {
                if (sweets[x, friendY].Type == SweetsType.BARRIER && sweets[x, friendY].CanClear())
                {
                    sweets[x, friendY].ClearedComponent.Clear();
                    CreateNewSweet(x, friendY, SweetsType.EMPTY);
                }
            }
        }
    }

    private bool ClearAllMatchedSweet()
    {
        bool needRefill = false;
        for (int y = 0; y < yRow; y++)
        {
            for (int x = 0; x < xColumn; x++) {
                if (sweets[x, y].CanClear())
                {
                    List<GameSweet> matchList = MatchSweets(sweets[x, y], x, y);

                    if (matchList != null)
                    {
                        //行、列、同色消除方块相关
                        SweetsType specialSweetsType = SweetsType.COUNT;
                        GameSweet randomSweet = matchList[Random.Range(0, matchList.Count)];//随机位置
                        int specialSweetX = randomSweet.X;
                        int specialSweetY = randomSweet.Y;
                        if (matchList.Count == 4)//行列
                        {
                            specialSweetsType = (SweetsType)Random.Range((int)SweetsType.ROW_CLEAR, (int)SweetsType.COLUMN_CLEAR);
                        }
                        else if (matchList.Count >= 5)//同色
                        {
                            specialSweetsType = SweetsType.RAINBOWCANDY;
                        }

                        for (int i = 0; i < matchList.Count; i++)
                        {
                            if (ClearSweet(matchList[i].X, matchList[i].Y))
                            {
                                needRefill = true;
                            }
                        }

                        if (specialSweetsType != SweetsType.COUNT)
                        {
                            Destroy(sweets[specialSweetX, specialSweetY]);
                            GameSweet newSweet = CreateNewSweet(specialSweetX, specialSweetY, specialSweetsType);
                            if (specialSweetsType == SweetsType.ROW_CLEAR || specialSweetsType == SweetsType.COLUMN_CLEAR && newSweet.CanColor() && matchList[0].CanColor())
                            {//满足条件并通过安全校验
                                newSweet.ColoredComponent.SetColor(matchList[0].ColoredComponent.Color);
                            }
                            else if (specialSweetsType == SweetsType.RAINBOWCANDY && newSweet.CanColor())
                            {
                                newSweet.ColoredComponent.SetColor(ColorSweet.ColorType.ANY);
                            }

                        }
                    }
                }
            }
        }
        return needRefill;
    }

    //返回主界面和重玩
    public void ReturnToMain()
    {
        SceneManager.LoadScene(0);
    }
    public void Restart()
    {
        SceneManager.LoadScene(1);
    }

    //特殊方块的方法
    public void ClearRow(int row)//行
    {
        for (int x = 0; x < xColumn; x++)
        {
            ClearSweet(x, row);
        }
    }
    public void ClearColumn(int column)//列
    {
        for (int y = 0; y < yRow; y++)
        {
            ClearSweet(column, y);
        }
    }
    public void ClearColor(ColorSweet.ColorType color)//同色
    {
        for (int x = 0; x < xColumn; x++)
        {
            for (int y = 0; y < yRow; y++)
            {
                if (sweets[x, y].CanColor() && (sweets[x, y].ColoredComponent.Color == color || color == ColorSweet.ColorType.ANY))
                {
                    ClearSweet(x, y);
                }
            }
        }
    }

    //操作
    public void PressSweet(GameSweet sweet)
    {
        if (gameOver)
        {
            return;
        }
        pressedSweet = sweet;
    }
    public void EnterSweet(GameSweet sweet)
    {
        if (gameOver)
        {
            return;
        }
        enteredSweet = sweet;
    }
    //public void ReleaseSweet()
    //{
    //    if (gameOver)
    //    {
    //        return;
    //    }
    //    if (Adjoin(pressedSweet, enteredSweet))
    //    {
    //        ExchangeSweets(pressedSweet, enteredSweet);
    //    }
    //}
    public void ReleaseSweet()
    {
        if (gameOver)
        {
            return;
        }
        if (Adjoin(pressedSweet, enteredSweet))
        {
            ExchangeSweets(pressedSweet, enteredSweet);
        }
        else if (pressedSweet.X == enteredSweet.X)
        {
            if (pressedSweet.Y > enteredSweet.Y)
            {
                enteredSweet = sweets[pressedSweet.X, pressedSweet.Y - 1];
                ExchangeSweets(pressedSweet, enteredSweet);
            }
            if (pressedSweet.Y < enteredSweet.Y)
            {
                enteredSweet = sweets[pressedSweet.X, pressedSweet.Y + 1];
                ExchangeSweets(pressedSweet, enteredSweet);
            }
        }
        else if (pressedSweet.Y == enteredSweet.Y)
        {
            if (pressedSweet.X > enteredSweet.X)
            {
                enteredSweet = sweets[pressedSweet.X - 1, pressedSweet.Y];
                ExchangeSweets(pressedSweet, enteredSweet);
            }
            if (pressedSweet.X < enteredSweet.X)
            {
                enteredSweet = sweets[pressedSweet.X + 1, pressedSweet.Y]; ;
                ExchangeSweets(pressedSweet, enteredSweet);
            }
        }

    }
}
