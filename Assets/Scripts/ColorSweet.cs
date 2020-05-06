﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorSweet : MonoBehaviour {
    
    //游戏方块种类的枚举
    public enum ColorType
    {
        PURPLE,
        RED,
        YELLOW,
        GREEN,
        BLUE,
        PINK,
        ANY,
        COUNT
    }

    [System.Serializable]
    public struct ColorSprite
    {
        public ColorType color;
        public Sprite sprite;
    }

    public ColorSprite[] ColorSprites;

    private Dictionary<ColorType, Sprite> colorSpriteDict;

    private SpriteRenderer sprite;

    private ColorType color;
    public ColorType Color
    {
        get
        {
            return color;
        }

        set
        {
            SetColor(value);
        }
    }

    public int NumColors
    {
        get
        {
            return ColorSprites.Length;
        }
    }

    private void Awake()
    {
        sprite = transform.Find("Sweet").GetComponent<SpriteRenderer>();
        colorSpriteDict = new Dictionary<ColorType, Sprite>();
        for(int i = 0; i < ColorSprites.Length; i++)
        {
            colorSpriteDict.Add(ColorSprites[i].color, ColorSprites[i].sprite);
        }
    }

    public void SetColor(ColorType newColor)
    {
        color = newColor;
        if (colorSpriteDict.ContainsKey(newColor))
        {
            sprite.sprite = colorSpriteDict[newColor];
        }
    }
}
