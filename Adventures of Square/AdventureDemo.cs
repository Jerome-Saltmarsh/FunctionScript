using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class AdventureDemo : MonoBehaviour
{
    public AdventureDemo_Square player;
    public AdventureDemo_Square guard1;
    public Text text;
    public Transform textTarget;
    
    public Vector3 playerStartPosition;
    public Vector3 playerWalkTarget;

    private Camera camera;


    void Awake()
    {
        camera = FindObjectOfType<Camera>();
        
        Script introduction = new Script("Introduction")
            .setPosition(player, playerStartPosition)
            .wait(0.5f);

        Script guardPatrol = guard1.loop()
            .walkTo(guard1, guard1.transform.position - new Vector3(0, -3, 0))
            .wait(3f)
            .walkTo(guard1, guard1.transform.position)
            .wait(3f);

        Script waitUntilSpotPlayer = guard1.script().waitUntilWithinDistance(guard1, player, 4f)
            .pause(guardPatrol)
            .say(guard1, "Hello there", this)
            .perform(() =>
            {
                text.text = "Hello there!";
                textTarget = guard1.transform;
            })
            .wait(2f)
            .say(guard1, "Welcome to the kingdom", this);
    }

    private void Update()
    {
        if (textTarget != null)
        {
            Vector2 screenPosition = camera.WorldToScreenPoint(textTarget.position);
            text.transform.position = screenPosition;
        }
    }

    public void say(Object obj, String value)
    {
        text.gameObject.SetActive(true);
        text.text = value;
        textTarget = obj.Transform();
    }
}

public static class SquareScripts
{
    public static Script walkTo(this Script script, AdventureDemo_Square square, Vector3 position)
    {
        return script
            .performUntil(
                () => square.moveTowards(position), 
                () => square.arrivedAt(position));

    }

    public static Script say(this Script script, Object obj, String text, AdventureDemo adventureDemo)
    {
        return script
            .perform(() =>
                adventureDemo.say(obj, text)
            )
            .setOpacity(adventureDemo.text, 0f)
            .animateOpacity(adventureDemo.text, 1f, 0.4f, Ease.InOutQuad)
            .wait(2f)
            .animateOpacity(adventureDemo.text, 0, 0.4f, Ease.InOutQuad)
            .setDeactive(adventureDemo.text);
    }
}

