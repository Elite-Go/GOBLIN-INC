using System.Collections.Generic;
using UnityEngine;

public class OutlineSwitcher : Singleton<OutlineSwitcher>
{
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private string texturePropertyName = "_MainTex";

    private readonly Dictionary<Renderer, Material> _originalMaterials = new Dictionary<Renderer, Material>();
    private readonly Dictionary<Renderer, MaterialPropertyBlock> _propertyBlocks = new Dictionary<Renderer, MaterialPropertyBlock>();
    private int _texturePropertyId;

    protected override void Awake()
    {
        base.Awake();
        _texturePropertyId = Shader.PropertyToID(texturePropertyName);
    }

    public void SetOutline(Renderer renderer, bool active)
    {
        if (renderer == null || outlineMaterial == null) return;

        if (active)
        {
            if (!_originalMaterials.TryGetValue(renderer, out var original))
            {
                original = renderer.sharedMaterial;
                _originalMaterials[renderer] = original;
            }

            if (renderer.sharedMaterial != outlineMaterial)
            {
                renderer.sharedMaterial = outlineMaterial;
            }

            if (!_propertyBlocks.TryGetValue(renderer, out var block))
            {
                block = new MaterialPropertyBlock();
                _propertyBlocks[renderer] = block;
            }

            renderer.GetPropertyBlock(block);

            Texture sourceTex = GetRendererTexture(renderer, original);
            if (sourceTex != null && outlineMaterial.HasProperty(_texturePropertyId))
            {
                block.SetTexture(_texturePropertyId, sourceTex);
            }

            renderer.SetPropertyBlock(block);
        }
        else
        {
            if (_propertyBlocks.TryGetValue(renderer, out var block))
            {
                block.Clear();
                renderer.SetPropertyBlock(null);
                _propertyBlocks.Remove(renderer);
            }

            if (_originalMaterials.TryGetValue(renderer, out var original))
            {
                if (renderer.sharedMaterial != original)
                {
                    renderer.sharedMaterial = original;
                }

                _originalMaterials.Remove(renderer);
            }
        }
    }

    private Texture GetRendererTexture(Renderer renderer, Material originalMaterial)
    {
        if (renderer is SpriteRenderer spriteRenderer && spriteRenderer.sprite != null)
        {
            return spriteRenderer.sprite.texture;
        }

        if (originalMaterial != null)
        {
            if (originalMaterial.HasProperty(_texturePropertyId))
            {
                return originalMaterial.GetTexture(_texturePropertyId);
            }

            if (originalMaterial.mainTexture != null)
            {
                return originalMaterial.mainTexture;
            }
        }

        return null;
    }

    public void ClearAll()
    {
        foreach (var pair in _propertyBlocks)
        {
            if (pair.Key != null)
            {
                pair.Key.SetPropertyBlock(null);
            }
        }
        _propertyBlocks.Clear();

        foreach (var pair in _originalMaterials)
        {
            if (pair.Key != null && pair.Key.sharedMaterial != pair.Value)
            {
                pair.Key.sharedMaterial = pair.Value;
            }
        }
        _originalMaterials.Clear();
    }

    private void OnDestroy()
    {
        ClearAll();
    }
}