using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnitySensors.Interface.Sensor;
using UnitySensors.Interface.Sensor.PointCloud;
using UnitySensors.DataType.Sensor.PointCloud;
 
public class PointCloudRecorder : MonoBehaviour
{
    [Header("Sorgente dati (stesso oggetto usato da ParticlePointCloudVisualizer)")]
    [SerializeField] private UnityEngine.Object _source;
 
    [Header("Registrazione")]
    [Tooltip("Se true, registra automaticamente uno scan ogni 'Record Interval' secondi")]
    [SerializeField] private bool _recordingContinuous = false;
    [SerializeField] private float _recordInterval = 1.0f;
    [Tooltip("Tasto per registrare manualmente uno scan singolo")]
    [SerializeField] private KeyCode _manualRecordKey = KeyCode.R;
 
    [Header("Output")]
    [Tooltip("Cartella di output, relativa alla cartella del progetto Unity (fuori da Assets)")]
    [SerializeField] private string _outputFolder = "Recordings";
 
    private IPointCloudInterface<PointXYZI> _sourceInterface;
    private StreamWriter _writer;
    private float _timeSinceLastRecord = 0f;
    private int _pingNumber = 0;
    private int _totalPointsWritten = 0;
    private string _outputPath;
 
    private void Start()
    {
        _sourceInterface = ResolvePointCloudInterface(_source);
 
        if (_sourceInterface == null)
        {
            Debug.LogWarning("PointCloudRecorder: la sorgente non implementa " +
                              "IPointCloudInterface<PointXYZI> (nemmeno cercando tra i " +
                              "componenti del GameObject collegato). Verifica il campo 'Source'.");
            enabled = false;
            return;
        }
 
        string folderPath = Path.Combine(Application.dataPath, "..", _outputFolder);
        Directory.CreateDirectory(folderPath);
 
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _outputPath = Path.Combine(folderPath, "scan_" + timestamp + ".csv");
 
        _writer = new StreamWriter(_outputPath, append: false);
        _writer.WriteLine("x,y,z,intensity,ping_number,elapsed_s");
        _writer.Flush();
 
        Debug.Log("PointCloudRecorder: registrazione su " + _outputPath +
                   " -- tasto " + _manualRecordKey + " per scan manuale" +
                   (_recordingContinuous ? ", registrazione continua attiva." : "."));
    }
 
    /// <summary>
    /// Cerca IPointCloudInterface<PointXYZI> nel riferimento assegnato al campo Source,
    /// qualunque sia il tipo esatto trascinato (il componente stesso, oppure il
    /// GameObject/altro componente sullo stesso oggetto).
    /// </summary>
    private IPointCloudInterface<PointXYZI> ResolvePointCloudInterface(UnityEngine.Object source)
    {
        if (source == null) return null;
 
        // Caso 1: il riferimento e' gia' direttamente il componente giusto
        if (source is IPointCloudInterface<PointXYZI> direct)
            return direct;
 
        // Caso 2: e' stato trascinato il GameObject -> cerca tra i suoi componenti
        if (source is GameObject go)
            return go.GetComponent(typeof(IPointCloudInterface<PointXYZI>)) as IPointCloudInterface<PointXYZI>;
 
        // Caso 3: e' stato trascinato un altro Component sullo stesso oggetto
        if (source is Component comp)
            return comp.GetComponent(typeof(IPointCloudInterface<PointXYZI>)) as IPointCloudInterface<PointXYZI>;
 
        return null;
    }
 
    private void Update()
    {
        if (_sourceInterface == null) return;
 
        if (Input.GetKeyDown(_manualRecordKey))
        {
            RecordScan();
        }
 
        if (_recordingContinuous)
        {
            _timeSinceLastRecord += Time.deltaTime;
            if (_timeSinceLastRecord >= _recordInterval)
            {
                _timeSinceLastRecord = 0f;
                RecordScan();
            }
        }
    }
 
    private void RecordScan()
    {
        int count = _sourceInterface.pointsNum;
        if (count <= 0) return;
 
        var points = _sourceInterface.pointCloud.points;
 
        var sb = new StringBuilder();
        int puntiScartati = 0;
        for (int i = 0; i < count; i++)
        {
            float intensity = points[i].intensity;
 
            // I raggi che non colpiscono nulla (o colpiscono più vicino di minRange)
            // producono un punto fittizio a distanza 0 (posizione del sensore stesso)
            // con intensity = 0 -- vedi IRaycastHitsToPointsJob.cs nella libreria
            // UnitySensors. Vanno scartati, altrimenti generano uno strato fantasma
            // di punti concentrati alla profondità di montaggio del sensore.
            if (intensity == 0f)
            {
                puntiScartati++;
                continue;
            }
 
            Vector3 localPos = points[i].position;
            Vector3 worldPos = transform.TransformPoint(localPos);
 
            sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5}",
                worldPos.x, worldPos.y, worldPos.z, intensity, _pingNumber, Time.time));
        }
 
        _writer.Write(sb.ToString());
        _writer.Flush();
 
        _totalPointsWritten += (count - puntiScartati);
        _pingNumber++;
 
        Debug.Log("PointCloudRecorder: scan #" + _pingNumber + " registrato (" +
                   (count - puntiScartati) + " punti validi, " + puntiScartati +
                   " scartati come miss, totale finora: " + _totalPointsWritten + ")");
    }
 
    private void OnDestroy()
    {
        CloseWriter();
    }
 
    private void OnApplicationQuit()
    {
        CloseWriter();
    }
 
    private void CloseWriter()
    {
        if (_writer != null)
        {
            _writer.Flush();
            _writer.Close();
            _writer = null;
            Debug.Log("PointCloudRecorder: file chiuso -- " + _totalPointsWritten +
                       " punti totali salvati in " + _outputPath);
        }
    }
}