using UnityEngine;

namespace Getaway
{
    /// <summary>
    /// Alternates the two halves of a patrol car's light bar. Toggling renderers costs nothing and
    /// reads from far further away than a static coloured block, which matters because the patrols
    /// spend most of a run behind the camera.
    /// </summary>
    public sealed class PoliceLights : MonoBehaviour
    {
        public Renderer red, blue;
        [Min(0.05f)] public float interval = 0.22f;
        float next;
        bool onRed = true;

        void LateUpdate()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + interval;
            onRed = !onRed;
            if (red != null) red.enabled = onRed;
            if (blue != null) blue.enabled = !onRed;
        }
    }
}
