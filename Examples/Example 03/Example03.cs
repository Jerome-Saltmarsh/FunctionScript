using UnityEngine;

public class Example03 : MonoBehaviour
{
    public Vector2 destination;
    public Transform square;
    
    void Start()
    {
        new Script("Click Listener")
            .perform(() => square.transform.position = Mouse.Position)
            .loop();

    }
}
