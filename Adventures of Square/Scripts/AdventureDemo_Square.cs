using UnityEngine;

public class AdventureDemo_Square : MonoBehaviour
{
    private Script currentAction;
    
    private const float MovementDuration = 0.3f;
    private const float SpinDuration = 1.2f;
    private const float MovementDistance = 1f;
    private const Ease MovementEase = Ease.InOutCirc;
    private const Ease SpinEase = Ease.InOutElastic;

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
                    .expand(transform, MovementDuration, Ease.Linear, y: amount)
                    .shrink(transform, MovementDuration, Ease.Linear, y: amount);
            })
            .translate(transform, MovementDuration, MovementEase, x: x * MovementDistance, y:y * MovementDistance)
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
}
