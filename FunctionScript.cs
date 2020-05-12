/*
 * Created by Jerome Saltmarsh
 * 
 * https://gitlab.com/JeromeSaltmarsh/functionscript
 * 
 * The MIT License (MIT)
 * 
 * Copyright (c) 2020
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public enum OnScriptFinished
{
    Destroy_Behavior,
    Disable_Behavior,
    Destroy_GameObject,
    Deactivate_GameObject,
    Loop
}

public class Script
{
    private enum Channel
    {
        Update,
        Fixed_Update
    }

    private class GameActionChain : MonoBehaviour
    {
        public List<Func<bool>> actions = new List<Func<bool>>();
        public OnScriptFinished onScriptFinished;
        public int index;
        public Channel channel = Channel.Fixed_Update;

        private void FixedUpdate()
        {
            if (channel == Channel.Fixed_Update)
            {
                run();
            }
        }

        private void Update()
        {
            if (channel == Channel.Update)
            {
                run();
            }
        }

        private void run()
        {
            for (; index < actions.Count; index++)
            {
                if (!actions[index].Invoke()) return;
            }

            switch (onScriptFinished)
            {
                case OnScriptFinished.Destroy_Behavior:
                    Destroy(this);
                    return;
                case OnScriptFinished.Disable_Behavior:
                    enabled = false;
                    return;
                case OnScriptFinished.Deactivate_GameObject:
                    gameObject.SetActive(false);
                    return;
                case OnScriptFinished.Destroy_GameObject:
                    Destroy(gameObject);
                    return;
                case OnScriptFinished.Loop:
                    index = 0;
                    return;
            }
        }
    }

    public string name => actionChain.name;
    public GameObject gameObject => actionChain.gameObject;
    private readonly GameActionChain actionChain;
    private static GameObject scripts;

    public Script(
        GameObject gameObject,
        OnScriptFinished onScriptFinished = OnScriptFinished.Destroy_Behavior,
        bool newGameActionChain = true)
    {
        actionChain = newGameActionChain
            ? gameObject.AddComponent<GameActionChain>()
            : gameObject.GetOrAdd<GameActionChain>();
        actionChain.onScriptFinished = onScriptFinished;
    }

    public void setOnFinished(OnScriptFinished value)
    {
        actionChain.onScriptFinished = value;
    }

    public void pause()
    {
        actionChain.enabled = false;
    }

    public void resume()
    {
        actionChain.enabled = true;
    }

    public Script(string name, OnScriptFinished onScriptFinished = OnScriptFinished.Deactivate_GameObject,
        Transform parent = null)
    {
        actionChain = new GameObject("Script - " + name).AddComponent<GameActionChain>();
        actionChain.onScriptFinished = onScriptFinished;

        if (parent == null)
        {
            if (scripts == null)
            {
                scripts = new GameObject("Scripts");
            }

            actionChain.transform.parent = scripts.transform;
        }
        else
        {
            actionChain.transform.parent = parent;
        }
    }

    public Script performIf(Func<bool> condition, Action action, Action elseAction = null)
    {
        return perform(delegate
        {
            if (condition())
            {
                action();
            }
            else
            {
                elseAction?.Invoke();
            }
        });
    }

    public Script performIf(bool condition, Action action, Action elseAction = null)
    {
        return perform(delegate
        {
            if (condition)
            {
                action();
            }
            else
            {
                elseAction?.Invoke();
            }
        });
    }

    public Script performFor(Action action, float duration)
    {
        float endTime = 0;
        return perform(() => endTime = Time.time + duration)
            .performUntil(action, () => Time.time > endTime);
    }

    public Script performFor(Action action, int times)
    {
        for (int i = 0; i < times; i++)
        {
            perform(action);
        }

        return this;
    }

    public Script async(Action<Script> asyncScript, string name = "Async")
    {
        return perform(delegate { asyncScript(new Script(name, OnScriptFinished.Destroy_GameObject)); });
    }

    public Script perform(Action action)
    {
        return add(
            delegate
            {
                action();
                return true;
            }
        );
    }

    public Script performUntil(Action action, Func<bool> condition)
    {
        return add(delegate
        {
            if (condition())
            {
                return true;
            }

            action();
            return false;
        });
    }

    public Script waitUntil(Func<bool> condition)
    {
        return add(condition);
    }

    public void deactivateActionChain()
    {
        perform(() => actionChain.gameObject.SetActive(false));
    }

    public void destroyActionChain()
    {
        perform(() => GameObject.Destroy(actionChain));
    }

    protected Script add(Func<bool> action)
    {
        actionChain.actions.Add(action);
        return this;
    }

    public Script setChannelUpdate()
    {
        return setChannel(Channel.Update);
    }

    public Script setChannelFixedUpdate()
    {
        return setChannel(Channel.Fixed_Update);
    }

    private Script setChannel(Channel channel)
    {
        return perform(() => actionChain.channel = channel);
    }

    public Script loop()
    {
        actionChain.onScriptFinished = OnScriptFinished.Loop;
        // setActionIndex(0);
        return this;
    }

    public Script setActionIndex(int value)
    {
        return perform(delegate { actionChain.index = value; });
    }

    public void destroy()
    {
        GameObject.Destroy(actionChain);
    }
}

public static class ScriptExtensionsCore
{
    public static Script rotate(this Script script, Object obj, float angle, float duration = 1f,
        Ease ease = Ease.Linear)
    {
        Transform transform = obj.Transform();

        return script.tween(
            angle,
            duration,
            eulerZ =>
            {
                Vector3 eulerAngles = transform.eulerAngles;
                eulerAngles.z += eulerZ;
                transform.eulerAngles = eulerAngles;
            }, ease);
    }

    public static Script tween(this Script script, float value, float duration,
        Action<float> tweenFunction, Ease ease = Ease.Linear)
    {
        float startTime = 0;
        float endTime = 0;
        float previousValue = 0;
        EasingFunction.Function easeFunction = EasingFunction.GetEasingFunction(ease);

        return script.perform(() =>
            {
                startTime = Time.time;
                endTime = startTime + duration;
                previousValue = 0;
            }).performUntil(() =>
            {
                float timeElapsed = Time.time - startTime;
                float easeValue = timeElapsed / duration;
                float val = easeFunction(0, value, easeValue);
                tweenFunction(val - previousValue);
                previousValue = val;
            }, () => Time.time >= endTime)
            .perform(
                () => tweenFunction(easeFunction(0, value, 1f) - previousValue)
            );
    }

    public static Script remove(this Script script, Component component)
    {
        return script.perform(() => GameObject.Destroy(component));
    }

    public static Script remove<T>(this Script script, Component component) where T : Component
    {
        return script.perform(delegate
        {
            T t = component.GetComponent<T>();
            if (t != null)
            {
                GameObject.Destroy(t);
            }
        });
    }

    public static Script remove<T>(this Script script, GameObject gameObject) where T : Component
    {
        return script.perform(delegate
        {
            T t = gameObject.GetComponent<T>();
            if (t != null)
            {
                GameObject.Destroy(t);
            }
        });
    }

    public static Script setSprite(this Script script, GameObject gameObject, Sprite sprite)
    {
        return script.setSprite(gameObject.GetComponent<SpriteRenderer>(), sprite);
    }

    public static Script setSprite(this Script script, Component component, Sprite sprite)
    {
        return script.setSprite(component.GetComponent<SpriteRenderer>(), sprite);
    }

    public static Script setSprite(this Script script, SpriteRenderer spriteRenderer, Sprite sprite)
    {
        return script.perform(() => spriteRenderer.sprite = sprite);
    }

    public static Script destroy(this Script script, GameObject target)
    {
        return script.perform(() => GameObject.Destroy(target));
    }

    public static Script setActive(this Script script, Component component)
    {
        return script.setActive(component.gameObject);
    }

    public static Script setActive(this Script script, GameObject gameObject, bool value = true)
    {
        return script.perform(() => gameObject.SetActive(value));
    }

    public static Script setDeactive(this Script script, Component component)
    {
        return script.setDeactive(component.gameObject);
    }

    public static Script setOpacity(this Script script, Object obj, float value)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>(obj);
        if (spriteRenderer != null)
        {
            return script.perform(() =>
            {
                script.setOpacity(spriteRenderer, value);
            }); 
        }
        Graphic graphic = GetComponent<Graphic>(obj);
        if (graphic != null)
        {
            return script.setOpacity(graphic, value);
        }
        return script;
    }

    public static Script setOpacity(this Script script, Graphic graphic, float value)
    {
        return script.perform(() =>
        {
            Color color = graphic.color;
            color.a = value;
            graphic.color = color;
        });
    }
    
    public static Script setOpacity(this Script script, SpriteRenderer spriteRenderer, float value)
    {
        return script.perform(() =>
        {
            Color color = spriteRenderer.color;
            color.a = value;
            spriteRenderer.color = color;
        });
    }

    public static Script setDeactive(this Script script, GameObject gameObject)
    {
        return script.setActive(gameObject, false);
    }

    public static Script wait(this Script script, float duration)
    {
        float endTime = 0;
        return script
            .perform(() => endTime = Time.time + duration)
            .waitUntil(() => Time.time >= endTime);
    }

    public static Script log(this Script script, string value)
    {
        return script.perform(() => Debug.Log("Script " + script.name + " : " + value));
    }

    public static Script enableCollider(this Script script, GameObject gameObject)
    {
        return script.perform(() => gameObject.GetComponent<Collider>().enabled = true);
    }

    public static Script enableCollider(this Script script, Component component)
    {
        return script.perform(() => component.GetComponent<Collider>().enabled = true);
    }

    public static Script disableCollider(this Script script, GameObject gameObject)
    {
        return script.perform(() => gameObject.GetComponent<Collider>().enabled = false);
    }

    public static Script disableCollider(this Script script, Component component)
    {
        return script.perform(() => component.GetComponent<Collider>().enabled = false);
    }

    public static Script disable<T>(this Script script, Component target) where T : MonoBehaviour
    {
        return script.perform(delegate
        {
            T t = target.GetComponent<T>();
            if (t != null)
            {
                t.enabled = false;
            }
        });
    }

    public static Script animatePosition(this Script script, Object objekt, Vector3 destination, float duration = 1f,
        Ease ease = Ease.Linear)
    {
        Transform transform = GetTransform(objekt);
        Vector3 startingPosition = Vector3.zero;
        float startTime = 0;
        float endTime = 0;
        float totalDistance = 0;
        Vector3 translation = Vector3.zero;
        EasingFunction.Function easeFunction = EasingFunction.GetEasingFunction(ease);

        return script.perform(() =>
            {
                startTime = Time.time;
                endTime = startTime + duration;
                startingPosition = transform.position;
                totalDistance = Vector3.Distance(transform.position, destination);
                translation = destination - transform.position;
            })
            .performUntil(
                () =>
                {
                    float timeElapsed = Time.time - startTime;
                    float distance = easeFunction(0, totalDistance, timeElapsed / duration);
                    transform.position = startingPosition + Vector3.ClampMagnitude(translation, distance);
                }
                , () => Time.time > endTime);
    }

    public static Script animatePosition(this Script script, Object objekt, Object destination, float duration = 1f,
        Ease ease = Ease.Linear)
    {
        Transform transform = GetTransform(objekt);
        Transform targetTransform = GetTransform(destination);
        Vector3 startingPosition = Vector3.zero;
        float startTime = 0;
        float endTime = 0;
        float totalDistance = 0;
        Vector3 translation = Vector3.zero;
        EasingFunction.Function easeFunction = EasingFunction.GetEasingFunction(ease);

        return script.perform(() =>
            {
                startTime = Time.time;
                endTime = startTime + duration;
                startingPosition = transform.position;
                totalDistance = Vector3.Distance(transform.position, targetTransform.position);
                translation = targetTransform.position - transform.position;
            })
            .performUntil(
                () =>
                {
                    float timeElapsed = Time.time - startTime;
                    float distance = easeFunction(0, totalDistance, timeElapsed / duration);
                    transform.position = startingPosition + Vector3.ClampMagnitude(translation, distance);
                }
                , () => Time.time > endTime);
    }

    public static Script translate(this Script script, Object obj, float duration = 1f,
        Ease ease = Ease.Linear, float x = 0, float y = 0, float z = 0)
    {
        return script.translate(obj, new Vector3(x, y, z), duration, ease); 
    }

    public static Script translate(this Script script, Object obj, Vector3 translation, float duration,
        Ease ease = Ease.Linear)
    {
        Transform transform = GetTransform(obj);

        return script
            .tween(translation.magnitude, duration,
                distance => transform.position += Vector3.ClampMagnitude(translation, distance)
                , ease);
    }

    public static Script expand(this Script script, Object obj, float duration = 1f, Ease ease = Ease.Linear,
        float x = 0, float y = 0, float z = 0)
    {
        return script.expand(obj, new Vector3(x, y, z), duration, ease);
    }

    public static Script expand(this Script script, Object obj, Vector3 scale, float duration = 1f, Ease ease = Ease.Linear)
    {
        Transform transform = GetTransform(obj);
        
        return script
            .tween(scale.magnitude, duration,
                distance => transform.localScale += Vector3.ClampMagnitude(scale, distance)
                , ease);
    }

    public static Script color(this Script script, Object obj, Color color, float duration = 1,
        Ease ease = Ease.Linear)
    {
        float value = 0;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>(obj);

        if (spriteRenderer != null)
        {
            Color startingColor = Color.white;
            
            return script.
                perform(() =>
                {
                    startingColor = spriteRenderer.color;
                    value = 0;
                })
                .tween(duration, duration, (val) =>
            {
                value += val;
                spriteRenderer.color = Color.Lerp(startingColor, color, value);

            }, ease);
        }

        return script;
    }
    
    public static Script shrink(this Script script, Object obj, float duration = 1f, Ease ease = Ease.Linear,
        float x = 0, float y = 0, float z = 0)
    {
        return script.shrink(obj, new Vector3(x, y, z), duration, ease);
    }
    
    public static Script shrink(this Script script, Object obj, Vector3 scale, float duration, Ease ease = Ease.Linear)
    {
        Transform transform = GetTransform(obj);
        
        return script
            .tween(scale.magnitude, duration,
                distance => transform.localScale -= Vector3.ClampMagnitude(scale, distance)
                , ease);
    }

    public static Script animateOpacity(this Script script, Object objekt, float targetOpacity, float duration = 1f,
        Ease ease = Ease.Linear, bool childrenToo = true)
    {
        if (childrenToo)
        {
            script.perform(delegate
            {
                foreach (Transform child in GetTransform(objekt))
                {
                    script.async(asyncScript =>
                        asyncScript.animateOpacity(child, targetOpacity, duration, ease, childrenToo));
                }
            });
        }

        float startTime = 0;
        float endTime = 0;
        float startOpacity = 0;
        float value = 0;
        EasingFunction.Function easeFunction = EasingFunction.GetEasingFunction(ease);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>(objekt);
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            script.perform(delegate
            {
                color = spriteRenderer.color;
                startOpacity = color.a;
                startTime = Time.time;
                endTime = startTime + duration;
            }).performUntil(() =>
                {
                    float timeElapsed = Time.time - startTime;
                    color.a = easeFunction(startOpacity, targetOpacity, timeElapsed / duration);
                    spriteRenderer.color = color;
                },
                () => Time.time >= endTime);
        }
        else
        {
            Graphic graphic = GetComponent<Graphic>(objekt);
            if (graphic != null)
            {
                Color color = graphic.color;
                script.perform(() =>
                {
                    startTime = Time.time;
                    endTime = startTime + duration;
                    color = graphic.color;
                    startOpacity = color.a;
                }).performUntil(delegate
                    {
                        float timeElapsed = Time.time - startTime;
                        color.a = easeFunction(startOpacity, targetOpacity, timeElapsed / duration);
                        graphic.color = color;
                    },
                    () => Time.time >= endTime);
            }
        }

        return script;
    }

    public static Script waitUntilWithinDistance(this Script script, Object a, Object b, float distance)
    {
        Transform aTransform = GetTransform(a);
        Transform bTransform = GetTransform(b);
        return script.waitUntil(() => Vector3.Distance(aTransform.position, bTransform.position) <= distance);
    }

    public static Script pause(this Script script, Script that)
    {
        return script.perform(that.pause);
    }

    public static Script resume(this Script script, Script that)
    {
        return script.perform(that.resume);
    }

    public static Script waitUntilDistanceApart(this Script script, Object a, Object b, float distance)
    {
        Transform aTransform = GetTransform(a);
        Transform bTransform = GetTransform(b);
        return script.waitUntil(() => Vector3.Distance(aTransform.position, bTransform.position) > distance);
    }

    private static Transform GetTransform(Object obj)
    {
        if (obj is GameObject gameObject)
        {
            return gameObject.transform;
        }

        return ((Component) obj).transform;
    }

    private static T GetComponent<T>(Object obj) where T : Component
    {
        if (obj is GameObject gameObject)
        {
            return gameObject.GetComponent<T>();
        }

        return ((Component) obj).GetComponent<T>();
    }
}

public static class Mouse
{
    private const int Left = 0;
    private const int Right = 1;

    public static bool LeftClicked => Input.GetMouseButton(Left);

    public static bool RightClicked => Input.GetMouseButton(Right);

    public static Vector2 ScreenPosition => Input.mousePosition;
}

public static class GraphicExtensions
{
    public static Script waitUntilClicked(this Script script, Graphic graphic)
    {
        return script
            .setChannelUpdate()
            .waitUntil(graphic.isLeftClicked)
            .setChannelFixedUpdate();
    }
    
    public static bool isLeftClicked(this Graphic graphic)
    {
        return Mouse.LeftClicked && graphic.containsMouse();
    }

    public static bool containsMouse(this Graphic graphic)
    {
        return graphic.contains(Mouse.ScreenPosition);
    }

    public static bool contains(this Graphic graphic, Vector2 screenPosition)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, screenPosition);
    }
}

public static class IOExtensions
{
    public static bool isPressed(this KeyCode keyCode)
    {
        return Input.GetKeyDown(keyCode);
    }

    public static bool isReleased(this KeyCode keyCode)
    {
        return Input.GetKeyUp(keyCode);
    }

    public static bool isHeld(this KeyCode keyCode)
    {
        return Input.GetKey(keyCode);
    }

    public static Script waitUntilLeftClick(this Script script)
    {
        return script
            .setChannelUpdate()
            .waitUntil(() => Mouse.LeftClicked)
            .setChannelFixedUpdate();
    }
}

public static class ObjectExtensions
{
    public static T GetOrAdd<T>(this GameObject gameObject) where T : Component
    {
        T t = gameObject.GetComponent<T>();
        if (t == null)
        {
            gameObject.AddComponent<T>();
        }

        return t;
    }


    public static Script script(this Component component, OnScriptFinished onScriptFinished = OnScriptFinished.Destroy_Behavior)
    {
        return new Script(component.gameObject, onScriptFinished);
    }

    public static Script loop(this Component component)
    {
        return new Script(component.gameObject, OnScriptFinished.Loop);
    }

    public static Transform Transform(this Object obj)
    {
        switch (obj)
        {
            case GameObject gameObject:
                return gameObject.transform;
            case Component component:
                return component.transform;
            default:
                throw new UnityException("Cannot get transform from object");
        }
    }
}


/*
 * https://gist.github.com/cjddmut/d789b9eb78216998e95c
 * Created by C.J. Kimberlin
 * 
 * The MIT License (MIT)
 * 
 * Copyright (c) 2019
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 * 
 * TERMS OF USE - EASING EQUATIONS
 * Open source under the BSD License.
 * Copyright (c)2001 Robert Penner
 * All rights reserved.
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
 * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
 * Neither the name of the author nor the names of contributors may be used to endorse or promote products derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, 
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE 
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; 
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 *
 * ============= Description =============
 *
 * Below is an example of how to use the easing functions in the file. There is a getting function that will return the function
 * from an enum. This is useful since the enum can be exposed in the editor and then the function queried during Start().
 * 
 * EasingFunction.Ease ease = EasingFunction.Ease.InOutQuad;
 * EasingFunction.EasingFunc func = GetEasingFunction(ease;
 * 
 * float value = func(0, 10, 0.67f);
 * 
 * EasingFunction.EaseingFunc derivativeFunc = GetEasingFunctionDerivative(ease);
 * 
 * float derivativeValue = derivativeFunc(0, 10, 0.67f);
 */
