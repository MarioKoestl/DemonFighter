#nullable enable
using System;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace DemonFighter.Presentation.Combat
{
    /// <summary>
    /// Viscera bursts (GAME_DESIGN, "Visible damage and gore", stage 2): lumps and strands thrown from a sever, a
    /// destroyed part or a death, flying on a plain ballistic arc without physics, landing on the ground found under
    /// the burst, lying there for a while and shrinking away. Pooled and reused oldest first (D-081). Look only.
    /// </summary>
    public sealed class VisceraPool
    {
        private const int LumpKinds = 4;
        private const int StrandKinds = 2;
        private const float ScatterPerSize = 0.5f;
        private const float UpwardBias = 1.2f;
        private const float RestHeightPerSize = 0.4f;
        private const float GroundProbeHeight = 1f;
        private const float GroundProbeDepth = 30f;
        private const float SpinDegreesPerSecond = 360f;

        private readonly GoreSettings _settings;
        private readonly Material _material;
        private readonly Transform _root;
        private readonly Mesh[] _meshes;
        private readonly Piece[] _pieces;
        private int _next;

        public VisceraPool(GoreSettings settings, Material material, Transform parent)
        {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            _material = material != null ? material : throw new ArgumentNullException(nameof(material));
            _root = new GameObject("Viscera").transform;
            _root.SetParent(parent, false);
            _meshes = new Mesh[LumpKinds + StrandKinds];
            for (int i = 0; i < LumpKinds; i++)
            {
                _meshes[i] = VisceraMeshes.CreateLump(i + 1);
            }

            for (int i = 0; i < StrandKinds; i++)
            {
                _meshes[LumpKinds + i] = VisceraMeshes.CreateStrand(i + 1);
            }

            _pieces = new Piece[Mathf.Max(1, settings.VisceraCap)];
        }

        /// <summary>Throws pieces from a point, sized by the body they came from.</summary>
        public void Burst(Vector3 position, float sizeMeters, int count)
        {
            float groundY = GroundBelow(position);
            float baseSize = sizeMeters * _settings.VisceraSizePerMeter;
            for (int i = 0; i < count; i++)
            {
                Piece piece = Take();
                float size = baseSize * Random.Range(0.6f, 1.4f);
                piece.Size = size;
                piece.GroundY = groundY;
                piece.Born = Time.time;
                piece.Resting = false;
                piece.Velocity = (Random.insideUnitSphere + Vector3.up * UpwardBias).normalized * (_settings.VisceraSpeed * Random.Range(0.5f, 1.2f));
                piece.Spin = Random.insideUnitSphere * SpinDegreesPerSecond;
                piece.Filter.sharedMesh = _meshes[Random.Range(0, _meshes.Length)];
                Transform transform = piece.Transform;
                transform.SetPositionAndRotation(position + Random.insideUnitSphere * (size * ScatterPerSize), Random.rotation);
                transform.localScale = Vector3.one * size;
                piece.Object.SetActive(true);
            }
        }

        /// <summary>Flies, lands and ages every live piece; call once per frame.</summary>
        public void Update(float deltaTime, float now)
        {
            for (int i = 0; i < _pieces.Length; i++)
            {
                Piece? piece = _pieces[i];
                if (piece == null || !piece.Object.activeSelf)
                {
                    continue;
                }

                float age = now - piece.Born;
                if (age >= _settings.VisceraLifeSeconds)
                {
                    piece.Object.SetActive(false);
                    continue;
                }

                if (!piece.Resting)
                {
                    piece.Velocity += Vector3.up * (_settings.VisceraGravity * deltaTime);
                    Vector3 position = piece.Transform.position + piece.Velocity * deltaTime;
                    float restHeight = piece.GroundY + piece.Size * RestHeightPerSize;
                    if (position.y <= restHeight)
                    {
                        position.y = restHeight;
                        piece.Resting = true;
                    }

                    piece.Transform.position = position;
                    piece.Transform.Rotate(piece.Spin * deltaTime, Space.World);
                }

                float shrink = GoreMath.ShrinkFactor(age, _settings.VisceraLifeSeconds, _settings.VisceraShrinkSeconds);
                if (shrink < 1f)
                {
                    piece.Transform.localScale = Vector3.one * (piece.Size * shrink);
                }
            }
        }

        /// <summary>Removes every piece; called when the run ends.</summary>
        public void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }

            for (int i = 0; i < _meshes.Length; i++)
            {
                Object.Destroy(_meshes[i]);
            }
        }

        private Piece Take()
        {
            Piece? piece = _pieces[_next];
            if (piece == null)
            {
                var pieceObject = new GameObject("Viscera");
                pieceObject.transform.SetParent(_root, false);
                MeshFilter filter = pieceObject.AddComponent<MeshFilter>();
                MeshRenderer renderer = pieceObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                piece = new Piece(pieceObject, filter);
                _pieces[_next] = piece;
            }

            _next = (_next + 1) % _pieces.Length;
            return piece;
        }

        private static float GroundBelow(Vector3 position)
        {
            return GroundProbe.TryFind(position, GroundProbeHeight, GroundProbeDepth, out Vector3 ground) ? ground.y : position.y;
        }

        private sealed class Piece
        {
            public Piece(GameObject gameObject, MeshFilter filter)
            {
                Object = gameObject;
                Transform = gameObject.transform;
                Filter = filter;
            }

            public GameObject Object { get; }

            public Transform Transform { get; }

            public MeshFilter Filter { get; }

            public Vector3 Velocity { get; set; }

            public Vector3 Spin { get; set; }

            public float Born { get; set; }

            public float GroundY { get; set; }

            public float Size { get; set; }

            public bool Resting { get; set; }
        }
    }
}
