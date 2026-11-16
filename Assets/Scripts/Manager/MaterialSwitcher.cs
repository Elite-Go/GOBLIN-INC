using System;
using UnityEngine;

[Serializable]
public class MaterialSwitcher
{
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material selectedMaterial;

    public void Change(Renderer rd, bool isOrigin)
    {
        rd.material = isOrigin ? defaultMaterial : selectedMaterial;
    }
}