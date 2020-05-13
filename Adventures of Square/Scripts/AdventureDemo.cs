using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class AdventureDemo : MonoBehaviour
{
    public AdventureDemo_Square player;
    public AdventureDemo_Square guard1;
    public Image textImage;
    public Text text;
    public Transform textTarget;
    
    public Vector3 playerStartPosition;

    private Transform cameraTarget;
    private Camera camera;
    
    private const KeyCode MoveLeftKey = KeyCode.A;
    private const KeyCode MoveRightKey = KeyCode.D;
    private const KeyCode MoveUpKey = KeyCode.W;
    private const KeyCode MoveDownKey = KeyCode.S;
    private const KeyCode SpinKey = KeyCode.Space;
    private const float CameraSmooth = 0.01f;
    private const float TreeSway = 0.2f;

    void Awake()
    {
        camera = FindObjectOfType<Camera>();
        cameraTarget = player.transform;

        Script introduction = new Script("Introduction")
            .setPosition(player, playerStartPosition)
            .setPosition(camera, new Vector3(0, 0, -100))
            .animatePosition(camera, new Vector3(0, 0, -10), 3, Ease.InOutQuad)
            .say(player, "Welcome to Square Kingdom", this)
            .say(player, "Use the WASD Keys to move", this);

        Script guardPatrol = guard1.loop()
            .walkTo(guard1, guard1.transform.position - new Vector3(0, -3, 0))
            .wait(3f)
            .walkTo(guard1, guard1.transform.position)
            .wait(3f);

        Script waitUntilSpotPlayer = guard1.script()
            .waitUntilWithinDistance(guard1, player, 4f)
            .pause(guardPatrol)
            .say(guard1, "Hi there!", this)
            .say(guard1, "Press Spacebar to spin around!", this)
            .say(guard1, "Like this!", this)
            .perform(guard1.spin)
            .perform(guard1.spin)
            .wait(2)
            .say(guard1, "I'm all dizzy now...", this)
            .say(guard1, "You can also press the 1,2,3 keys to change color!", this)
            .perform(() => guard1.changeColor(Color.yellow))
            .wait(1)
            .perform(() => guard1.changeColor(Color.green))
            .wait(1)
            .say(guard1, "But my favourite color is red", this)
            .perform(() => guard1.changeColor(Color.red))
            .wait(1)
            .resume(guardPatrol);

        GameObject.FindGameObjectsWithTag("Sway").ToList().ForEach(addSway);
        GameObject.FindGameObjectsWithTag("Sparkle").ToList().ForEach(addSparkle);
        // GameObject.FindGameObjectsWithTag("Blink").ToList().ForEach(addBlink);
    }

    private void Update()
    {
        updateCamera();
        handleKeyboardInput();
        updateText();
    }

    private void updateCamera()
    {
        if (cameraTarget == null) return;
        
        Vector3 cameraPosition = camera.ScreenToWorldPoint(transform.position);
        float differenceX = cameraPosition.x - cameraTarget.position.x;
        float differenceY = cameraPosition.y - cameraTarget.position.y;
        cameraPosition.x -= differenceX * CameraSmooth;
        cameraPosition.y -= differenceY * CameraSmooth;
        camera.transform.position = cameraPosition;
    }

    private void handleKeyboardInput()
    {
        MoveLeftKey.onHeld(player.moveLeft);
        MoveRightKey.onHeld(player.moveRight);
        MoveUpKey.onHeld(player.moveUp);
        MoveDownKey.onHeld(player.moveDown);
        SpinKey.onHeld(player.spin);

        KeyCode.Alpha1.onPressed(() => player.changeColor(Color.yellow));
        KeyCode.Alpha2.onPressed(() => player.changeColor(Color.red));
        KeyCode.Alpha3.onPressed(() => player.changeColor(Color.green));
        KeyCode.Alpha4.onPressed(() => player.changeColor(Color.cyan));
        KeyCode.Alpha5.onPressed(() => player.changeColor(Color.magenta));
        KeyCode.Alpha6.onPressed(() => player.changeColor(Color.black));
    }

    private void updateText()
    {
        if (textTarget == null) return;
        
        Vector2 screenPosition = camera.WorldToScreenPoint(textTarget.position);
        screenPosition.y += 80;
        textImage.transform.position = screenPosition;
    }

    public void say(Object obj, String value)
    {
        textImage.gameObject.SetActive(true);
        text.gameObject.SetActive(true);
        text.text = value;
        textTarget = obj.Transform();
    }

    private static void addSway(Object obj)
    {
        obj.loop()
            .translate(obj, 1f, Ease.InOutQuad, TreeSway)
            .wait(() => Random.value * 3f)
            .translate(obj, 1f, Ease.InOutQuad, -TreeSway)
            .wait(() => Random.value * 3f);
    }

    private static void addBlink(Object obj)
    {
        obj.loop()
            .wait(Random.value * 8)
            .scaleTo(obj, 0.5f, Ease.Linear, new Vector3(0, 0.1f, 0))
            .scaleTo(obj, 0.5f, Ease.Linear, obj.Transform().localScale);
    }
    
    private static void addSparkle(Object obj)
    {
        obj.loop()
            .wait(Random.value * 8)
            .animateOpacity(obj, 0.8f)
            .animateOpacity(obj, 0);
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
            .setDeactive(adventureDemo.textImage);
    }
}

