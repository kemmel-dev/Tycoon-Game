using UnityEngine;

[CreateAssetMenu(fileName = "Resource", menuName = "ScriptableObjects/Resource", order = 1)]
public class ResourceUI : ScriptableObject
{
    public ResourceType Type;
    public Sprite Sprite;
    public string Description;
}