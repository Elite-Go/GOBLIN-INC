using System;
using UnityEngine;

public class MaterialSwitcher : Singleton<MaterialSwitcher>
{
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material onMouseMaterial;
    [SerializeField] private Material selectedMaterial;

    private void Update()
    {
    }
    
    public void OnMouse(Renderer rd, bool isOrigin)
    {
        rd.material = isOrigin ? defaultMaterial : onMouseMaterial;
    }
    
    public void Change(Renderer rd, bool isOrigin)
    {
        rd.material = isOrigin ? defaultMaterial : selectedMaterial;
    }
}