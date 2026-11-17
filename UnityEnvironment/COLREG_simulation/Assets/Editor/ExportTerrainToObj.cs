// ExportTerrainToObj.cs
// -----------------------------------------------------------------------
// Esporta un Terrain di Unity come file .obj, da usare come ground truth
// nel confronto con le mesh ricostruite (Poisson/PPSurf) via
// confronta_con_ground_truth() nello script Python.
//
// Setup:
//   1. Crea una cartella "Editor" dentro Assets (se non esiste già):
//      Assets/Editor/
//   2. Metti questo file dentro Assets/Editor/ExportTerrainToObj.cs
//   3. In Unity: seleziona l'oggetto Terrain nella Hierarchy
//   4. Menu in alto: Tools > Export Terrain to OBJ
//   5. Scegli dove salvare il file .obj
//
// Nota: la risoluzione dell'heightmap di un Terrain è spesso 513x513 o
// più (oltre 260.000 vertici) -- DOWNSAMPLE_STEP riduce i vertici
// esportati prendendone uno ogni N, per tenere il file più leggero e
// il confronto più veloce. Con STEP=1 esporti la risoluzione piena.
// -----------------------------------------------------------------------

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;

public class ExportTerrainToObj : EditorWindow
{
    private const int DOWNSAMPLE_STEP = 2; // 1 = risoluzione piena, 2 = metà, 4 = un quarto, ecc.

    [MenuItem("Tools/Export Terrain to OBJ")]
    private static void ExportSelectedTerrain()
    {
        Terrain terrain = null;

        if (Selection.activeGameObject != null)
            terrain = Selection.activeGameObject.GetComponent<Terrain>();

        if (terrain == null)
            terrain = Terrain.activeTerrain;

        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Export Terrain to OBJ",
                "Nessun Terrain selezionato e nessun Terrain attivo trovato nella scena.",
                "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanel(
            "Esporta Terrain come OBJ",
            Application.dataPath,
            terrain.name + "_ground_truth",
            "obj");

        if (string.IsNullOrEmpty(path))
            return;

        ExportTerrain(terrain, path, DOWNSAMPLE_STEP);

        EditorUtility.DisplayDialog("Export Terrain to OBJ",
            "Esportazione completata:\n" + path, "OK");
    }

    private static void ExportTerrain(Terrain terrain, string path, int step)
    {
        TerrainData data = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        int heightmapRes = data.heightmapResolution; // es. 513
        Vector3 terrainSize = data.size;              // dimensioni reali in metri (x, y=altezza max, z)

        // Campiona le altezze con lo step scelto (downsampling)
        int sampledWidth = Mathf.CeilToInt((heightmapRes - 1f) / step) + 1;
        int sampledHeight = sampledWidth;

        float[,] heights = data.GetHeights(0, 0, heightmapRes, heightmapRes);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("# Terrain esportato da Unity per confronto ground truth");
        sb.AppendLine("# Risoluzione heightmap originale: " + heightmapRes + "x" + heightmapRes);
        sb.AppendLine("# Downsample step: " + step);
        sb.AppendLine("o " + terrain.name + "_ground_truth");

        // --- Vertici ---
        // L'heightmap di Unity è indicizzata [z, x] (riga, colonna); l'altezza normalizzata
        // (0..1) va moltiplicata per terrainSize.y per ottenere l'altezza reale in metri.
        for (int zi = 0; zi < heightmapRes; zi += step)
        {
            for (int xi = 0; xi < heightmapRes; xi += step)
            {
                float worldX = terrainPos.x + (xi / (float)(heightmapRes - 1)) * terrainSize.x;
                float worldZ = terrainPos.z + (zi / (float)(heightmapRes - 1)) * terrainSize.z;
                float worldY = terrainPos.y + heights[zi, xi] * terrainSize.y;

                // Formato OBJ: v x y z (asse Y verticale, coerente con la convenzione
                // usata nel resto della pipeline: Y = altezza)
                sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "v {0} {1} {2}", worldX, worldY, worldZ));
            }
        }

        // --- Facce (due triangoli per ogni quadrato della griglia) ---
        // OBJ usa indici a partire da 1, non da 0.
        for (int zi = 0; zi < sampledHeight - 1; zi++)
        {
            for (int xi = 0; xi < sampledWidth - 1; xi++)
            {
                int i0 = zi * sampledWidth + xi + 1;
                int i1 = zi * sampledWidth + (xi + 1) + 1;
                int i2 = (zi + 1) * sampledWidth + xi + 1;
                int i3 = (zi + 1) * sampledWidth + (xi + 1) + 1;

                // Triangolo 1: i0, i2, i1  |  Triangolo 2: i1, i2, i3
                sb.AppendLine("f " + i0 + " " + i2 + " " + i1);
                sb.AppendLine("f " + i1 + " " + i2 + " " + i3);
            }
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log("Terrain esportato in: " + path +
                   " (" + (sampledWidth * sampledHeight) + " vertici)");
    }
}