public enum Ease
{
    InQuad = 0,
    OutQuad,
    InOutQuad,
    InCubic,
    OutCubic,
    InOutCubic,
    InQuart,
    OutQuart,
    InOutQuart,
    InQuint,
    OutQuint,
    InOutQuint,
    InSine,
    OutSine,
    InOutSine,
    InExpo,
    OutExpo,
    InOutExpo,
    InCirc,
    OutCirc,
    InOutCirc,
    Linear,
    Spring,
    InBounce,
    OutBounce,
    InOutBounce,
    InBack,
    OutBack,
    InOutBack,
    InElastic,
    OutElastic,
    InOutElastic,
}

public static class EasingFunction
{
    private const float NATURAL_LOG_OF_2 = 0.693147181f;

    //
    // Easing functions
    //

    public static float Linear(float start, float end, float value)
    {
        return Mathf.Lerp(start, end, value);
    }

    public static float Spring(float start, float end, float value)
    {
        value = Mathf.Clamp01(value);
        value = (Mathf.Sin(value * Mathf.PI * (0.2f + 2.5f * value * value * value)) * Mathf.Pow(1f - value, 2.2f) +
                 value) * (1f + (1.2f * (1f - value)));
        return start + (end - start) * value;
    }

    public static float EaseInQuad(float start, float end, float value)
    {
        end -= start;
        return end * value * value + start;
    }

