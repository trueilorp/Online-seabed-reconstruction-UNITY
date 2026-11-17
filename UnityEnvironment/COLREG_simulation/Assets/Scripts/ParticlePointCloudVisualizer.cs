using UnityEngine;
using UnitySensors.Interface.Sensor;
using UnitySensors.Interface.Sensor.PointCloud;
using UnitySensors.DataType.Sensor.PointCloud;

[RequireComponent(typeof(ParticleSystem))]
public class ParticlePointCloudVisualizer : MonoBehaviour
{
    [SerializeField] private Object _source;
    [SerializeField] private float _minDistance = 0.5f;
    [SerializeField] private float _maxDistance = 40f;
    [SerializeField] private Color _nearColor = new Color(1f, 0.85f, 0f); // giallo/oro
    [SerializeField] private Color _farColor = new Color(0f, 0.3f, 1f);   // blu

    private IPointCloudInterface<PointXYZI> _sourceInterface;
    private ParticleSystem _ps;
    private ParticleSystem.Particle[] _particles;

    private void Start()
    {
        _ps = GetComponent<ParticleSystem>();
        _sourceInterface = _source as IPointCloudInterface<PointXYZI>;

        if (_sourceInterface == null)
        {
            Debug.LogWarning("ParticlePointCloudVisualizer: source does not implement IPointCloudInterface<PointXYZI>.");
        }
    }

    private void Update()
    {
        if (_sourceInterface == null) return;

        int count = _sourceInterface.pointsNum;
        if (count <= 0) return;

        if (_particles == null || _particles.Length < count)
            _particles = new ParticleSystem.Particle[count];

        var points = _sourceInterface.pointCloud.points;

        for (int i = 0; i < count; i++)
        {
            Vector3 localPos = points[i].position;
            Vector3 worldPos = transform.TransformPoint(localPos);

            float dist = localPos.magnitude;
            float t = Mathf.InverseLerp(_minDistance, _maxDistance, dist);
            Color pointColor = Color.Lerp(_nearColor, _farColor, t);

            _particles[i].position = worldPos;
            _particles[i].startSize = 0.15f;
            _particles[i].startColor = pointColor;
            _particles[i].remainingLifetime = 1f;
            _particles[i].startLifetime = 1f;
        }

        _ps.SetParticles(_particles, count);
    }
}