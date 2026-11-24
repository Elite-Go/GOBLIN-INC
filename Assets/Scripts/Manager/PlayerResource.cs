using System;
using TMPro;
using UnityEngine;

public class PlayerResource : Singleton<PlayerResource>
{
    public Res wood;

    private void Start()
    {
        wood.ApplyText();
    }
}

[Serializable]
public class Res
{
    public int amount;
    public TextMeshProUGUI amountText;
    
    public void Get(int amt)
    {
        amount += amt;
        ApplyText();
    }

    public void ApplyText()
    {
        amountText.text = amount.ToString();
    }
}