    public static float EaseOutQuad(float start, float end, float value)
    {
        end -= start;
        return -end * value * (value - 2) + start;
    }

    public static float EaseInOutQuad(float start, float end, float value)
    {
        value /= .5f;
        end -= start;
        if (value < 1) return end * 0.5f * value * value + start;
        value--;
        return -end * 0.5f * (value * (value - 2) - 1) + start;
    }

    public static float EaseInCubic(float start, float end, float value)
    {
        end -= start;
        return end * value * value * value + start;
    }

    public static float EaseOutCubic(float start, float end, float value)
    {
        value--;
        end -= start;
        return end * (value * value * value + 1) + start;
    }

    public static float EaseInOutCubic(float start, float end, float value)
    {
        value /= .5f;
        end -= start;
        if (value < 1) return end * 0.5f * value * value * value + start;
        value -= 2;
        return end * 0.5f * (value * value * value + 2) + start;
    }

    public static float EaseInQuart(float start, float end, float value)
    {
        end -= start;
        return end * value * value * value * value + start;
    }

    public static float EaseOutQuart(float start, float end, float value)
    {
        value--;
        end -= start;
        return -end * (value * value * value * value - 1) + start;
    }

    public static float EaseInOutQuart(float start, float end, float value)
    {
        value /= .5f;
        end -= start;
        if (value < 1) return end * 0.5f * value * value * value * value + start;
        value -= 2;
        return -end * 0.5f * (value * value * value * value - 2) + start;
    }

