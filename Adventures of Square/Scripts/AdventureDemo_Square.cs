using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class AdventureDemo_Square : MonoBehaviour
{
    private Script currentAction;
    
    private const float MovementDuration = 0.3f;
    private const float SpinDuration = 1.2f;
    private const float MovementDistance = 1f;
    private const Ease MovementEase = Ease.InOutCirc;
    private const Ease SpinEase = Ease.InOutElastic;
    private Script breathScript;

    private void Start()
    {
        Vector3 breath = new Vector3(0.1f, 0.1f, 0);
        float breathDuration = 1f;
        
        breathScript = transform.loop()
            .expand(transform, breath,breathDuration)
            .shrink(transform, breath, breathDuration);
    }

    public void moveLeft()
    {
        move(-1, 0);
    }

    public void moveRight()
    {
        move(1, 0);
    }

    public void moveUp()
    {
        move(0, 1);
    }

    public void moveDown()
    {
        move(0, -1);
    }

    public void spin()
    {
        if(Busy) return;

        float angle = Random.value > 0.5f ? 360 : -360; // randomly get the direction to spin in
        
        currentAction = this.script()
            .rotate(transform, angle, SpinDuration, SpinEase)
            .perform(() => currentAction = null);
    }

    private void move(int x, int y)
    {
        if(Busy) return;
        
        currentAction = this.script()
            .async((thatScript) =>
            {
                float amount = 0.25f;

                thatScript
                    .expand(transform, MovementDuration * 0.5f, Ease.Linear, y: amount)
                    .shrink(transform, MovementDuration * 0.5f, Ease.Linear, y: amount);
            })
            .perform(() =>
            {
                if (x == 1)
                {
                    rotation = 270;
                }else if (y == 1)
                {
                    rotation = 0;
                }else if (x == -1)
                {
                    rotation = 90;
                }
                else
                {
                    rotation = 180;
                }
            })
            .translate(transform, MovementDuration, MovementEase, x: x * MovementDistance, y:y * MovementDistance)
            .wait(0.05f)
            .perform(() => currentAction = null);
    }

    public void changeColor(Color color)
    {
        if (Busy) return;

        currentAction = this.script()
            .color(this,color, 1f, Ease.InOutQuad)
            .perform(() => currentAction = null);
    }

    public void moveTowards(Vector3 position)
    {
        if (Busy || arrivedAt(position)) return;
        
        Vector3 difference = position - transform.position;
        if (difference.x.abs() > difference.y.abs())
        {
            difference.x.isNegative().then(moveLeft, moveRight);
        }
        else
        {
            difference.y.isNegative().then(moveDown, moveUp);
        }
    }

    public bool arrivedAt(Vector3 position)
    {
        return transform.distanceFrom(position) < MovementDistance;
    }
    
    private bool Busy => currentAction != null;

    private float rotation
    {
        get
        {
            return transform.eulerAngles.z;
        }
        set
        {
            Vector3 eulerAngle = transform.eulerAngles;
            eulerAngle.z = value;
            transform.eulerAngles = eulerAngle;
        }
    }
}
