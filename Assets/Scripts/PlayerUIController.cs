using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;
using Random = System.Random;

public class PlayerUIController : MonoBehaviour
{
    
    public VisualElement HealthBar;
    public VisualElement ArmorBar;

    public float HealthBarLength;
    public float ArmorBarLength;

    public Random test = new Random();
    
    void Start()
    {

        HealthBar = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("health");
        ArmorBar = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("armor");

        HealthBarLength = 250;
        ArmorBarLength = 250;
        
        HealthBar.style.width = new Length(100, LengthUnit.Percent);
        ArmorBar.style.width = new Length(100, LengthUnit.Percent);
    }
    
    void Update()
    {
        
        Thread.Sleep(1000);
        
        var damage1 = test.Next(0, (int) HealthBarLength);
        var damage2 = test.Next(0, (int) ArmorBarLength);


        HealthBar.style.width = new Length(HealthBarLength - damage1, LengthUnit.Pixel);
        ArmorBar.style.width = new Length((HealthBarLength - damage2), LengthUnit.Pixel);
        
        Debug.Log("Changing UI : " + damage1 + " and " + damage2);
    }
}