    public static float EaseInQuint(float start, float end, float value)
    {
        end -= start;
        return end * value * value * value * value * value + start;
    }

    public static float EaseOutQuint(float start, float end, float value)
    {
        value--;
        end -= start;
        return end * (value * value * value * value * value + 1) + start;
    }

    public static float EaseInOutQuint(float start, float end, float value)
    {
        value /= .5f;
        end -= start;
        if (value < 1) return end * 0.5f * value * value * value * value * value + start;
        value -= 2;
        return end * 0.5f * (value * value * value * value * value + 2) + start;
    }

    public static float EaseInSine(float start, float end, float value)
    {
        end -= start;
        return -end * Mathf.Cos(value * (Mathf.PI * 0.5f)) + end + start;
    }

    public static float EaseOutSine(float start, float end, float value)
    {
        end -= start;
        return end * Mathf.Sin(value * (Mathf.PI * 0.5f)) + start;
    }

    public static float EaseInOutSine(float start, float end, float value)
    {
        end -= start;
        return -end * 0.5f * (Mathf.Cos(Mathf.PI * value) - 1) + start;
    }

    public static float EaseInExpo(float start, float end, float value)
    {
        end -= start;
        return end * Mathf.Pow(2, 10 * (value - 1)) + start;
    }

    public static float EaseOutExpo(float start, float end, float value)
    {
        end -= start;
        return end * (-Mathf.Pow(2, -10 * value) + 1) + start;
    }

