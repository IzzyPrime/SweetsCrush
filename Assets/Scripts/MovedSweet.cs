using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovedSweet : MonoBehaviour {

    private GameSweet sweet;

    private IEnumerator moveCoroutine;

    private void Awake()
    {
        sweet = GetComponent<GameSweet>();
    }

    public void Move(int newX, int newY,float time)//游戏方块的移动方法 开启或结束一个协程
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = MoveCoroutine(newX, newY, time);
        StartCoroutine(moveCoroutine);
    }

    private IEnumerator MoveCoroutine(int newX, int newY,float time)
    {
        sweet.X = newX;
        sweet.Y = newY;

        Vector3 startPos = transform.position;
        Vector3 endPos = sweet.gameManager.CorrectPostion(newX, newY);

        for(float t = 0; t < time; t += Time.deltaTime)
        {
            sweet.transform.position = Vector3.Lerp(startPos, endPos, t / time);
            yield return 0;
        }

        sweet.transform.position = endPos;
    }

}
