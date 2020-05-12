using UnityEngine;

public class Example01 : MonoBehaviour
{
    public float duration = 0.5f;
    public float distance = 2f;
    public float expand = 2f;
    public float rotation = 90;
    public Ease ease = Ease.InOutQuad;
    public Color firstColor = Color.yellow;
    public Color secondColor = Color.red;
    public Color thirdColor = Color.green;
    public Color fourthColor = Color.blue;
    
    private void Start()
    {
        this.loop()
            .translate(transform, duration, ease, x: distance)
            .rotate(transform, rotation, duration, ease)
            .expand(transform, duration, ease, x: expand, y: expand)
            .color(transform, firstColor, duration, ease)
            .translate(transform, duration, ease, y: distance)
            .rotate(transform, rotation, duration, ease)
            .shrink(transform, duration, ease, x: expand, y: expand)
            .color(transform,  secondColor, duration, ease)
            .translate(transform, duration, ease, x: -distance)
            .rotate(transform, rotation, duration, ease)
            .expand(transform, duration, ease, x: expand, y: expand)
            .color(transform, thirdColor, duration, ease)
            .translate(transform, duration, ease, y: -distance)
            .rotate(transform, rotation, duration, ease)
            .shrink(transform, duration, ease, x: expand, y: expand)
            .color(transform, fourthColor, duration, ease);
    }
}