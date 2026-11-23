using UnityEngine;

// data for nearby interactions, dispatched by the existing puzzle controller
public sealed class DreamInteraction : MonoBehaviour
{
    public enum Kind { Keeper, Petal, Astral, Inscription }
    public Kind kind;
    public int index;
    public string title;
    [TextArea(2, 6)] public string inscription;
    public Renderer indicator;
    private MaterialPropertyBlock propertyBlock;

    public Vector3 Focus => transform.position + transform.up * 1.1f;

    public void Refresh(bool bloom)
    {
        if (indicator == null) return;
        bool lit = kind switch
        {
            Kind.Keeper => DreamProgression.KeeperRemembered,
            Kind.Petal => DreamProgression.PetalSteps > index,
            Kind.Astral => DreamProgression.AstralLit(index),
            _ => false
        };
        bool available = kind != Kind.Petal || bloom == (index == 1);
        Color color = lit ? new Color(1f, .76f, .3f) :
            available ? new Color(.46f, .65f, .8f) : new Color(.22f, .18f, .3f);
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        indicator.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_Color", color);
        propertyBlock.SetColor("_EmissionColor", color * (lit ? 2f : .12f));
        indicator.SetPropertyBlock(propertyBlock);
    }
}
