using UnityEngine;

public class TerrainCheck : MonoBehaviour
{
    [ContextMenu("Check heights")]
    void Check()
    {
        var t = GetComponent<Terrain>();
        Vector2[] pts = { new(25, 25), new(128, 128), new(200, 230), new(20, 150) };
        foreach (var p in pts)
        {
            var w = new Vector3(p.x, 0, p.y) + t.transform.position;
            float y = t.SampleHeight(w) + t.transform.position.y;
            Debug.Log($"x={p.x} z(nord)={p.y} -> quota {y:F2} m");
        }
    }
}