    public static float EaseInOutExpo(float start, float end, float value)
    {
        value /= .5f;
        end -= start;
        if (value < 1) return end * 0.5f * Mathf.Pow(2, 10 * (value - 1)) + start;
        value--;
        return end * 0.5f * (-Mathf.Pow(2, -10 * value) + 2) + start;
    }

    public static float EaseInCirc(float start, float end, float value)
    {
        end -= start;
        return -end * (Mathf.Sqrt(1 - value * value) - 1) + start;
    }

    public static float EaseOutCirc(float start, float end, float value)
    {
        value--;
        end -= start;
        return end * Mathf.Sqrt(1 - value * value) + start;
    }

    public static float EaseInOutCirc(float start, float end, float value)
    {
        value /= .5f;
        end -= start;
        if (value < 1) return -end * 0.5f * (Mathf.Sqrt(1 - value * value) - 1) + start;
        value -= 2;
        return end * 0.5f * (Mathf.Sqrt(1 - value * value) + 1) + start;
    }

    public static float EaseInBounce(float start, float end, float value)
    {
        end -= start;
        float d = 1f;
        return end - EaseOutBounce(0, end, d - value) + start;
    }

    public static float EaseOutBounce(float start, float end, float value)
    {
        value /= 1f;
        end -= start;
        if (value < (1 / 2.75f))
        {
            return end * (7.5625f * value * value) + start;
        }
        else if (value < (2 / 2.75f))
        {
            value -= (1.5f / 2.75f);
            return end * (7.5625f * (value) * value + .75f) + start;
        }
        else if (value < (2.5 / 2.75))
        {
            value -= (2.25f / 2.75f);
            return end * (7.5625f * (value) * value + .9375f) + start;
        }
        else
        {
            value -= (2.625f / 2.75f);
            return end * (7.5625f * (value) * value + .984375f) + start;
        }
    }

    public static float EaseInOutBounce(float start, float end, float value)
    {
        end -= start;
        float d = 1f;
        if (value < d * 0.5f) return EaseInBounce(0, end, value * 2) * 0.5f + start;
        else return EaseOutBounce(0, end, value * 2 - d) * 0.5f + end * 0.5f + start;
    }

    public static float EaseInBack(float start, float end, float value)
    {
        end -= start;
        value /= 1;
        float s = 1.70158f;
        return end * (value) * value * ((s + 1) * value - s) + start;
    }

    public static float EaseOutBack(float start, float end, float value)
    {
        float s = 1.70158f;
        end -= start;
        value = (value) - 1;
        return end * ((value) * value * ((s + 1) * value + s) + 1) + start;
    }

    public static float EaseInOutBack(float start, float end, float value)
    {
        float s = 1.70158f;
        end -= start;
        value /= .5f;
        if ((value) < 1)
        {
            s *= (1.525f);
            return end * 0.5f * (value * value * (((s) + 1) * value - s)) + start;
        }

        value -= 2;
        s *= (1.525f);
        return end * 0.5f * ((value) * value * (((s) + 1) * value + s) + 2) + start;
    }

    public static float EaseInElastic(float start, float end, float value)
    {
        end -= start;

        float d = 1f;
        float p = d * .3f;
        float s;
        float a = 0;

        if (value == 0) return start;

        if ((value /= d) == 1) return start + end;

        if (a == 0f || a < Mathf.Abs(end))
        {
            a = end;
            s = p / 4;
        }
        else
        {
            s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
        }

        return -(a * Mathf.Pow(2, 10 * (value -= 1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p)) + start;
    }

    public static float EaseOutElastic(float start, float end, float value)
    {
        end -= start;

        float d = 1f;
        float p = d * .3f;
        float s;
        float a = 0;

        if (value == 0) return start;

        if ((value /= d) == 1) return start + end;

        if (a == 0f || a < Mathf.Abs(end))
        {
            a = end;
            s = p * 0.25f;
        }
        else
        {
            s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
        }

        return (a * Mathf.Pow(2, -10 * value) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p) + end + start);
    }

