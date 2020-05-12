using UnityEngine;

public class Example03 : MonoBehaviour
{
    public Transform a;
    public Transform b;
    
    void Start()
    {
        Script flashScript = new Script("Flashing")
            .animateOpacity(a, 0)
            .animateOpacity(a, 1)
            .loop();

        this.script()
            .waitUntilWithinDistance(a, b, 3f)
            .pause(flashScript)
            .color(a, Color.blue)
            .wait(5)
            .resume(flashScript);
    }
}
