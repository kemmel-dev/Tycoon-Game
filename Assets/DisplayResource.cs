using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DisplayResource : MonoBehaviour
{
    [SerializeField] private ResourceUI resourceUI;
    [SerializeField] private Image resourceSprite;
    [SerializeField] private TMP_Text resourceDescription;

    private void Start()
    {
        resourceSprite.sprite = resourceUI.Sprite;
        resourceDescription.text = resourceUI.Description;

        StartCoroutine(UpdateValues());
    }

    public void UpdateResourceAmount()
    {
        // TODO: Update resource values from a manager
    }
    
    /// <summary>
    /// Custom update loop.
    /// </summary>
    /// <returns></returns>
    IEnumerator UpdateValues()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.25f);
            
            UpdateResourceAmount();
        }
    }
}