    public static float EaseInOutElastic(float start, float end, float value)
    {
        end -= start;

        float d = 1f;
        float p = d * .3f;
        float s;
        float a = 0;

        if (value == 0) return start;

        if ((value /= d * 0.5f) == 2) return start + end;

        if (a == 0f || a < Mathf.Abs(end))
        {
            a = end;
            s = p / 4;
        }
        else
        {
            s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
        }

        if (value < 1)
            return -0.5f * (a * Mathf.Pow(2, 10 * (value -= 1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p)) +
                   start;
        return a * Mathf.Pow(2, -10 * (value -= 1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p) * 0.5f + end +
               start;
    }

    //
    // These are derived functions that the motor can use to get the speed at a specific time.
    //
    // The easing functions all work with a normalized time (0 to 1) and the returned value here
    // reflects that. Values returned here should be divided by the actual time.
    //
    // TODO: These functions have not had the testing they deserve. If there is odd behavior around
    //       dash speeds then this would be the first place I'd look.

    public static float LinearD(float start, float end, float value)
    {
        return end - start;
    }

    public static float EaseInQuadD(float start, float end, float value)
    {
        return 2f * (end - start) * value;
    }

    public static float EaseOutQuadD(float start, float end, float value)
    {
        end -= start;
        return -end * value - end * (value - 2);
    }

    public static float EaseInOutQuadD(float start, float end, float value)
    {
        value /= .5f;
        end -= start;

        if (value < 1)
        {
            return end * value;
        }

        value--;

        return end * (1 - value);
    }

    public static float EaseInCubicD(float start, float end, float value)
    {
        return 3f * (end - start) * value * value;
    }

    public static float EaseOutCubicD(float start, float end, float value)
    {
        value--;
        end -= start;
        return 3f * end * value * value;
    }

    public static float EaseInOutCubicD(float start, float end, float value)
    {
        value /= .5f;
        end -= start;

        if (value < 1)
        {
            return (3f / 2f) * end * value * value;
        }

        value -= 2;

        return (3f / 2f) * end * value * value;
    }

    public static float EaseInQuartD(float start, float end, float value)
    {
        return 4f * (end - start) * value * value * value;
    }

    public static float EaseOutQuartD(float start, float end, float value)
    {
        value--;
        end -= start;
        return -4f * end * value * value * value;
    }

    public static float EaseInOutQuartD(float start, float end, float value)
    {
        value /= .5f;
        end -= start;

        if (value < 1)
        {
            return 2f * end * value * value * value;
        }

        value -= 2;

        return -2f * end * value * value * value;
    }

    public static float EaseInQuintD(float start, float end, float value)
    {
        return 5f * (end - start) * value * value * value * value;
    }

    public static float EaseOutQuintD(float start, float end, float value)
    {
        value--;
        end -= start;
        return 5f * end * value * value * value * value;
    }

    public static float EaseInOutQuintD(float start, float end, float value)
    {
        value /= .5f;
        end -= start;

        if (value < 1)
        {
            return (5f / 2f) * end * value * value * value * value;
        }

        value -= 2;

        return (5f / 2f) * end * value * value * value * value;
    }

    public static float EaseInSineD(float start, float end, float value)
    {
        return (end - start) * 0.5f * Mathf.PI * Mathf.Sin(0.5f * Mathf.PI * value);
    }

    public static float EaseOutSineD(float start, float end, float value)
    {
        end -= start;
        return (Mathf.PI * 0.5f) * end * Mathf.Cos(value * (Mathf.PI * 0.5f));
    }

    public static float EaseInOutSineD(float start, float end, float value)
    {
        end -= start;
        return end * 0.5f * Mathf.PI * Mathf.Sin(Mathf.PI * value);
    }

    public static float EaseInExpoD(float start, float end, float value)
    {
        return (10f * NATURAL_LOG_OF_2 * (end - start) * Mathf.Pow(2f, 10f * (value - 1)));
    }

    public static float EaseOutExpoD(float start, float end, float value)
    {
        end -= start;
        return 5f * NATURAL_LOG_OF_2 * end * Mathf.Pow(2f, 1f - 10f * value);
    }

    public static float EaseInOutExpoD(float start, float end, float value)
    {
        value /= .5f;
        end -= start;

        if (value < 1)
        {
            return 5f * NATURAL_LOG_OF_2 * end * Mathf.Pow(2f, 10f * (value - 1));
        }

        value--;

        return (5f * NATURAL_LOG_OF_2 * end) / (Mathf.Pow(2f, 10f * value));
    }

    public static float EaseInCircD(float start, float end, float value)
    {
        return ((end - start) * value) / Mathf.Sqrt(1f - value * value);
    }

    public static float EaseOutCircD(float start, float end, float value)
    {
        value--;
        end -= start;
        return (-end * value) / Mathf.Sqrt(1f - value * value);
    }

    public static float EaseInOutCircD(float start, float end, float value)
    {
        value /= .5f;
        end -= start;

        if (value < 1)
        {
            return (end * value) / (2f * Mathf.Sqrt(1f - value * value));
        }

        value -= 2;

        return (-end * value) / (2f * Mathf.Sqrt(1f - value * value));
    }

    public static float EaseInBounceD(float start, float end, float value)
    {
        end -= start;
        float d = 1f;

        return EaseOutBounceD(0, end, d - value);
    }

    public static float EaseOutBounceD(float start, float end, float value)
    {
        value /= 1f;
        end -= start;

        if (value < (1 / 2.75f))
        {
            return 2f * end * 7.5625f * value;
        }
        else if (value < (2 / 2.75f))
        {
            value -= (1.5f / 2.75f);
            return 2f * end * 7.5625f * value;
        }
        else if (value < (2.5 / 2.75))
        {
            value -= (2.25f / 2.75f);
            return 2f * end * 7.5625f * value;
        }
        else
        {
            value -= (2.625f / 2.75f);
            return 2f * end * 7.5625f * value;
        }
    }

    public static float EaseInOutBounceD(float start, float end, float value)
    {
        end -= start;
        float d = 1f;

        if (value < d * 0.5f)
        {
            return EaseInBounceD(0, end, value * 2) * 0.5f;
        }
        else
        {
            return EaseOutBounceD(0, end, value * 2 - d) * 0.5f;
        }
    }

    public static float EaseInBackD(float start, float end, float value)
    {
        float s = 1.70158f;

        return 3f * (s + 1f) * (end - start) * value * value - 2f * s * (end - start) * value;
    }

    public static float EaseOutBackD(float start, float end, float value)
    {
        float s = 1.70158f;
        end -= start;
        value = (value) - 1;

        return end * ((s + 1f) * value * value + 2f * value * ((s + 1f) * value + s));
    }

    public static float EaseInOutBackD(float start, float end, float value)
    {
        float s = 1.70158f;
        end -= start;
        value /= .5f;

        if ((value) < 1)
        {
            s *= (1.525f);
            return 0.5f * end * (s + 1) * value * value + end * value * ((s + 1f) * value - s);
        }

        value -= 2;
        s *= (1.525f);
        return 0.5f * end * ((s + 1) * value * value + 2f * value * ((s + 1f) * value + s));
    }

    public static float EaseInElasticD(float start, float end, float value)
    {
        return EaseOutElasticD(start, end, 1f - value);
    }

    public static float EaseOutElasticD(float start, float end, float value)
    {
        end -= start;

        float d = 1f;
        float p = d * .3f;
        float s;
        float a = 0;

        if (a == 0f || a < Mathf.Abs(end))
        {
            a = end;
            s = p * 0.25f;
        }
        else
        {
            s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
        }

        return (a * Mathf.PI * d * Mathf.Pow(2f, 1f - 10f * value) *
                Mathf.Cos((2f * Mathf.PI * (d * value - s)) / p)) / p - 5f * NATURAL_LOG_OF_2 * a *
            Mathf.Pow(2f, 1f - 10f * value) * Mathf.Sin((2f * Mathf.PI * (d * value - s)) / p);
    }

    public static float EaseInOutElasticD(float start, float end, float value)
    {
        end -= start;

        float d = 1f;
        float p = d * .3f;
        float s;
        float a = 0;

        if (a == 0f || a < Mathf.Abs(end))
        {
            a = end;
            s = p / 4;
        }
        else
        {
            s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
        }

        if (value < 1)
        {
            value -= 1;

            return -5f * NATURAL_LOG_OF_2 * a * Mathf.Pow(2f, 10f * value) *
                   Mathf.Sin(2 * Mathf.PI * (d * value - 2f) / p) -
                   a * Mathf.PI * d * Mathf.Pow(2f, 10f * value) * Mathf.Cos(2 * Mathf.PI * (d * value - s) / p) / p;
        }

        value -= 1;

        return a * Mathf.PI * d * Mathf.Cos(2f * Mathf.PI * (d * value - s) / p) / (p * Mathf.Pow(2f, 10f * value)) -
               5f * NATURAL_LOG_OF_2 * a * Mathf.Sin(2f * Mathf.PI * (d * value - s) / p) /
               (Mathf.Pow(2f, 10f * value));
    }

    public static float SpringD(float start, float end, float value)
    {
        value = Mathf.Clamp01(value);
        end -= start;

        // Damn... Thanks http://www.derivative-calculator.net/
        // TODO: And it's a little bit wrong
        return end * (6f * (1f - value) / 5f + 1f) * (-2.2f * Mathf.Pow(1f - value, 1.2f) *
                                                      Mathf.Sin(
                                                          Mathf.PI * value * (2.5f * value * value * value + 0.2f)) +
                                                      Mathf.Pow(1f - value, 2.2f) *
                                                      (Mathf.PI * (2.5f * value * value * value + 0.2f) +
                                                       7.5f * Mathf.PI * value * value * value) *
                                                      Mathf.Cos(
                                                          Mathf.PI * value * (2.5f * value * value * value + 0.2f)) +
                                                      1f) -
               6f * end * (Mathf.Pow(1 - value, 2.2f) *
                   Mathf.Sin(Mathf.PI * value * (2.5f * value * value * value + 0.2f)) + value
                   / 5f);
    }

    public delegate float Function(float s, float e, float v);

    /// <summary>
    /// Returns the function associated to the easingFunction enum. This value returned should be cached as it allocates memory
    /// to return.
    /// </summary>
    /// <param name="easingFunction">The enum associated with the easing function.</param>
    /// <returns>The easing function</returns>
    public static Function GetEasingFunction(Ease easingFunction)
    {
        if (easingFunction == Ease.InQuad)
        {
            return EaseInQuad;
        }

        if (easingFunction == Ease.OutQuad)
        {
            return EaseOutQuad;
        }

        if (easingFunction == Ease.InOutQuad)
        {
            return EaseInOutQuad;
        }

        if (easingFunction == Ease.InCubic)
        {
            return EaseInCubic;
        }

        if (easingFunction == Ease.OutCubic)
        {
            return EaseOutCubic;
        }

        if (easingFunction == Ease.InOutCubic)
        {
            return EaseInOutCubic;
        }

        if (easingFunction == Ease.InQuart)
        {
            return EaseInQuart;
        }

        if (easingFunction == Ease.OutQuart)
        {
            return EaseOutQuart;
        }

        if (easingFunction == Ease.InOutQuart)
        {
            return EaseInOutQuart;
        }

        if (easingFunction == Ease.InQuint)
        {
            return EaseInQuint;
        }

        if (easingFunction == Ease.OutQuint)
        {
            return EaseOutQuint;
        }

        if (easingFunction == Ease.InOutQuint)
        {
            return EaseInOutQuint;
        }

        if (easingFunction == Ease.InSine)
        {
            return EaseInSine;
        }

        if (easingFunction == Ease.OutSine)
        {
            return EaseOutSine;
        }

        if (easingFunction == Ease.InOutSine)
        {
            return EaseInOutSine;
        }

        if (easingFunction == Ease.InExpo)
        {
            return EaseInExpo;
        }

        if (easingFunction == Ease.OutExpo)
        {
            return EaseOutExpo;
        }

        if (easingFunction == Ease.InOutExpo)
        {
            return EaseInOutExpo;
        }

        if (easingFunction == Ease.InCirc)
        {
            return EaseInCirc;
        }

        if (easingFunction == Ease.OutCirc)
        {
            return EaseOutCirc;
        }

        if (easingFunction == Ease.InOutCirc)
        {
            return EaseInOutCirc;
        }

        if (easingFunction == Ease.Linear)
        {
            return Linear;
        }

        if (easingFunction == Ease.Spring)
        {
            return Spring;
        }

        if (easingFunction == Ease.InBounce)
        {
            return EaseInBounce;
        }

        if (easingFunction == Ease.OutBounce)
        {
            return EaseOutBounce;
        }

        if (easingFunction == Ease.InOutBounce)
        {
            return EaseInOutBounce;
        }

        if (easingFunction == Ease.InBack)
        {
            return EaseInBack;
        }

        if (easingFunction == Ease.OutBack)
        {
            return EaseOutBack;
        }

        if (easingFunction == Ease.InOutBack)
        {
            return EaseInOutBack;
        }

        if (easingFunction == Ease.InElastic)
        {
            return EaseInElastic;
        }

        if (easingFunction == Ease.OutElastic)
        {
            return EaseOutElastic;
        }

        if (easingFunction == Ease.InOutElastic)
        {
            return EaseInOutElastic;
        }

        return null;
    }

    /// <summary>
    /// Gets the derivative function of the appropriate easing function. If you use an easing function for position then this
    /// function can get you the speed at a given time (normalized).
    /// </summary>
    /// <param name="easingFunction"></param>
    /// <returns>The derivative function</returns>
    public static Function GetEasingFunctionDerivative(Ease easingFunction)
    {
        if (easingFunction == Ease.InQuad)
        {
            return EaseInQuadD;
        }

        if (easingFunction == Ease.OutQuad)
        {
            return EaseOutQuadD;
        }

        if (easingFunction == Ease.InOutQuad)
        {
            return EaseInOutQuadD;
        }

        if (easingFunction == Ease.InCubic)
        {
            return EaseInCubicD;
        }

        if (easingFunction == Ease.OutCubic)
        {
            return EaseOutCubicD;
        }

        if (easingFunction == Ease.InOutCubic)
        {
            return EaseInOutCubicD;
        }

        if (easingFunction == Ease.InQuart)
        {
            return EaseInQuartD;
        }

        if (easingFunction == Ease.OutQuart)
        {
            return EaseOutQuartD;
        }

        if (easingFunction == Ease.InOutQuart)
        {
            return EaseInOutQuartD;
        }

        if (easingFunction == Ease.InQuint)
        {
            return EaseInQuintD;
        }

        if (easingFunction == Ease.OutQuint)
        {
            return EaseOutQuintD;
        }

        if (easingFunction == Ease.InOutQuint)
        {
            return EaseInOutQuintD;
        }

        if (easingFunction == Ease.InSine)
        {
            return EaseInSineD;
        }

        if (easingFunction == Ease.OutSine)
        {
            return EaseOutSineD;
        }

        if (easingFunction == Ease.InOutSine)
        {
            return EaseInOutSineD;
        }

        if (easingFunction == Ease.InExpo)
        {
            return EaseInExpoD;
        }

        if (easingFunction == Ease.OutExpo)
        {
            return EaseOutExpoD;
        }

        if (easingFunction == Ease.InOutExpo)
        {
            return EaseInOutExpoD;
        }

        if (easingFunction == Ease.InCirc)
        {
            return EaseInCircD;
        }

        if (easingFunction == Ease.OutCirc)
        {
            return EaseOutCircD;
        }

        if (easingFunction == Ease.InOutCirc)
        {
            return EaseInOutCircD;
        }

        if (easingFunction == Ease.Linear)
        {
            return LinearD;
        }

        if (easingFunction == Ease.Spring)
        {
            return SpringD;
        }

        if (easingFunction == Ease.InBounce)
        {
            return EaseInBounceD;
        }

        if (easingFunction == Ease.OutBounce)
        {
            return EaseOutBounceD;
        }

        if (easingFunction == Ease.InOutBounce)
        {
            return EaseInOutBounceD;
        }

        if (easingFunction == Ease.InBack)
        {
            return EaseInBackD;
        }

        if (easingFunction == Ease.OutBack)
        {
            return EaseOutBackD;
        }

        if (easingFunction == Ease.InOutBack)
        {
            return EaseInOutBackD;
        }

        if (easingFunction == Ease.InElastic)
        {
            return EaseInElasticD;
        }

        if (easingFunction == Ease.OutElastic)
        {
            return EaseOutElasticD;
        }

        if (easingFunction == Ease.InOutElastic)
        {
            return EaseInOutElasticD;
        }

        return null;
    }
}