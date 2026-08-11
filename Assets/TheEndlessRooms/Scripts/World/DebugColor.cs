using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Flat, unmistakable colors for grey-box testing — walls, doors, the Attendant,
    /// hiding spots, and pickups all look identical otherwise (default primitive grey),
    /// which is exactly what made doors hard to tell apart from walls during Milestone
    /// 7 testing. Deliberately simple (Unlit shader, no lighting dependence) so color
    /// reads the same regardless of scene lighting. Replace with real materials once
    /// Milestone 8 does an art pass — this is placeholder-by-design, not a rendering
    /// system.
    /// </summary>
    public static class DebugColor
    {
        public static readonly Color Wall = Color.yellow;
        public static readonly Color Door = new(0.45f, 0.28f, 0.1f);
        public static readonly Color Attendant = Color.red;
        public static readonly Color HidingSpot = Color.blue;
        public static readonly Color Pickup = Color.green;

        public static void Apply(GameObject target, Color color)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader == null)
            {
                return;
            }

            var material = new Material(shader) { color = color };
            renderer.material = material;
        }
    }
}
