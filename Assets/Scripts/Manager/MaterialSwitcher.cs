using System;
using UnityEngine;

public class MaterialSwitcher : Singleton<MaterialSwitcher>
{
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material selectedMaterial;

    public void Change(Renderer rd, bool isOrigin)
    {
        rd.material = isOrigin ? defaultMaterial : selectedMaterial;
    }
}